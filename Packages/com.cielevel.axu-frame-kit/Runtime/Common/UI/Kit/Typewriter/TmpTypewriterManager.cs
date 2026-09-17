using TMPro;
using UnityEngine;
using AxuFrameKit.Common.Utility.Multiton;

namespace AxuFrameKit.Common.UI.Kit.Typewriter
{
    // 2026-6-11 
    // 补充：
    // 1.实现基于字符的回调（比如显示完成某个char后/到达某一个char后，调用一次回调）
    // 2.每显示一个字符或者N个字符（按数量），调用一次回调
    // 效果预览：打字机声音（每一个空格播放一次/每N个字符播放一次）
    public class TmpTypewriterManager : MultitonManager<TmpTypewriterManager, TmpTypewriterItem>
    {
        /// <summary>
        /// 快捷注册：对目标 TMP_Text 启动打字机效果
        /// </summary>
        public TmpTypewriterItem Play(TMP_Text label, float charInterval = 0.05f, bool smooth = false)
        {
            var item = new TmpTypewriterItem();
            item.Setup(label, charInterval, smooth);
            Register(item);
            return item;
        }
    }

    public class TmpTypewriterItem : IMultitonManagerItem
    {
        // ─────────────────────────────────────────────────────────────
        // 配置
        // ─────────────────────────────────────────────────────────────

        public float CharInterval { get; private set; } = 0.05f;

        /// <summary>
        /// 字符逐个出现的总时长（估算值，受字符数量与间隔影响）
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 是否启用平滑渐变模式（关闭即逐字跳变）
        /// </summary>
        public bool Smooth { get; private set; }

        // ─────────────────────────────────────────────────────────────
        // 引用
        // ─────────────────────────────────────────────────────────────

        public TMP_Text Label { get; private set; }
        private TMP_TextInfo _textInfo;
        private int _totalCharCount;

        // ─────────────────────────────────────────────────────────────
        // 运行时状态
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 当前进度（浮点）
        /// - 非 Smooth 模式：整数部分 = 已完全显示的字符数，小数部分恒为 0
        /// - Smooth 模式：整数部分 = 已完全显示的字符数，小数部分 = 边界字符的渐显进度（0~1）
        /// </summary>
        private float _progress;

        /// <summary>
        /// 非 Smooth 模式下的累计计时器，用于控制逐字间隔
        /// </summary>
        private float _timer;

        private bool _isComplete;

        public bool IsComplete => _isComplete;

        // ─────────────────────────────────────────────────────────────
        // 初始化
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 配置打字机参数（Register 前调用）
        /// </summary>
        public void Setup(TMP_Text label, float charInterval = 0.05f, bool smooth = false)
        {
            Label = label;
            CharInterval = Mathf.Max(0.001f, charInterval);
            Smooth = smooth;
        }

        public void OnCreate()
        {
            if (Label == null)
            {
                _isComplete = true;
                return;
            }

            // 强制生成一次完整 mesh，之后不再重构
            Label.ForceMeshUpdate();
            _textInfo = Label.textInfo;
            _totalCharCount = _textInfo.characterCount;
            Duration = _totalCharCount * CharInterval;

            // 空文本：没有可显示的字符，直接置完成，避免条目永久注册在 Manager 中
            if (_totalCharCount == 0)
            {
                _isComplete = true;
                return;
            }

            // 初始全部隐藏
            SetAllAlpha(0);
            Label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            _progress = 0f;
            _timer = 0f;
            _isComplete = false;
        }

        // ─────────────────────────────────────────────────────────────
        // 每帧驱动
        // ─────────────────────────────────────────────────────────────

        public void Tick(float deltaTime)
        {
            // Label 已销毁（Unity 假 null）：置完成让 Manager 回收本条目，避免每帧 MissingReferenceException
            if (Label == null)
            {
                _isComplete = true;
                return;
            }

            if (_isComplete || _totalCharCount == 0)
                return;

            if (Smooth)
            {
                TickSmooth(deltaTime);
            }
            else
            {
                TickNonSmooth(deltaTime);
            }

            // 只更新颜色数据，不重建 mesh
            Label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            // 检查是否全部显示完成
            if (_progress >= _totalCharCount)
            {
                _isComplete = true;
            }
        }

        /// <summary>
        /// 非平滑模式：逐字跳变
        /// </summary>
        private void TickNonSmooth(float deltaTime)
        {
            _timer += deltaTime;

            int visibleCount = Mathf.FloorToInt(_progress);

            while (_timer >= CharInterval && visibleCount < _totalCharCount)
            {
                RevealChar(visibleCount);
                visibleCount++;
                _timer -= CharInterval;
            }

            _progress = visibleCount;
        }

        /// <summary>
        /// 平滑模式：字符从左到右渐变出现
        /// </summary>
        private void TickSmooth(float deltaTime)
        {
            _progress += deltaTime / CharInterval;
            _progress = Mathf.Min(_progress, _totalCharCount);

            RefreshAlpha();
        }

        // ─────────────────────────────────────────────────────────────
        // 字符显隐控制
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 逐字跳变：瞬间显示一个字符的全部四个顶点
        /// </summary>
        private void RevealChar(int charIndex)
        {
            var charInfo = _textInfo.characterInfo[charIndex];

            // 不可见字符（空格、换行、富文本标签等）跳过
            if (!charInfo.isVisible) return;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] colors = _textInfo.meshInfo[meshIndex].colors32;

            colors[vertexIndex + 0].a = 255; // 左下
            colors[vertexIndex + 1].a = 255; // 左上
            colors[vertexIndex + 2].a = 255; // 右上
            colors[vertexIndex + 3].a = 255; // 右下
        }

        /// <summary>
        /// 平滑模式：刷新所有字符的 alpha
        /// 已完全显示的字符 → 全不透明
        /// 当前边界字符 → 左侧顶点先于右侧顶点变不透明（从左到右渐变）
        /// 尚未显示的字符 → 全透明
        /// </summary>
        private void RefreshAlpha()
        {
            int fullyVisible = Mathf.FloorToInt(_progress);
            float edgeFraction = _progress - fullyVisible;

            for (int i = 0; i < _totalCharCount; i++)
            {
                if (i < fullyVisible)
                {
                    SetCharAlpha(i, 255, 255);
                }
                else if (i == fullyVisible)
                {
                    // 用 smoothstep 让渐变更自然
                    byte leftAlpha = (byte)(Mathf.SmoothStep(0f, 1f, edgeFraction) * 255);
                    byte rightAlpha = (byte)(Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, edgeFraction - 0.5f) * 2f) * 255);
                    SetCharAlpha(i, leftAlpha, rightAlpha);
                }
                else
                {
                    SetCharAlpha(i, 0, 0);
                }
            }
        }

        /// <summary>
        /// 设置单个字符四个顶点的 alpha
        /// </summary>
        /// <param name="charIndex">字符在 characterInfo 中的下标</param>
        /// <param name="leftAlpha">左侧两个顶点（左下 +0，左上 +1）的 alpha</param>
        /// <param name="rightAlpha">右侧两个顶点（右上 +2，右下 +3）的 alpha</param>
        private void SetCharAlpha(int charIndex, byte leftAlpha, byte rightAlpha)
        {
            var charInfo = _textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) return;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] colors = _textInfo.meshInfo[meshIndex].colors32;

            colors[vertexIndex + 0].a = leftAlpha;   // 左下
            colors[vertexIndex + 1].a = leftAlpha;   // 左上
            colors[vertexIndex + 2].a = rightAlpha;  // 右上
            colors[vertexIndex + 3].a = rightAlpha;  // 右下
        }

        /// <summary>
        /// 将所有字符顶点 alpha 设为同一值
        /// </summary>
        private void SetAllAlpha(byte alpha)
        {
            for (int i = 0; i < _textInfo.meshInfo.Length; i++)
            {
                Color32[] colors = _textInfo.meshInfo[i].colors32;
                for (int j = 0; j < colors.Length; j++)
                    colors[j].a = alpha;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 生命周期回调
        // ─────────────────────────────────────────────────────────────

        public void OnComplete()
        {
            // 确保所有字符完全可见
            if (Label != null && _textInfo != null)
            {
                SetAllAlpha(255);
                Label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
        }

        public void OnDispose()
        {
            Label = null;
            _textInfo = null;
        }
    }
}