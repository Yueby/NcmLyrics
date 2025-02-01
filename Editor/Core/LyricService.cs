using UnityEditor;
using UnityEngine;
using System;
using Yueby.NcmLyrics.Server;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Editor.Data;

namespace Yueby.NcmLyrics.Editor.Windows
{
    [InitializeOnLoad]
    public class LyricService
    {
        private static LyricManager _lyricManager;
        private static bool _isReconnecting;
        private static float _lastReconnectTime;
        private const float RECONNECT_INTERVAL = 5f; // Reconnection interval (seconds)
        
        // Renderer
        private static LyricRenderer _renderer;
        
        // Service status
        public static bool IsInitialized { get; private set; }
        public static SongInfo CurrentSong => _lyricManager?.CurrentSong;
        public static LyricData CurrentLyric => _lyricManager?.CurrentLyric;
        public static ProgressData CurrentProgress => _lyricManager?.CurrentProgress;
        public static PlayStateData CurrentPlayState => _lyricManager?.CurrentPlayState;
        public static bool IsConnected => _lyricManager?.IsConnected ?? false;
        public static int Port => _lyricManager?.Port ?? LyricConfig.Instance.Port;

        // Events
        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action<string> OnError;
        public static event Action<SongInfo> OnSongChanged;
        public static event Action<LyricData> OnLyricReceived;
        public static event Action<ProgressData> OnProgressUpdated;
        public static event Action<PlayStateData> OnPlayStateChanged;

        private static float _lastUpdateTime;
        private static float _deltaTime;

        // 统一的Update事件
        public static event Action<float> OnUpdateTick;

        static LyricService()
        {
            Initialize();
            EditorApplication.quitting += OnEditorQuitting;
            EditorApplication.update += OnUpdate;
        }

        public static LyricRenderer GetRenderer()
        {
            if (_renderer == null)
            {
                _renderer = new LyricRenderer();
                RegisterRendererEvents();
            }
            return _renderer;
        }

        public static void ReleaseRenderer()
        {
            if (_renderer != null)
            {
                UnregisterRendererEvents();
                _renderer.Dispose();
                _renderer = null;
            }
        }

        private static void RegisterRendererEvents()
        {
            if (_renderer == null) return;
            OnSongChanged += _renderer.OnSongChanged;
            OnLyricReceived += _renderer.OnLyricReceived;
            OnProgressUpdated += _renderer.OnProgressUpdated;
            OnPlayStateChanged += _renderer.OnPlayStateChanged;

            // Sync current state
            if (CurrentSong != null)
            {
                _renderer.OnSongChanged(CurrentSong);
            }
            if (CurrentLyric != null)
            {
                _renderer.OnLyricReceived(CurrentLyric);
            }
            if (CurrentProgress != null)
            {
                _renderer.OnProgressUpdated(CurrentProgress);
            }
            if (CurrentPlayState != null)
            {
                _renderer.OnPlayStateChanged(CurrentPlayState);
            }
        }

        private static void UnregisterRendererEvents()
        {
            if (_renderer == null) return;
            OnSongChanged -= _renderer.OnSongChanged;
            OnLyricReceived -= _renderer.OnLyricReceived;
            OnProgressUpdated -= _renderer.OnProgressUpdated;
            OnPlayStateChanged -= _renderer.OnPlayStateChanged;
        }

        private static void Initialize()
        {
            if (IsInitialized) return;

            try
            {
                _lyricManager = new LyricManager();
                RegisterEvents();
                
                _lyricManager.Initialize(LyricConfig.Instance.Port);
                
                IsInitialized = true;
                Debug.Log("[NcmLyrics] Lyric service started");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NcmLyrics] Failed to initialize lyric service: {ex.Message}");
                Dispose();
            }
        }

        private static void RegisterEvents()
        {
            if (_lyricManager == null) return;

            _lyricManager.OnConnected += () => 
            {
                _isReconnecting = false;
                OnConnected?.Invoke();
            };
            _lyricManager.OnDisconnected += () => 
            {
                OnDisconnected?.Invoke();
                TryReconnect();
            };
            _lyricManager.OnError += error => OnError?.Invoke(error);
            _lyricManager.OnSongChanged += song => OnSongChanged?.Invoke(song);
            _lyricManager.OnLyricReceived += lyric => OnLyricReceived?.Invoke(lyric);
            _lyricManager.OnProgressUpdated += progress => OnProgressUpdated?.Invoke(progress);
            _lyricManager.OnPlayStateChanged += state => OnPlayStateChanged?.Invoke(state);
        }

        private static void OnEditorQuitting()
        {
            EditorApplication.update -= OnUpdate;
            Dispose();
        }

        private static void OnUpdate()
        {
            if (!IsInitialized) return;

            float currentTime = Time.realtimeSinceStartup;
            _deltaTime = _lastUpdateTime > 0 ? currentTime - _lastUpdateTime : 0;
            _lastUpdateTime = currentTime;

            // 触发统一的Update事件
            OnUpdateTick?.Invoke(_deltaTime);
            
            // 更新LyricManager
            _lyricManager?.Update(_deltaTime);
            
            // 更新渲染器
            if (_renderer != null)
            {
                _renderer.Update(_deltaTime);
            }

            // 重连逻辑
            if (_isReconnecting && currentTime - _lastReconnectTime >= RECONNECT_INTERVAL)
            {
                _lastReconnectTime = currentTime;
                Initialize();
            }
        }

        private static void TryReconnect()
        {
            if (!_isReconnecting)
            {
                _isReconnecting = true;
                _lastReconnectTime = Time.realtimeSinceStartup;
                Debug.Log("[NcmLyrics] Connection lost, attempting to reconnect...");
            }
        }

        public static void UpdatePort(int port)
        {
            if (!IsInitialized || port == Port) return;

            LyricConfig.Instance.Port = port;
            _lyricManager?.UpdatePort(port);
        }

        public static int GetCurrentLineIndex()
        {
            return _lyricManager?.GetCurrentLineIndex() ?? -1;
        }

        public static string GetFormattedTime()
        {
            return _lyricManager?.GetFormattedTime() ?? "0:00/0:00";
        }

        public static float GetProgress()
        {
            return _lyricManager?.GetProgress() ?? 0f;
        }

        public static long GetCurrentTime()
        {
            return _lyricManager?.GetCurrentTime() ?? 0;
        }

        private static void Dispose()
        {
            ReleaseRenderer();
            if (_lyricManager != null)
            {
                _lyricManager.Dispose();
                _lyricManager = null;
            }
            IsInitialized = false;
            _isReconnecting = false;
            _lastUpdateTime = 0;
            OnUpdateTick = null;
        }
    }
} 