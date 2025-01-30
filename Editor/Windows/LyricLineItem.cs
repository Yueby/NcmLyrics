using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Yueby.NcmLyrics.Models;

namespace Yueby.NcmLyrics.Editor.Windows
{

    public class LyricLineItem
    {
        private static GUIStyle _badgeStyle;
        private static GUIStyle _textStyle;
        private static GUIStyle _translationStyle;
        private static GUIStyle _romajiStyle;
        private static GUIStyle _waitingStyle;

        private static GUIStyle BadgeStyle
        {
            get
            {
                if (_badgeStyle == null)
                {
                    _badgeStyle = new GUIStyle(EditorStyles.helpBox);
                }
                return _badgeStyle;
            }
        }

        private static GUIStyle TextStyle
        {
            get
            {
                if (_textStyle == null)
                {
                    _textStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 14,
                        fontStyle = FontStyle.Normal,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleLeft
                    };
                }
                return _textStyle;
            }
        }

        private static GUIStyle TranslationStyle
        {
            get
            {
                if (_translationStyle == null)
                {
                    _translationStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                    };
                }
                return _translationStyle;
            }
        }

        private static GUIStyle RomajiStyle
        {
            get
            {
                if (_romajiStyle == null)
                {
                    _romajiStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 12,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                    };
                }
                return _romajiStyle;
            }
        }

        private static GUIStyle WaitingStyle
        {
            get
            {
                if (_waitingStyle == null)
                {
                    _waitingStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 14,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                }
                return _waitingStyle;
            }
        }

        private const float LINE_SPACING = 2f; // 减小行间距
        private const float PADDING_VERTICAL = 6f; // 减小垂直内边距
        private const float PADDING_HORIZONTAL = 6f; // 保持水平内边距不变

        public LyricLine Line { get; private set; }
        public bool IsEmpty { get; private set; }
        public bool IsWaiting { get; private set; }

        private float _cachedHeight; // 缓存的高度值
        public float Height => _cachedHeight; // 供外部访问的高度属性

        private float _originalHeight;
        private float _translationHeight;
        private float _romajiHeight;
        private float _contentWidth;
        private bool _isCurrent;

        private LyricLineItem(LyricLine line = null, bool isWaiting = false)
        {
            Line = line;
            IsEmpty = line == null || line.originalLyric == "";
            IsWaiting = isWaiting;
        }

        public static LyricLineItem Create(LyricLine line)
        {
            return new LyricLineItem(line);
        }

        public static LyricLineItem CreateEmpty()
        {
            return new LyricLineItem();
        }

        public static LyricLineItem CreateWaiting()
        {
            return new LyricLineItem(null, true);
        }

        public void UpdateHeight(float width, bool showTranslation, bool showRomaji)
        {
            if (IsEmpty)
            {
                _cachedHeight = 0;
                return;
            }

            _contentWidth = width - PADDING_HORIZONTAL * 2;



            if (IsWaiting)
            {
                _cachedHeight = TextStyle.CalcHeight(new GUIContent("等待歌曲开始..."), _contentWidth);
                _cachedHeight += PADDING_VERTICAL * 2;
                return;
            }

            bool hasTranslation = !string.IsNullOrEmpty(Line.translatedLyric) && showTranslation;
            bool hasRomaji = !string.IsNullOrEmpty(Line.romanLyric) && showRomaji;

            var originalLyricContent = new GUIContent(Line.originalLyric);
            var translationLyricContent = new GUIContent(Line.translatedLyric);
            var romanLyricContent = new GUIContent(Line.romanLyric);

            _originalHeight = TextStyle.CalcHeight(originalLyricContent, _contentWidth);
            _translationHeight = hasTranslation ? TranslationStyle.CalcHeight(translationLyricContent, _contentWidth) : 0;
            _romajiHeight = hasRomaji ? RomajiStyle.CalcHeight(romanLyricContent, _contentWidth) : 0;

            _cachedHeight = _originalHeight;

            if (hasTranslation)
            {
                _cachedHeight += _translationHeight + LINE_SPACING;
            }

            if (hasRomaji)
            {
                _cachedHeight += _romajiHeight + LINE_SPACING;
            }

            _cachedHeight += PADDING_VERTICAL * 2;
        }

        public void Draw(Rect rect, bool isCurrent, bool showTranslation, bool showRomaji, bool hideWaiting = false)
        {
            if (IsEmpty) return;

            // 设置样式颜色
            if (isCurrent)
            {
                TextStyle.fontStyle = FontStyle.Bold;
                TextStyle.normal.textColor = Color.white;
                TranslationStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                RomajiStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                WaitingStyle.normal.textColor = Color.white;

                UpdateHeight(rect.width, showTranslation, showRomaji);
            }
            else
            {
                TextStyle.fontStyle = FontStyle.Normal;
                TextStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                TranslationStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                RomajiStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                WaitingStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            }


            // 创建绘制区域
            var drawRect = rect;
            var badgeRect = drawRect;

            if (Event.current != null && badgeRect.Contains(Event.current.mousePosition))
            {
                GUI.Box(badgeRect, GUIContent.none, BadgeStyle);
            }

            if (IsWaiting)
            {
                if (!hideWaiting)
                {
                    GUI.Label(drawRect, "等待歌曲开始...", WaitingStyle);
                }
                return;
            }

            float currentY = rect.y + PADDING_VERTICAL;

            // 绘制原文
            GUI.Label(new Rect(rect.x + PADDING_HORIZONTAL, currentY, _contentWidth, _originalHeight),
                Line.originalLyric, TextStyle);
            currentY += _originalHeight;

            // 绘制翻译
            if (!string.IsNullOrEmpty(Line.translatedLyric) && showTranslation)
            {
                currentY += LINE_SPACING;
                GUI.Label(new Rect(rect.x + PADDING_HORIZONTAL, currentY, _contentWidth, _translationHeight),
                    Line.translatedLyric, TranslationStyle);
                currentY += _translationHeight;
            }

            // 绘制罗马音
            if (!string.IsNullOrEmpty(Line.romanLyric) && showRomaji)
            {
                currentY += LINE_SPACING;
                GUI.Label(new Rect(rect.x + PADDING_HORIZONTAL, currentY, _contentWidth, _romajiHeight),
                    Line.romanLyric, RomajiStyle);
            }
        }
    }
}