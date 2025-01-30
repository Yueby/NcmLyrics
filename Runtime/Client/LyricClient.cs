using System;
using System.Threading.Tasks;
using UnityEngine;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Messages;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace Yueby.NcmLyrics.Client
{
    public class LyricClient : IDisposable
    {
        private readonly HttpListener _httpListener;
        private readonly object _lock = new object();
        private bool _disposed;
        public bool IsRunning { get; private set; }
        public int Port { get; private set; }

        public event Action<SongInfo> OnSongChanged;
        public event Action<LyricData> OnLyricReceived;
        public event Action<ProgressData> OnProgressUpdated;
        public event Action<PlayStateData> OnPlayStateChanged;
        public event Action<ErrorData> OnError;
        public event Action OnServerStarted;
        public event Action OnServerStopped;

        public LyricClient(int port = 35010)
        {
            Port = port;
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
            // _httpListener.Prefixes.Add($"http://localhost:{port}/");
        }

        public async Task StartAsync()
        {
            lock (_lock)
            {
                if (_disposed || IsRunning) return;
                try
                {
                    _httpListener.Start();
                    IsRunning = true;
                    OnServerStarted?.Invoke();
                    Debug.Log("[NcmLyrics] Server started");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NcmLyrics] {ex.Message}");
                    OnError?.Invoke(new ErrorData { message = ex.Message });
                    return;
                }
            }

            try
            {
                while (IsRunning && !_disposed)
                {
                    var context = await _httpListener.GetContextAsync();
                    if (!IsRunning || _disposed) break;
                    _ = HandleRequestAsync(context);
                }
            }
            catch (Exception ex)
            {
                if (!_disposed)
                {
                    Debug.LogError($"[NcmLyrics] {ex.Message}");
                    OnError?.Invoke(new ErrorData { message = ex.Message });
                }
            }
            finally
            {
                if (IsRunning)
                {
                    Stop();
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            if (_disposed) return;

            try
            {
                using var response = context.Response;

                // 处理PING请求
                if (context.Request.Url.LocalPath == "/ping")
                {
                    response.StatusCode = 200;
                    return;
                }

                // 只处理POST请求
                if (context.Request.HttpMethod == "POST")
                {
                    string json;
                    using (var reader = new StreamReader(context.Request.InputStream))
                    {
                        json = await reader.ReadToEndAsync();
                    }

                    if (!_disposed)
                    {
                        var baseMessage = JsonConvert.DeserializeObject<BaseMessage<object>>(json);
                        switch (baseMessage.type)
                        {
                            case LyricMessageType.SONG_CHANGE:
                                var songMsg = JsonConvert.DeserializeObject<SongMessage>(json);
                                OnSongChanged?.Invoke(songMsg.data);
                                break;
                            case LyricMessageType.LYRIC:
                                var lyricMsg = JsonConvert.DeserializeObject<LyricMessage>(json);
                                OnLyricReceived?.Invoke(lyricMsg.data);
                                break;
                            case LyricMessageType.PROGRESS:
                                var progressMsg = JsonConvert.DeserializeObject<ProgressMessage>(json);
                                OnProgressUpdated?.Invoke(progressMsg.data);
                                break;
                            case LyricMessageType.PLAY_STATE:
                                var stateMsg = JsonConvert.DeserializeObject<PlayStateMessage>(json);
                                OnPlayStateChanged?.Invoke(stateMsg.data);
                                break;
                            case LyricMessageType.ERROR:
                                var errorMsg = JsonConvert.DeserializeObject<ErrorMessage>(json);
                                OnError?.Invoke(errorMsg.data);
                                break;
                        }
                    }
                }

                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                if (!_disposed)
                {
                    Debug.LogError($"[NcmLyrics] {ex.Message}");
                    OnError?.Invoke(new ErrorData { message = ex.Message });
                }
                try
                {
                    context.Response.StatusCode = 500;
                }
                catch { }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!IsRunning) return;

                IsRunning = false;
                try
                {
                    if (_httpListener?.IsListening == true)
                    {
                        _httpListener.Stop();
                        OnServerStopped?.Invoke();
                        Debug.Log("[NcmLyrics] Server stopped");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NcmLyrics] Stop server failed: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        _httpListener?.Close();
                    }
                    catch { }
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                try
                {
                    Stop();
                }
                finally
                {
                    try
                    {
                        if (_httpListener != null)
                        {
                            _httpListener.Close();
                            (_httpListener as IDisposable)?.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NcmLyrics] Release resources failed: {ex.Message}");
                    }
                }
            }
        }
    }
}