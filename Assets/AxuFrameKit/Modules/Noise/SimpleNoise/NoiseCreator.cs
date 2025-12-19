using System;

namespace AxuFrameKit.Modules.Noise.SimpleNoise
{
    /// <summary>
    /// 纯C#实现的噪声生成器，不依赖UnityEngine
    /// </summary>
    public static class NoiseCreator
    {
        // 预计算的排列表 (Permutation Table)
        private static readonly int[] P = new int[512];

        private static int[] _permutation =
        {
            151, 160, 137, 91, 90, 15,
            131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
            190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
            88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
            77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244,
            102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
            135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
            5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
            223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
            129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
            251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
            49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
            138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
        };

        private static bool _initialized = false;

        /// <summary>
        /// 使用种子初始化排列表
        /// </summary>
        public static void Init(int seed)
        {
            Random rnd = new Random(seed);

            // 洗牌算法
            int[] tempPermutation = new int[256];
            for (int i = 0; i < 256; i++) 
                tempPermutation[i] = i;

            for (int i = 0; i < 256; i++)
            {
                int j = rnd.Next(i, 256);
                (tempPermutation[i], tempPermutation[j]) = (tempPermutation[j], tempPermutation[i]);
            }

            for (int i = 0; i < 256; i++)
            {
                P[i]       = tempPermutation[i];
                P[256 + i] = tempPermutation[i];
            }

            _initialized = true;
        }

        // 默认初始化
        static NoiseCreator()
        {
            if (!_initialized) Init(12345);
        }

        /// <summary>
        /// 生成标准 Perlin Noise (范围约 -1 到 1，通常需要映射到 0-1)
        /// </summary>
        public static double Perlin(double x, double y)
        {
            // 查找单位正方形包含点的坐标
            int xPos = (int)System.Math.Floor(x) & 255;
            int yPos = (int)System.Math.Floor(y) & 255;

            // 查找在正方形中的相对 x,y
            x -= System.Math.Floor(x);
            y -= System.Math.Floor(y);

            // 计算淡入淡出曲线 (Fade curves)
            double u = Fade(x);
            double v = Fade(y);

            // 哈希正方形的8个角坐标
            int a = P[xPos] + yPos,     aa = P[a], ab = P[a + 1];
            int b = P[xPos + 1] + yPos, ba = P[b], bb = P[b + 1];

            // 混合结果
            return Lerp(v, Lerp(u, Grad(P[aa], x, y), Grad(P[ba], x - 1, y)),
                        Lerp(u, Grad(P[ab], x, y - 1), Grad(P[bb], x - 1, y - 1)));
        }

        /// <summary>
        /// 分形布朗运动 (FBM) - 用于生成多层细节的噪声 (如云、地形)
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="octaves">倍频 (层数)，越大细节越多</param>
        /// <param name="persistence">持续度，控制每层振幅衰减 (通常 0.5)</param>
        /// <param name="lacunarity">空隙度，控制每层频率增加 (通常 2.0)</param>
        /// <returns>返回 0.0 到 1.0 之间的值</returns>
        public static double FractalNoise(double x, double y, int octaves, double persistence, double lacunarity)
        {
            double total     = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue  = 0; // 用于归一化结果

            for (int i = 0; i < octaves; i++)
            {
                // Perlin 原始输出约为 -1 到 1，这里简单偏移使其尽量为正，方便叠加
                total += Perlin(x * frequency, y * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            // 归一化到 -1 到 1
            double result = total / maxValue;

            // 映射到 0 到 1
            return (result + 1.0) * 0.5;
        }

        /// <summary>
        /// 简单的白噪声
        /// </summary>
        public static double WhiteNoise(double x, double y)
        {
            // 简单的伪随机哈希
            double val = System.Math.Sin(x * 12.9898 + y * 78.233) * 43758.5453;
            return val - System.Math.Floor(val);
        }

        #region Math Helpers (Pure C#)

        private static double Fade(double t)
        {
            // 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static double Lerp(double t, double a, double b)
        {
            return a + t * (b - a);
        }

        private static double Grad(int hash, double x, double y)
        {
            // 将哈希值的低4位转换为12个梯度方向之一
            int    h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : h == 12 || h == 14 ? x : 0; // 修正以匹配原始 Perlin
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        #endregion
    }
}