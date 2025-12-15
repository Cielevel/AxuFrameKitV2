using UnityEngine;

namespace AxuFrameKit.Common.Extension
{
    public static class ColorExtension
    {
        public static Color CalculateHDRColorByEv(Color baseColor, float exposureValue)
        {
            // 计算 2 的 exposureValue 次方
            float factor = Mathf.Pow(2, exposureValue);

            return new Color(baseColor.r * factor,
                             baseColor.g * factor,
                             baseColor.b * factor,
                             baseColor.a);
        }

        // ==========================================
        // 1. RGB (0-1) 与 HSV 的相互转换
        // ==========================================

        /// <summary>
        /// 将 Color (RGB 0-1) 转换为 HSV 元组 (H:0-1, S:0-1, V:0-1)
        /// </summary>
        public static (float h, float s, float v) ToHSV(this Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return (h, s, v);
        }

        /// <summary>
        /// 将 HSV (0-1) 转换为 Color (RGB 0-1)
        /// </summary>
        public static Color FromHSV(float h, float s, float v, float alpha = 1f)
        {
            Color c = Color.HSVToRGB(h, s, v);
            c.a = alpha;
            return c;
        }

        // ==========================================
        // 2. RGB (0-1) 与 RGB (0-255) 的相互转换
        // ==========================================

        /// <summary>
        /// 将 Color (RGB 0-1) 转换为 RGB255 元组 (int)
        /// </summary>
        public static (int r, int g, int b) ToRGB255(this Color color)
        {
            // 使用 Color32 进行转换可以避免浮点数精度导致的 254.999 变成 254 的问题
            Color32 c32 = color;
            return (c32.r, c32.g, c32.b);
        }

        /// <summary>
        /// 将 RGB255 (int) 转换为 Color (RGB 0-1)
        /// </summary>
        private static Color FromRGB255(int r, int g, int b, int a = 255)
        {
            // 自动归一化：将 0-255 映射到 0-1
            return new Color32((byte)Mathf.Clamp(r, 0, 255),
                               (byte)Mathf.Clamp(g, 0, 255),
                               (byte)Mathf.Clamp(b, 0, 255),
                               (byte)Mathf.Clamp(a, 0, 255));
        }

        // ==========================================
        // 3. HSV 与 RGB (0-255) 的直接转换 (跨级转换)
        // ==========================================

        /// <summary>
        /// 将 RGB255 转换为 HSV (0-1)
        /// </summary>
        public static (float h, float s, float v) RGB255ToHSV(int r, int g, int b)
        {
            Color c = FromRGB255(r, g, b);
            return c.ToHSV();
        }

        /// <summary>
        /// 将 HSV (0-1) 转换为 RGB255 元组
        /// </summary>
        public static (int r, int g, int b) HSVToRGB255(float h, float s, float v)
        {
            Color c = Color.HSVToRGB(h, s, v);
            return c.ToRGB255();
        }
    }
}