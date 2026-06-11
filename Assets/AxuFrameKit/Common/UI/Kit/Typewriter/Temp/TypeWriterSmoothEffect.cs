using TMPro;
using UnityEngine;

namespace EffectLab.EffectTypewriter
{
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterSmoothEffect : MonoBehaviour
    {
        [SerializeField] private float charInterval = 0.05f;

        private TMP_Text _label;
        private TMP_TextInfo _textInfo;
        private int _totalCharCount;

        // 当前进度，用浮点数表示
        // 整数部分 = 已完全显示的字符数
        // 小数部分 = 当前边界字符的显示进度（0~1）
        private float _progress;

        void Start()
        {
            _label = GetComponent<TMP_Text>();
            _label.ForceMeshUpdate();
            _textInfo = _label.textInfo;
            _totalCharCount = _textInfo.characterCount;

            SetAllAlpha(0);
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            _progress = 0f;
        }

        void Update()
        {
            if (_progress >= _totalCharCount) return;

            _progress += Time.deltaTime / charInterval;
            _progress = Mathf.Min(_progress, _totalCharCount);

            RefreshAlpha();
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void RefreshAlpha()
        {
            // 整数部分：已完全显示的字符数
            int fullyVisible = Mathf.FloorToInt(_progress);

            // 小数部分：当前边界字符的渐显进度（0~1）
            float edgeFraction = _progress - fullyVisible;

            for (int i = 0; i < _totalCharCount; i++)
            {
                if (i < fullyVisible)
                {
                    // 已完全显示的字符，四个顶点全部不透明
                    SetCharAlpha(i, 255, 255);
                }
                else if (i == fullyVisible)
                {
                    // 当前边界字符：左侧顶点跟随整体进度，右侧顶点滞后
                    // 用 smoothstep 让渐变更自然，避免线性插值显得太机械
                    byte leftAlpha = (byte)(Mathf.SmoothStep(0f, 1f, edgeFraction) * 255);
                    byte rightAlpha = (byte)(Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, edgeFraction - 0.5f) * 2f) * 255);
                    SetCharAlpha(i, leftAlpha, rightAlpha);
                }
                else
                {
                    // 尚未显示的字符，全部透明
                    SetCharAlpha(i, 0, 0);
                }
            }
        }

        /// <summary>
        /// 设置单个字符四个顶点的 alpha 值。
        /// 左右分开传入，用于实现从左到右的渐显效果。
        /// </summary>
        /// <param name="charIndex">字符在 characterInfo 中的下标</param>
        /// <param name="leftAlpha">左侧两个顶点（+0 左下，+1 左上）的 alpha</param>
        /// <param name="rightAlpha">右侧两个顶点（+2 右上，+3 右下）的 alpha</param>
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

        private void SetAllAlpha(byte alpha)
        {
            for (int i = 0; i < _textInfo.meshInfo.Length; i++)
            {
                Color32[] colors = _textInfo.meshInfo[i].colors32;
                for (int j = 0; j < colors.Length; j++)
                    colors[j].a = alpha;
            }
        }
    }
}