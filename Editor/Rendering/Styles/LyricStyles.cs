using UnityEngine;
using UnityEditor;

namespace Yueby.NcmLyrics.Editor.Windows.Rendering
{
    public static class LyricStyles
    {
        public static class Text
        {
            private static GUIStyle _original;
            private static GUIStyle _translation;
            private static GUIStyle _romaji;
            private static GUIStyle _waiting;
            private static GUIStyle _dynamicWord;
            private static GUIStyle _dynamicWordHighlight;

            public static GUIStyle Original
            {
                get
                {
                    if (_original == null)
                    {
                        _original = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 12,
                            wordWrap = true,
                            richText = true
                        };
                    }
                    return _original;
                }
            }

            public static GUIStyle Translation
            {
                get
                {
                    if (_translation == null)
                    {
                        _translation = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 11,
                            wordWrap = true,
                            richText = true
                        };
                    }
                    return _translation;
                }
            }

            public static GUIStyle Romaji
            {
                get
                {
                    if (_romaji == null)
                    {
                        _romaji = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 10,
                            wordWrap = true,
                            richText = true
                        };
                    }
                    return _romaji;
                }
            }

            public static GUIStyle Waiting
            {
                get
                {
                    if (_waiting == null)
                    {
                        _waiting = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                        };
                    }
                    return _waiting;
                }
            }

            public static GUIStyle DynamicWord
            {
                get
                {
                    if (_dynamicWord == null)
                    {
                        _dynamicWord = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 12,
                            richText = true,
                            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                        };
                    }
                    return _dynamicWord;
                }
            }

            public static GUIStyle DynamicWordHighlight
            {
                get
                {
                    if (_dynamicWordHighlight == null)
                    {
                        _dynamicWordHighlight = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 12,
                            richText = true,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = Color.white }
                        };
                    }
                    return _dynamicWordHighlight;
                }
            }
        }

        public static class UI
        {
            private static GUIStyle _badge;
            private static GUIStyle _loading;
            private static GUIStyle _songTitle;
            private static GUIStyle _artist;
            private static GUIStyle _playState;

            public static GUIStyle Badge
            {
                get
                {
                    if (_badge == null)
                    {
                        _badge = new GUIStyle("Badge");
                    }
                    return _badge;
                }
            }

            public static GUIStyle Loading
            {
                get
                {
                    if (_loading == null)
                    {
                        _loading = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 12
                        };
                    }
                    return _loading;
                }
            }

            public static GUIStyle SongTitle
            {
                get
                {
                    if (_songTitle == null)
                    {
                        _songTitle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            fontSize = 14,
                            wordWrap = true,
                            clipping = TextClipping.Clip
                        };
                    }
                    return _songTitle;
                }
            }

            public static GUIStyle Artist
            {
                get
                {
                    if (_artist == null)
                    {
                        _artist = new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 12,
                            wordWrap = true,
                            clipping = TextClipping.Clip,
                            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                        };
                    }
                    return _artist;
                }
            }

            public static GUIStyle PlayState
            {
                get
                {
                    if (_playState == null)
                    {
                        _playState = new GUIStyle(EditorStyles.label)
                        {
                            normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f) },
                            fontSize = 12,
                            alignment = TextAnchor.MiddleRight
                        };
                    }
                    return _playState;
                }
            }
        }

        public static void UpdateColors(bool isCurrent)
        {
            if (isCurrent)
            {
                Text.Original.fontStyle = FontStyle.Bold;
                Text.Original.normal.textColor = Color.white;
                Text.Translation.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                Text.Romaji.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                Text.Waiting.normal.textColor = Color.white;
                Text.DynamicWord.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                Text.DynamicWordHighlight.normal.textColor = Color.white;
            }
            else
            {
                Text.Original.fontStyle = FontStyle.Normal;
                Text.Original.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                Text.Translation.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                Text.Romaji.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                Text.Waiting.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                Text.DynamicWord.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
                Text.DynamicWordHighlight.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }
} 