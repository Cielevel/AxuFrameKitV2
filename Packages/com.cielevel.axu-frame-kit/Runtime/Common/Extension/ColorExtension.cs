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

        // ==================== 链式修改 ====================

        /// <summary>
        /// 链式修改透明度，返回新颜色
        /// </summary>
        /// <param name="color">原颜色</param>
        /// <param name="alpha">新的透明度 (0-1)</param>
        /// <returns>修改透明度后的颜色</returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// 链式修改 RGB 分量，保持 Alpha 不变
        /// </summary>
        public static Color WithRGB(this Color color, float r, float g, float b)
        {
            return new Color(r, g, b, color.a);
        }

        // ==================== 颜色插值 ====================

        /// <summary>
        /// 从当前颜色插值到目标颜色
        /// </summary>
        /// <param name="from">起始颜色</param>
        /// <param name="to">目标颜色</param>
        /// <param name="t">插值因子 (0-1)</param>
        /// <returns>插值后的颜色</returns>
        public static Color LerpTo(this Color from, Color to, float t)
        {
            return Color.Lerp(from, to, Mathf.Clamp01(t));
        }

        /// <summary>
        /// 从当前颜色插值到目标颜色 (无钳制)
        /// </summary>
        public static Color LerpToUnclamped(this Color from, Color to, float t)
        {
            return Color.LerpUnclamped(from, to, t);
        }

        // ==================== 十六进制转换 ====================

        /// <summary>
        /// 将颜色转换为十六进制字符串
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="includeAlpha">是否包含透明度，默认 true</param>
        /// <returns>十六进制字符串，如 "#FF5500" 或 "#FF5500AA"</returns>
        public static string ToHexString(this Color color, bool includeAlpha = true)
        {
            Color32 c32 = color;
            if (includeAlpha)
                return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
            else
                return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}";
        }

        /// <summary>
        /// 从十六进制字符串解析颜色
        /// </summary>
        /// <param name="hexString">十六进制字符串，支持 "#RGB"、"#RGBA"、"#RRGGBB"、"#RRGGBBAA" 格式</param>
        /// <returns>解析后的颜色，解析失败返回白色</returns>
        public static Color FromHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return Color.white;

            // 移除 # 前缀
            if (hexString[0] == '#')
                hexString = hexString.Substring(1);

            // 根据长度解析
            switch (hexString.Length)
            {
                case 3: // RGB
                    return new Color(
                        HexToFloat(hexString[0]) / 15f,
                        HexToFloat(hexString[1]) / 15f,
                        HexToFloat(hexString[2]) / 15f);

                case 4: // RGBA
                    return new Color(
                        HexToFloat(hexString[0]) / 15f,
                        HexToFloat(hexString[1]) / 15f,
                        HexToFloat(hexString[2]) / 15f,
                        HexToFloat(hexString[3]) / 15f);

                case 6: // RRGGBB
                    return new Color32(
                        HexToByte(hexString.Substring(0, 2)),
                        HexToByte(hexString.Substring(2, 2)),
                        HexToByte(hexString.Substring(4, 2)),
                        255);

                case 8: // RRGGBBAA
                    return new Color32(
                        HexToByte(hexString.Substring(0, 2)),
                        HexToByte(hexString.Substring(2, 2)),
                        HexToByte(hexString.Substring(4, 2)),
                        HexToByte(hexString.Substring(6, 2)));

                default:
                    Debug.LogWarning($"ColorExtension.FromHexString: 无效的十六进制格式 '{hexString}'");
                    return Color.white;
            }
        }

        /// <summary>
        /// 尝试从十六进制字符串解析颜色
        /// </summary>
        /// <param name="hexString">十六进制字符串</param>
        /// <param name="color">解析结果</param>
        /// <returns>是否解析成功</returns>
        public static bool TryParseHexString(string hexString, out Color color)
        {
            color = Color.white;

            if (string.IsNullOrEmpty(hexString))
                return false;

            try
            {
                color = FromHexString(hexString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte HexToByte(string hex)
        {
            return byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }

        private static float HexToFloat(char c)
        {
            return int.Parse(c.ToString(), System.Globalization.NumberStyles.HexNumber);
        }

        // ==================== 明暗调整 ====================

        /// <summary>
        /// 调暗颜色
        /// </summary>
        /// <param name="color">原颜色</param>
        /// <param name="amount">调暗程度 (0-1)，0=不变，1=全黑</param>
        /// <returns>调暗后的颜色</returns>
        public static Color Darken(this Color color, float amount)
        {
            amount = Mathf.Clamp01(amount);
            return new Color(
                color.r * (1f - amount),
                color.g * (1f - amount),
                color.b * (1f - amount),
                color.a);
        }

        /// <summary>
        /// 调亮颜色
        /// </summary>
        /// <param name="color">原颜色</param>
        /// <param name="amount">调亮程度 (0-1)，0=不变，1=全白</param>
        /// <returns>调亮后的颜色</returns>
        public static Color Lighten(this Color color, float amount)
        {
            amount = Mathf.Clamp01(amount);
            return new Color(
                color.r + (1f - color.r) * amount,
                color.g + (1f - color.g) * amount,
                color.b + (1f - color.b) * amount,
                color.a);
        }

        /// <summary>
        /// 反转颜色 (负片效果)
        /// </summary>
        public static Color Invert(this Color color)
        {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
        }

        /// <summary>
        /// 设置饱和度
        /// </summary>
        /// <param name="color">原颜色</param>
        /// <param name="saturation">饱和度 (0=灰度, 1=原色, >1=过饱和)</param>
        /// <returns>调整后的颜色</returns>
        public static Color WithSaturation(this Color color, float saturation)
        {
            var (h, _, v) = color.ToHSV();
            return FromHSV(h, Mathf.Clamp01(saturation), v, color.a);
        }
    }
}
