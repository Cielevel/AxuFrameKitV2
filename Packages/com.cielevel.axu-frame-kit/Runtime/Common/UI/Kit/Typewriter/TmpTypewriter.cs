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
        /// Reveal 开始前的原始顶点色快照（按 submesh 分组深拷贝）
        /// 用于保留富文本 <color>/<alpha> 语义：完成/停止时恢复，打字过程中以此为基准缩放 alpha
        /// </summary>
        private Color32[][] _originalColors;

        /// <summary>
        /// 捕获快照时的文本，用于检测播放期间外部是否修改了 _label.text
        /// </summary>
        private string _capturedText;

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

            // Reveal 开始前捕获原始顶点色与文本状态，之后只在此基础上改 alpha
            CaptureState();
            Duration = _totalCharCount * _charInterval;

            // 空文本：没有可显示的字符，直接完成
            if (_totalCharCount == 0)
            {
                IsComplete = true;
                IsPlaying = false;
                return;
            }

            // 初始全部隐藏（只清 alpha 通道，不动 RGB）
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
                // 恢复捕获时的原始顶点色，而不是强写全不透明（保留富文本 alpha 语义）
                RestoreOriginalColors();
                _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }

            IsComplete = true;
            IsPlaying = false;
        }

        /// <summary>
        /// 停止打字机效果并重置状态（恢复原始顶点色，避免残留半隐藏的 alpha）
        /// </summary>
        public void Stop()
        {
            if (_label != null && _textInfo != null)
            {
                RestoreOriginalColors();
                _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }

            IsPlaying = false;
            IsComplete = false;
        }

        // ─────────────────────────────────────────────────────────────
        // Unity 生命周期
        // ─────────────────────────────────────────────────────────────

        private void Update()
        {
            // Label 已销毁（Unity 假 null）：无法继续驱动，直接结束
            if (_label == null)
            {
                IsPlaying = false;
                IsComplete = true;
                return;
            }

            if (!IsPlaying || IsComplete || _totalCharCount == 0)
                return;

            // 播放中外部修改了文本：旧 textInfo 数组已失效，重新捕获并按当前进度重放 alpha
            if (_label.text != _capturedText)
            {
                CaptureState();

                // 新文本可能比当前进度短，收敛进度避免越界
                if (_progress > _totalCharCount)
                    _progress = _totalCharCount;

                if (_totalCharCount == 0)
                {
                    Complete();
                    return;
                }

                ReplayAlpha();
                _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }

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
            _originalColors = null;
            _capturedText = null;
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
        /// 逐字跳变：瞬间显示一个字符的全部四个顶点（恢复到捕获时的原始 alpha）
        /// </summary>
        private void RevealChar(int charIndex)
        {
            var charInfo = _textInfo.characterInfo[charIndex];

            // 不可见字符（空格、换行、富文本标签等）跳过
            if (!charInfo.isVisible) return;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] colors = _textInfo.meshInfo[meshIndex].colors32;
            Color32[] origin = _originalColors[meshIndex];

            colors[vertexIndex + 0].a = origin[vertexIndex + 0].a; // 左下
            colors[vertexIndex + 1].a = origin[vertexIndex + 1].a; // 左上
            colors[vertexIndex + 2].a = origin[vertexIndex + 2].a; // 右上
            colors[vertexIndex + 3].a = origin[vertexIndex + 3].a; // 右下
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
        /// 在捕获的原始 alpha 基础上按进度缩放，只动 alpha 通道不动 RGB（保留富文本颜色语义）
        /// </summary>
        /// <param name="charIndex">字符在 characterInfo 中的下标</param>
        /// <param name="leftAlpha">左侧两个顶点（左下 +0，左上 +1）的显示进度（0~255）</param>
        /// <param name="rightAlpha">右侧两个顶点（右上 +2，右下 +3）的显示进度（0~255）</param>
        private void SetCharAlpha(int charIndex, byte leftAlpha, byte rightAlpha)
        {
            var charInfo = _textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) return;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] colors = _textInfo.meshInfo[meshIndex].colors32;
            Color32[] origin = _originalColors[meshIndex];

            colors[vertexIndex + 0].a = ScaleAlpha(origin[vertexIndex + 0].a, leftAlpha);   // 左下
            colors[vertexIndex + 1].a = ScaleAlpha(origin[vertexIndex + 1].a, leftAlpha);   // 左上
            colors[vertexIndex + 2].a = ScaleAlpha(origin[vertexIndex + 2].a, rightAlpha);  // 右上
            colors[vertexIndex + 3].a = ScaleAlpha(origin[vertexIndex + 3].a, rightAlpha);  // 右下
        }

        /// <summary>
        /// 原始 alpha × 显示进度（均为 0~255，等比缩放）
        /// </summary>
        private static byte ScaleAlpha(byte origin, byte progress)
        {
            return (byte)(origin * progress / 255);
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
        // 顶点色快照（保留富文本 <color>/<alpha> 语义）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 强制重建 mesh 并捕获当前顶点色快照与文本状态
        /// </summary>
        private void CaptureState()
        {
            _label.ForceMeshUpdate();
            _textInfo = _label.textInfo;
            _totalCharCount = _textInfo.characterCount;
            _capturedText = _label.text;

            // 深拷贝各 submesh 的顶点色（colors32 是 TMP 内部的活数组，必须复制副本）
            _originalColors = new Color32[_textInfo.meshInfo.Length][];
            for (int i = 0; i < _textInfo.meshInfo.Length; i++)
            {
                var src = _textInfo.meshInfo[i].colors32;
                _originalColors[i] = new Color32[src.Length];
                for (int j = 0; j < src.Length; j++)
                    _originalColors[i][j] = src[j];
            }
        }

        /// <summary>
        /// 将顶点色恢复为捕获快照时的原始值（整份 Color32，含 RGB）
        /// </summary>
        private void RestoreOriginalColors()
        {
            if (_originalColors == null) return;

            for (int i = 0; i < _textInfo.meshInfo.Length && i < _originalColors.Length; i++)
            {
                var colors = _textInfo.meshInfo[i].colors32;
                var origin = _originalColors[i];
                for (int j = 0; j < colors.Length && j < origin.Length; j++)
                    colors[j] = origin[j];
            }
        }

        /// <summary>
        /// 按当前进度重放所有字符的 alpha（用于外部改文本后重建状态）
        /// </summary>
        private void ReplayAlpha()
        {
            SetAllAlpha(0);

            if (_smooth)
            {
                RefreshAlpha(); // 平滑模式全量重刷（含边界渐变）
            }
            else
            {
                int visibleCount = Mathf.Min(Mathf.FloorToInt(_progress), _totalCharCount);
                for (int i = 0; i < visibleCount; i++)
                    RevealChar(i);
            }
        }

        private void OnValidate()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();
        }
    }
}
