using UnityEngine;

namespace AxuFrameKit.Common.Extension
{
    /// <summary>
    /// 为 Color 提供 HDR 计算以及 RGB、RGB255、HSV 之间的相互转换扩展方法。
    /// </summary>
    public static class ColorExtension
    {
        /// <summary>
        /// 根据曝光值（EV）计算 HDR 颜色。颜色分量乘以 2^exposureValue。
        /// </summary>
        /// <param name="baseColor">基础颜色（RGB 0-1）。</param>
        /// <param name="exposureValue">曝光值，可正可负。</param>
        /// <returns>返回计算后的 HDR 颜色，保持 Alpha 不变。</returns>
        public static Color CalculateHDRColorByEv(this Color baseColor, float exposureValue)
        {
            float factor = Mathf.Pow(2f, exposureValue);
            return new Color(baseColor.r * factor, baseColor.g * factor, baseColor.b * factor, baseColor.a);
        }

        // ==================== RGB (0-1) <-> HSV (0-1) ====================
        /// <summary>
        /// 将 RGB (0-1) 颜色转换为 HSV (0-1)。
        /// </summary>
        /// <param name="color">源颜色。</param>
        /// <returns>返回 (h, s, v) 元组，范围均为 0-1。</returns>
        public static (float h, float s, float v) ToHSV(this Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return (h, s, v);
        }

        /// <summary>
        /// 根据 HSV (0-1) 创建颜色。
        /// </summary>
        /// <param name="h">色相。</param>
        /// <param name="s">饱和度。</param>
        /// <param name="v">明度。</param>
        /// <param name="alpha">透明度，默认 1。</param>
        /// <returns>返回对应的 Color。</returns>
        public static Color FromHSV(float h, float s, float v, float alpha = 1f)
        {
            Color c = Color.HSVToRGB(h, s, v);
            c.a = alpha;
            return c;
        }

        // ==================== RGB (0-1) <-> RGB255 (0-255) ====================
        /// <summary>
        /// 将 RGB (0-1) 颜色转换为 8 位整数 RGB255。
        /// </summary>
        /// <param name="color">源颜色。</param>
        /// <returns>返回 (r, g, b) 元组，范围 0-255。</returns>
        public static (int r, int g, int b) ToRGB255(this Color color)
        {
            Color32 c32 = color;
            return (c32.r, c32.g, c32.b);
        }

        /// <summary>
        /// 将 RGB255 (0-255) 转换为 Color (0-1)。
        /// </summary>
        /// <param name="r">红色分量。</param>
        /// <param name="g">绿色分量。</param>
        /// <param name="b">蓝色分量。</param>
        /// <param name="a">透明度分量，默认 255。</param>
        /// <returns>返回 0-1 范围的 Color。</returns>
        public static Color FromRGB255(int r, int g, int b, int a = 255)
        {
            return new Color32(
                (byte)Mathf.Clamp(r, 0, 255),
                (byte)Mathf.Clamp(g, 0, 255),
                (byte)Mathf.Clamp(b, 0, 255),
                (byte)Mathf.Clamp(a, 0, 255));
        }

        // ==================== HSV <-> RGB255 ====================
        /// <summary>
        /// 将 RGB255 转换为 HSV (0-1)。
        /// </summary>
        public static (float h, float s, float v) RGB255ToHSV(int r, int g, int b)
        {
            Color c = FromRGB255(r, g, b);
            return c.ToHSV();
        }

        /// <summary>
        /// 将 HSV (0-1) 转换为 RGB255。
        /// </summary>
        public static (int r, int g, int b) HSVToRGB255(float h, float s, float v)
        {
            Color c = Color.HSVToRGB(h, s, v);
            return c.ToRGB255();
        }
    }
}