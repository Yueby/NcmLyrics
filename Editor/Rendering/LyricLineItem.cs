using UnityEngine;
using UnityEditor;
using Yueby.NcmLyrics.Models;
using Yueby.NcmLyrics.Editor.Data;
using Yueby.NcmLyrics.Editor.Windows.Rendering;
using System.Collections.Generic;

namespace Yueby.NcmLyrics.Editor.Windows
{
    public class LyricLineItem
    {
        #region Constants
        private const float LINE_SPACING = 2f;
        private const float PADDING_VERTICAL = 6f;
        private const float PADDING_HORIZONTAL = 6f;
        #endregion

        #region Properties
        public LyricLine Line { get; private set; }
        public bool IsEmpty { get; private set; }
        public bool IsWaiting { get; private set; }
        public float ContentWidth => _contentWidth;
        public float OriginalTextWidth { get; private set; }
        public float TranslationWidth { get; private set; }
        public float RomajiWidth { get; private set; }
        public float RequiredWidth => Mathf.Max(OriginalTextWidth, TranslationWidth, RomajiWidth) + PADDING_HORIZONTAL * 2;
        public float Height => _cachedHeight;
        #endregion

        #region Private Fields
        private float _cachedHeight;
        private float _originalHeight;
        private float _translationHeight;
        private float _romajiHeight;
        private float _contentWidth;
        private GUIStyle _cachedOriginalStyle;
        private GUIStyle _cachedTranslationStyle;
        private GUIStyle _cachedRomajiStyle;
        private GUIStyle _cachedDynamicStyle;
        private Dictionary<string, Vector2> _wordSizeCache = new Dictionary<string, Vector2>();
        #endregion

        private LyricLineItem(LyricLine line = null, bool isWaiting = false)
        {
            Line = line;
            IsEmpty = line == null || line.originalLyric == "";
            IsWaiting = isWaiting;
        }

        public static LyricLineItem Create(LyricLine line) => new LyricLineItem(line);
        public static LyricLineItem CreateEmpty() => new LyricLineItem();
        public static LyricLineItem CreateWaiting() => new LyricLineItem(null, true);

        public void CalculateTextWidths(bool noWrap = false)
        {
            if (IsEmpty)
            {
                OriginalTextWidth = 0;
                TranslationWidth = 0;
                RomajiWidth = 0;
                return;
            }

            if (IsWaiting)
            {
                OriginalTextWidth = LyricStyles.Text.Waiting.CalcSize(new GUIContent("Waiting for song...")).x;
                TranslationWidth = 0;
                RomajiWidth = 0;
                return;
            }

            // 计算原文宽度
            if (Line.dynamicLyric != null && Line.dynamicLyric.Length > 0)
            {
                OriginalTextWidth = CalcDynamicLyricWidth(_contentWidth);
            }
            else
            {
                var originalStyle = new GUIStyle(LyricStyles.Text.Original);
                if (noWrap) originalStyle.wordWrap = false;
                OriginalTextWidth = originalStyle.CalcSize(new GUIContent(Line.originalLyric)).x;
            }

            // 计算翻译和罗马音宽度
            var translationStyle = new GUIStyle(LyricStyles.Text.Translation);
            var romajiStyle = new GUIStyle(LyricStyles.Text.Romaji);

            if (noWrap)
            {
                translationStyle.wordWrap = false;
                romajiStyle.wordWrap = false;
            }

            TranslationWidth = !string.IsNullOrEmpty(Line.translatedLyric) && LyricConfig.Instance.ShowTranslation
                ? translationStyle.CalcSize(new GUIContent(Line.translatedLyric)).x
                : 0;
            RomajiWidth = !string.IsNullOrEmpty(Line.romanLyric) && LyricConfig.Instance.ShowRomaji
                ? romajiStyle.CalcSize(new GUIContent(Line.romanLyric)).x
                : 0;
        }

        private float CalcDynamicLyricWidth(float contentWidth)
        {
            if (Line.dynamicLyric == null || Line.dynamicLyric.Length == 0)
                return 0;

            float maxScale = 1.0f + 0.15f; // 最大缩放比例
            float totalWidth = 0;
            float lineWidth = 0;
            var dynamicStyle = GetCachedStyle(LyricStyles.Text.DynamicWord, ref _cachedDynamicStyle);

            foreach (var word in Line.dynamicLyric)
            {
                if (string.IsNullOrEmpty(word.word)) continue;

                var size = GetCachedWordSize(word.word, dynamicStyle);
                float wordWidth = size.x * maxScale; // 使用最大缩放比例计算宽度

                // 如果这个词加上去会超出宽度，就换行
                if (lineWidth + wordWidth > contentWidth)
                {
                    totalWidth = Mathf.Max(totalWidth, lineWidth);
                    lineWidth = wordWidth;
                }
                else
                {
                    lineWidth += wordWidth;
                }
            }

            return Mathf.Max(totalWidth, lineWidth);
        }

        private float CalcDynamicLyricHeight(float width)
        {
            if (Line.dynamicLyric == null || Line.dynamicLyric.Length == 0)
                return 0;

            float x = 0;
            float y = 0;
            var dynamicStyle = GetCachedStyle(LyricStyles.Text.DynamicWord, ref _cachedDynamicStyle);
            float maxScale = 1.15f; // 与DrawDynamicLyric中的最大缩放一致 (1.0f + 0.15f)

            foreach (var word in Line.dynamicLyric)
            {
                var content = new GUIContent(word.word);
                var originalSize = GetCachedWordSize(word.word, dynamicStyle);
                var scaledSize = new Vector2(originalSize.x * maxScale, originalSize.y * maxScale);

                // 检查换行，与DrawDynamicLyric保持一致
                if (x + scaledSize.x > width)
                {
                    x = 0;
                    y += dynamicStyle.lineHeight * 1.2f;
                }
                x += originalSize.x;
            }

            return y + dynamicStyle.lineHeight * maxScale;
        }

        private void ClearCache()
        {
            _cachedOriginalStyle = null;
            _cachedTranslationStyle = null;
            _cachedRomajiStyle = null;
            _cachedDynamicStyle = null;
            _wordSizeCache.Clear();
        }

        private GUIStyle GetCachedStyle(GUIStyle baseStyle, ref GUIStyle cachedStyle)
        {
            if (cachedStyle == null)
            {
                cachedStyle = new GUIStyle(baseStyle);
            }
            return cachedStyle;
        }

        private Vector2 GetCachedWordSize(string word, GUIStyle style)
        {
            string key = $"{word}_{style.fontSize}_{style.font?.name}";
            if (!_wordSizeCache.TryGetValue(key, out Vector2 size))
            {
                size = style.CalcSize(new GUIContent(word));
                _wordSizeCache[key] = size;
            }
            return size;
        }

        public void UpdateHeight(float width)
        {
            if (IsEmpty)
            {
                _cachedHeight = 0;
                return;
            }

            // 如果宽度没有变化，使用缓存的高度
            if (Mathf.Approximately(_contentWidth, width - PADDING_HORIZONTAL * 2))
            {
                return;
            }

            _contentWidth = width - PADDING_HORIZONTAL * 2;

            if (IsWaiting)
            {
                var waitingStyle = GetCachedStyle(LyricStyles.Text.Waiting, ref _cachedOriginalStyle);
                _cachedHeight = waitingStyle.CalcSize(new GUIContent("Waiting for song...")).x;
                _cachedHeight += PADDING_VERTICAL * 2;
                return;
            }

            bool hasTranslation = !string.IsNullOrEmpty(Line.translatedLyric) && LyricConfig.Instance.ShowTranslation;
            bool hasRomaji = !string.IsNullOrEmpty(Line.romanLyric) && LyricConfig.Instance.ShowRomaji;

            // 计算原文高度
            if (Line.dynamicLyric != null && Line.dynamicLyric.Length > 0)
            {
                _originalHeight = CalcDynamicLyricHeight(_contentWidth);
            }
            else
            {
                var originalStyle = GetCachedStyle(LyricStyles.Text.Original, ref _cachedOriginalStyle);
                var originalLyricContent = new GUIContent(Line.originalLyric);
                _originalHeight = originalStyle.CalcHeight(originalLyricContent, _contentWidth);
            }

            // 计算翻译和罗马音高度
            if (hasTranslation)
            {
                var translationStyle = GetCachedStyle(LyricStyles.Text.Translation, ref _cachedTranslationStyle);
                var translationLyricContent = new GUIContent(Line.translatedLyric);
                _translationHeight = translationStyle.CalcHeight(translationLyricContent, _contentWidth);
            }
            else
            {
                _translationHeight = 0;
            }

            if (hasRomaji)
            {
                var romajiStyle = GetCachedStyle(LyricStyles.Text.Romaji, ref _cachedRomajiStyle);
                var romanLyricContent = new GUIContent(Line.romanLyric);
                _romajiHeight = romajiStyle.CalcHeight(romanLyricContent, _contentWidth);
            }
            else
            {
                _romajiHeight = 0;
            }

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

        public void Draw(Rect rect, bool isCurrent, bool hideWaiting = false, bool isSceneView = false)
        {
            if (IsEmpty) return;

            LyricStyles.UpdateColors(isCurrent);

            var drawRect = rect;
            var badgeRect = drawRect;

            if (isSceneView || (Event.current != null && badgeRect.Contains(Event.current.mousePosition)))
            {
                GUI.Box(badgeRect, GUIContent.none, LyricStyles.UI.Badge);
            }

            if (IsWaiting)
            {
                if (!hideWaiting)
                {
                    GUI.Label(drawRect, "Waiting for song...", LyricStyles.Text.Waiting);
                }
                return;
            }

            float currentY = rect.y + PADDING_VERTICAL;

            // 绘制逐字歌词或整行歌词
            if (Line.dynamicLyric != null && Line.dynamicLyric.Length > 0)
            {
                DrawDynamicLyric(new Rect(rect.x + PADDING_HORIZONTAL, currentY, _contentWidth, _originalHeight),
                    Line, LyricService.GetCurrentTime());
            }
            else
            {
                GUI.Label(new Rect(rect.x + PADDING_HORIZONTAL, currentY, _contentWidth, _originalHeight),
                    Line.originalLyric, LyricStyles.Text.Original);
            }
            currentY += _originalHeight;

            if (!string.IsNullOrEmpty(Line.translatedLyric) && LyricConfig.Instance.ShowTranslation)
            {
                currentY += LINE_SPACING;
                GUI.Label(new Rect(rect.x + PADDING_HORIZONTAL, currentY, _contentWidth, _translationHeight),
                    Line.translatedLyric, LyricStyles.Text.Translation);
                currentY += _translationHeight;
            }

            if (!string.IsNullOrEmpty(Line.romanLyric) && LyricConfig.Instance.ShowRomaji)
            {
                currentY += LINE_SPACING;
                GUI.Label(new Rect(rect.x + PADDING_HORIZONTAL, currentY, _contentWidth, _romajiHeight),
                    Line.romanLyric, LyricStyles.Text.Romaji);
            }
        }

        private void DrawDynamicLyric(Rect rect, LyricLine line, float smoothProgress)
        {
            float x = rect.x;
            float y = rect.y;

            // 找到当前播放的字和下一个字
            int currentWordIndex = -1;
            DynamicLyricWord currentWord = null;
            DynamicLyricWord nextWord = null;
            float lastEndTime = 0;

            // 优化当前字的查找
            for (int i = line.dynamicLyric.Length - 1; i >= 0; i--)
            {
                var word = line.dynamicLyric[i];
                if (smoothProgress >= word.time)
                {
                    currentWordIndex = i;
                    currentWord = word;
                    lastEndTime = word.time + word.duration;
                    
                    if (i + 1 < line.dynamicLyric.Length)
                    {
                        nextWord = line.dynamicLyric[i + 1];
                    }
                    break;
                }
            }

            var dynamicStyle = GetCachedStyle(LyricStyles.Text.DynamicWord, ref _cachedDynamicStyle);
            var highlightColor = LyricStyles.Text.DynamicWordHighlight.normal.textColor;
            var normalColor = dynamicStyle.normal.textColor;

            // 渲染每个字
            for (int i = 0; i < line.dynamicLyric.Length; i++)
            {
                var word = line.dynamicLyric[i];
                float alpha = 0.0f;
                
                // 优化高亮度计算
                if (i < currentWordIndex)
                {
                    alpha = 1.0f;
                }
                else if (i == currentWordIndex && currentWord != null)
                {
                    float wordProgress = (smoothProgress - currentWord.time) / (float)currentWord.duration;
                    wordProgress = Mathf.Clamp01(wordProgress);
                    
                    if (smoothProgress >= lastEndTime)
                    {
                        alpha = 1.0f;
                    }
                    else
                    {
                        alpha = wordProgress;
                    }
                }
                else if (i == currentWordIndex + 1 && nextWord != null && LyricService.CurrentPlayState?.IsPlaying == true)
                {
                    float timeToNext = nextWord.time - smoothProgress;
                    if (timeToNext < 50) // 减小预热时间以提高响应速度
                    {
                        alpha = 0.3f * (1 - timeToNext / 50f);
                    }
                }

                // 使用缓存获取字体大小
                var content = new GUIContent(word.word);
                var originalSize = GetCachedWordSize(word.word, dynamicStyle);
                
                // 优化缩放计算
                float scale = 1.0f + (0.15f * alpha);
                var scaledSize = new Vector2(originalSize.x * scale, originalSize.y * scale);

                // 检查换行
                if (x + scaledSize.x > rect.x + rect.width)
                {
                    x = rect.x;
                    y += dynamicStyle.lineHeight * 1.2f;
                }

                // 计算偏移
                float xOffset = (scaledSize.x - originalSize.x) * 0.5f;
                float yOffset = (scaledSize.y - originalSize.y) * 0.5f;

                // 创建样式和颜色
                var style = new GUIStyle(dynamicStyle);
                style.normal.textColor = Color.Lerp(normalColor, highlightColor, alpha);

                // 绘制文字
                var drawRect = new Rect(x - xOffset, y - yOffset, scaledSize.x, scaledSize.y);
                GUI.Label(drawRect, content, style);
                
                x += originalSize.x;
            }
        }
    }
} 