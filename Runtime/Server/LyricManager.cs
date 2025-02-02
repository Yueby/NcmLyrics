using UnityEngine;
using System;
using System.Threading.Tasks;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Messages;

namespace Yueby.NcmLyrics.Server
{
    public class LyricManager : IDisposable
    {
        private LyricServer _server;
        private bool _isInitialized;
        private int _port;

        // 当前状态
        public SongInfo CurrentSong { get; private set; }
        public LyricData CurrentLyric { get; private set; }
        public ProgressData CurrentProgress { get; private set; }
        public PlayStateData CurrentPlayState { get; private set; }
        public bool IsConnected => _server?.IsRunning ?? false;
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

        private long _currentTime; // 当前时间（毫秒）
        private float _lastUpdateTime; // 最后更新时间
        private const float UPDATE_INTERVAL = 0.016f; // 更新间隔
        private float _playbackSpeed = 1.0f; // 播放速度

        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Mathf.Clamp(value, 0.1f, 2.0f);
        }

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

                _server = new LyricServer(_port);
                CreateEventHandlers();
                RegisterEvents();
                _ = _server.StartAsync();
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
                ResetProgress(); // 重置进度
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
                _currentTime = progress.time;
                _lastUpdateTime = Time.realtimeSinceStartup;
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
            if (_server == null) return;

            _server.OnServerStarted += _onServerStarted;
            _server.OnServerStopped += _onServerStopped;
            _server.OnError += _onError;
            _server.OnSongChanged += _onSongChanged;
            _server.OnLyricReceived += _onLyricReceived;
            _server.OnProgressUpdated += _onProgressUpdated;
            _server.OnPlayStateChanged += _onPlayStateChanged;
        }

        private void UnregisterEvents()
        {
            if (_server == null) return;

            _server.OnServerStarted -= _onServerStarted;
            _server.OnServerStopped -= _onServerStopped;
            _server.OnError -= _onError;
            _server.OnSongChanged -= _onSongChanged;
            _server.OnLyricReceived -= _onLyricReceived;
            _server.OnProgressUpdated -= _onProgressUpdated;
            _server.OnPlayStateChanged -= _onPlayStateChanged;
        }

        public void Dispose()
        {
            if (_server != null)
            {
                try
                {
                    UnregisterEvents();
                    _server.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NcmLyrics] 释放资源时发生错误: {ex.Message}");
                }
                finally
                {
                    _server = null;
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

        public void Update(float deltaTime)
        {
            if (CurrentPlayState?.IsPlaying != true) return;

            // 计算新的进度（使用double避免精度问题）
            double timeIncrement = deltaTime * 1000.0 * _playbackSpeed;
            _currentTime += (long)timeIncrement;
            _lastUpdateTime = Time.realtimeSinceStartup;
        }

        public void OnProgressReceived(ProgressData progress)
        {
            if (progress == null) return;

            // 直接更新进度
            _currentTime = progress.time;
            _lastUpdateTime = Time.realtimeSinceStartup;
            CurrentProgress = progress;
            OnProgressUpdated?.Invoke(progress);
        }

        public long GetCurrentTime()
        {
            return _currentTime;
        }

        private void ResetProgress()
        {
            _currentTime = 0;
            _lastUpdateTime = Time.realtimeSinceStartup;
        }
    }
} 