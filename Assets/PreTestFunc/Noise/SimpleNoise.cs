using UnityEngine;

namespace PreTestFunc.Noise
{
    public static class SimpleNoise
    {
        // 1. 哈希函数 (The Blender)
        // 输入一个整数，返回 0.0 到 1.0 之间的确定性随机数
        // 这是一个经典的位运算哈希算法，虽然看起来像乱码，但它效率极高且分布均匀
        public static float GetRandomValue(int seed)
        {
            // 这里的数字是大质数，用于打乱比特位 (Bit-mangling)
            seed = (seed << 13) ^ seed;
            int t = (seed * (seed * seed * 15731 + 789221) + 1376312589) & 0x7fffffff;
        
            // 将巨大的整数压缩到 0~1 之间
            return 1.0f - t / 1073741824.0f; 
        }

        // 2. 平滑函数 (S-Curve)
        // 输入 0~1 的进度 t，输出“缓入缓出”后的 t
        // 公式：t * t * (3 - 2 * t)
        public static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        // 3. 线性插值 (Lerp)
        // 在 a 和 b 之间，根据 t 进行混合
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        // 4. 组装：一维值噪声主函数
        public static float ValueNoise1D(float x)
        {
            // [A] 拆分坐标
            // 比如 x = 5.3
            // floorX = 5 (左边的整数格子)
            // fractX = 0.3 (在格子里的相对位置)
            int   floorX = Mathf.FloorToInt(x);
            float fractX = x - floorX;

            // [B] 获取两头的随机值 (哈希)
            // 这里的 floorX 就是我们的“种子”
            float leftValue  = GetRandomValue(floorX);     // 左边柱子的高度
            float rightValue = GetRandomValue(floorX + 1); // 右边柱子的高度

            // [C] 平滑进度
            // 把线性的 0.3 变成平滑的 S 曲线进度
            float smoothT = SmoothStep(fractX);

            // [D] 插值
            // 根据平滑后的进度，混合左右高度
            return Lerp(leftValue, rightValue, smoothT);
        }
        
        // 4. 组装：二维值噪声主函数
        public static float ValueNoise2D(float x, float y)
        {
            // [A] 拆分坐标
            int   floorX = Mathf.FloorToInt(x);
            int   floorY = Mathf.FloorToInt(y);
            float fractX = x - floorX;
            float fractY = y - floorY;

            // [B] 获取四个角的随机值 (哈希)
            float v00 = GetRandomValue(floorX     + floorY * 57);
            float v10 = GetRandomValue(floorX + 1 + floorY * 57);
            float v01 = GetRandomValue(floorX     + (floorY + 1) * 57);
            float v11 = GetRandomValue(floorX + 1 + (floorY + 1) * 57);

            // [C] 平滑进度
            float smoothX = SmoothStep(fractX);
            float smoothY = SmoothStep(fractY);

            // [D] 插值
            float ix0 = Lerp(v00, v10, smoothX); // 上边插值
            float ix1 = Lerp(v01, v11, smoothX); // 下边插值
            return Lerp(ix0, ix1, smoothY);      // 最终插值
        }
    }
}