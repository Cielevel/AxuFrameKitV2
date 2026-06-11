using TMPro;
using UnityEngine;

namespace EffectLab.EffectTypewriter
{
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        private TMP_Text _label;
        private TMP_TextInfo _textInfo;
        private int _totalCharCount;
        private int _visibleCount;
        private float _timer;

        [SerializeField] private float charInterval = 0.05f;

        private void Start()
        {
            _label = GetComponent<TMP_Text>();

            // 强制生成一次完整 mesh，之后不再重构
            _label.ForceMeshUpdate();
            _textInfo = _label.textInfo;
            _totalCharCount = _textInfo.characterCount;

            // 初始全部隐藏：将所有字符顶点 alpha 设为 0
            SetAllAlpha(0);
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            _visibleCount = 0;
        }

        private void Update()
        {
            if (_visibleCount >= _totalCharCount) return;

            _timer += Time.deltaTime;
            while (_timer >= charInterval && _visibleCount < _totalCharCount)
            {
                RevealChar(_visibleCount);
                _visibleCount++;
                _timer -= charInterval;
            }

            // 关键：只更新颜色数据，不重建 mesh
            _label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void RevealChar(int charIndex)
        {
            // 通过字符下标取得该字符的描述信息
            // characterInfo 与原始字符串一一对应，包含空格、换行等不可见字符
            var charInfo = _textInfo.characterInfo[charIndex];

            // isVisible 为 false 表示该字符在 mesh 中没有分配顶点（空格、换行、富文本标签等）
            // 此时 vertexIndex 无意义，必须跳过，否则会写入错误的顶点位置
            if (!charInfo.isVisible) return;

            // materialReferenceIndex：该字符所属子 mesh 的下标
            // 多材质场景下（如普通字体混用 emoji），不同字符可能属于不同的 meshInfo
            int meshIndex = charInfo.materialReferenceIndex;

            // vertexIndex：该字符的 quad 在其所属子 mesh 顶点数组中的起始下标
            // 注意这是局部下标（相对于对应的 meshInfo），不是全局顶点下标
            int vertexIndex = charInfo.vertexIndex;

            // 取得该子 mesh 的顶点颜色缓冲区（托管内存，修改后需 UpdateVertexData 才会提交）
            Color32[] colors = _textInfo.meshInfo[meshIndex].colors32;

            // 每个字符对应一个 quad，固定由 4 个顶点组成，vertexIndex 是左下角顶点的起始位置
            // +0 左下  +1 左上  +2 右上  +3 右下
            // 将四个顶点的 alpha 全部设为不透明，使该字符在视觉上显现
            colors[vertexIndex + 0].a = 255;
            colors[vertexIndex + 1].a = 255;
            colors[vertexIndex + 2].a = 255;
            colors[vertexIndex + 3].a = 255;
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