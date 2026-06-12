using UnityEngine;
using TMPro;

namespace AxuFrameKit.Common.UI.Kit.Typewriter
{
    /// <summary>
    /// 打字机效果 MonoBehaviour 组件，可直接挂载到 GameObject 上使用
    /// 功能与 TmpTypewriterItem 一致，自带 Update 驱动，无需依赖 Manager
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TmpTypewriter : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // 配置（可在 Inspector 预设）
        // ─────────────────────────────────────────────────────────────

        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _charInterval = 0.05f;
        [SerializeField] private bool _smooth;

        // ─────────────────────────────────────────────────────────────
        // 公开属性
        // ─────────────────────────────────────────────────────────────

        public float CharInterval => _charInterval;

        /// <summary>
        /// 字符逐个出现的总时长（估算值，受字符数量与间隔影响）
        /// </summary>
        public float Duration { get; private set; }

        public bool Smooth => _smooth;
        public TMP_Text Label => _label;
        public bool IsComplete { get; private set; }
        public bool IsPlaying { get; private set; }

        // ─────────────────────────────────────────────────────────────
        // 运行时状态
        // ─────────────────────────────────────────────────────────────

        private TMP_TextInfo _textInfo;
        private int _totalCharCount;

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

        // ─────────────────────────────────────────────────────────────
        // Play（与 Manager 一致的签名）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 启动打字机效果（与 TmpTypewriterManager.Play 签名一致）
        /// </summary>
        public void Play(float charInterval = 0.05f, bool smooth = false)
        {
            _charInterval = Mathf.Max(0.001f, charInterval);
            _smooth = smooth;
            Play();
        }

        /// <summary>
        /// 使用已配置的参数启动打字机效果（Inspector 中预设或代码设置后调用）
        /// </summary>
        public void Play()
        {
            if (_label == null)
            {
                IsComplete = true;
                return;
            }

            // 强制生成一次完整 mesh，之后不再重构
            _label.ForceMeshUpdate();
            _textInfo = _label.textInfo;
            _totalCharCount = _textInfo.characterCount;
            Duration = _totalCharCount * _charInterval;

            // 初始全部隐藏
            SetAllAlpha(0);
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            _progress = 0f;
            _timer = 0f;
            IsComplete = false;
            IsPlaying = true;
        }

        /// <summary>
        /// 立即完成打字机效果，显示全部字符
        /// </summary>
        public void Complete()
        {
            if (_label != null && _textInfo != null)
            {
                SetAllAlpha(255);
                _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }

            IsComplete = true;
            IsPlaying = false;
        }

        /// <summary>
        /// 停止打字机效果并重置状态
        /// </summary>
        public void Stop()
        {
            IsPlaying = false;
            IsComplete = false;
        }

        // ─────────────────────────────────────────────────────────────
        // Unity 生命周期
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!IsPlaying || IsComplete || _totalCharCount == 0)
                return;

            if (_smooth)
            {
                TickSmooth(Time.deltaTime);
            }
            else
            {
                TickNonSmooth(Time.deltaTime);
            }

            // 只更新颜色数据，不重建 mesh
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            // 检查是否全部显示完成
            if (_progress >= _totalCharCount)
            {
                IsComplete = true;
                IsPlaying = false;
            }
        }

        private void OnDestroy()
        {
            _label = null;
            _textInfo = null;
        }

        // ─────────────────────────────────────────────────────────────
        // 非平滑模式：逐字跳变
        // ─────────────────────────────────────────────────────────────

        private void TickNonSmooth(float deltaTime)
        {
            _timer += deltaTime;

            int visibleCount = Mathf.FloorToInt(_progress);

            while (_timer >= _charInterval && visibleCount < _totalCharCount)
            {
                RevealChar(visibleCount);
                visibleCount++;
                _timer -= _charInterval;
            }

            _progress = visibleCount;
        }

        /// <summary>
        /// 平滑模式：字符从左到右渐变出现
        /// </summary>
        private void TickSmooth(float deltaTime)
        {
            _progress += deltaTime / _charInterval;
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

        private void OnValidate()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();
        }
    }
}
