using UnityEngine;

namespace AxuExperimental.Noise
{
    public class NoiseVisualizer : MonoBehaviour
    {
        [Header("噪声参数")]
        [Tooltip("缩放系数：越小地形越平缓，越大地形越陡峭")]
        public float scale = 1.0f; 
    
        [Tooltip("采样数量：画多少个点")]
        public int resolution = 100;

        void OnDrawGizmos()
        {
            // 设置绘图颜色
            Gizmos.color = Color.green;

            // 上一个点的坐标（用于画线）
            Vector3 lastPoint = Vector3.zero;

            for (int i = 0; i < resolution; i++)
            {
                // 1. 计算当前的 x 坐标
                // 乘以 scale 是为了控制我们“走”得有多快
                // 如果 scale 很大，我们在噪声图上跨步就很大，起伏就会很剧烈
                float xCoord = i * scale; 

                // 2. 获取噪声高度 (0~1)
                float yValue = SimpleNoise.ValueNoise1D(xCoord);

                // 3. 构建世界坐标点
                // 我们把 i 当作实际的 x 轴位置，yValue 当作高度
                Vector3 currentPoint = new Vector3(i * 0.5f, yValue * 5f, 0);

                // 4. 只有从第二个点开始才能画线
                if (i > 0)
                {
                    Gizmos.DrawLine(lastPoint, currentPoint);
                }

                lastPoint = currentPoint;
            }
        }
    }
}