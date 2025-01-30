using UnityEngine;
using System;
using System.Threading.Tasks;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Messages;

namespace Yueby.NcmLyrics.Client
{
    public class LyricManager : IDisposable
    {
        private LyricClient _client;
        private bool _isInitialized;
        private int _port;

        // 当前状态
        public SongInfo CurrentSong { get; private set; }
        public LyricData CurrentLyric { get; private set; }
        public ProgressData CurrentProgress { get; private set; }
        public PlayStateData CurrentPlayState { get; private set; }
        public bool IsConnected => _client?.IsRunning ?? false;
        public int Port => _port;

        // 事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<SongInfo> OnSongChanged;
        public event Action<LyricData> OnLyricReceived;
        public event Action<ProgressData> OnProgressUpdated;
        public event Action<PlayStateData> OnPlayStateChanged;

        private Action _onServerStarted;
        private Action _onServerStopped;
        private Action<ErrorData> _onError;
        private Action<SongInfo> _onSongChanged;
        private Action<LyricData> _onLyricReceived;
        private Action<ProgressData> _onProgressUpdated;
        private Action<PlayStateData> _onPlayStateChanged;

        public void Initialize(int port = 35010)
        {
            _port = port;
            InitializeClient();
        }

        private void InitializeClient()
        {
            try
            {
                if (_isInitialized)
                {
                    Dispose();
                }

                // 等待一小段时间确保端口被释放
                System.Threading.Thread.Sleep(100);

                _client = new LyricClient(_port);
                CreateEventHandlers();
                RegisterEvents();
                _ = _client.StartAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NcmLyrics] 初始化失败: {ex.Message}");
                OnError?.Invoke(ex.Message);
                Dispose();
            }
        }

        public void UpdatePort(int port)
        {
            if (_port == port) return;
            _port = port;
            InitializeClient();
        }

        private void CreateEventHandlers()
        {
            _onServerStarted = () =>
            {
                if (!_isInitialized) return;
                Debug.Log("[NcmLyrics] 已连接到服务器");
                OnConnected?.Invoke();
            };

            _onServerStopped = () =>
            {
                if (!_isInitialized) return;
                Debug.Log("[NcmLyrics] 已断开连接");
                OnDisconnected?.Invoke();
            };

            _onError = error =>
            {
                if (!_isInitialized) return;
                OnError?.Invoke(error.message);
            };

            _onSongChanged = song =>
            {
                if (!_isInitialized) return;
                CurrentSong = song;
                // 切换歌曲时清空相关数据
                CurrentLyric = null;
                CurrentProgress = null;
                CurrentPlayState = null;
                OnSongChanged?.Invoke(song);
            };

            _onLyricReceived = lyric =>
            {
                if (!_isInitialized) return;
                CurrentLyric = lyric;
                OnLyricReceived?.Invoke(lyric);
            };

            _onProgressUpdated = progress =>
            {
                if (!_isInitialized) return;
                CurrentProgress = progress;
                OnProgressUpdated?.Invoke(progress);
            };

            _onPlayStateChanged = state =>
            {
                if (!_isInitialized) return;
                CurrentPlayState = state;
                OnPlayStateChanged?.Invoke(state);
            };
        }

        private void RegisterEvents()
        {
            if (_client == null) return;

            _client.OnServerStarted += _onServerStarted;
            _client.OnServerStopped += _onServerStopped;
            _client.OnError += _onError;
            _client.OnSongChanged += _onSongChanged;
            _client.OnLyricReceived += _onLyricReceived;
            _client.OnProgressUpdated += _onProgressUpdated;
            _client.OnPlayStateChanged += _onPlayStateChanged;
        }

        private void UnregisterEvents()
        {
            if (_client == null) return;

            _client.OnServerStarted -= _onServerStarted;
            _client.OnServerStopped -= _onServerStopped;
            _client.OnError -= _onError;
            _client.OnSongChanged -= _onSongChanged;
            _client.OnLyricReceived -= _onLyricReceived;
            _client.OnProgressUpdated -= _onProgressUpdated;
            _client.OnPlayStateChanged -= _onPlayStateChanged;
        }

        public void Dispose()
        {
            if (_client != null)
            {
                try
                {
                    UnregisterEvents();
                    _client.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NcmLyrics] 释放资源时发生错误: {ex.Message}");
                }
                finally
                {
                    _client = null;
                }
            }
            _isInitialized = false;

            // 清空所有状态
            CurrentSong = null;
            CurrentLyric = null;
            CurrentProgress = null;
            CurrentPlayState = null;
        }

        // 获取当前播放的歌词行索引
        public int GetCurrentLineIndex()
        {
            if (CurrentLyric?.lines == null || CurrentProgress == null) return -1;

            for (int i = 0; i < CurrentLyric.lines.Length; i++)
            {
                var line = CurrentLyric.lines[i];
                if (line.time <= CurrentProgress.time &&
                    (i == CurrentLyric.lines.Length - 1 || CurrentLyric.lines[i + 1].time > CurrentProgress.time))
                {
                    return i;
                }
            }
            return -1;
        }

        // 获取当前播放进度的格式化时间
        public string GetFormattedTime()
        {
            if (CurrentProgress == null) return "0:00/0:00";

            string FormatTime(long milliseconds)
            {
                TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
                return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
            }

            return $"{FormatTime(CurrentProgress.time)}/{FormatTime(CurrentProgress.duration)}";
        }

        // 获取当前播放进度（0-1）
        public float GetProgress()
        {
            if (CurrentProgress == null) return 0f;
            return CurrentProgress.time / (float)CurrentProgress.duration;
        }
    }
} 