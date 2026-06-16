using System;
using AxuFrameKit._Core.Extension;

namespace AxuFrameKit._Core.Utility.RandomX
{
    /// <summary>
    /// 随机头像URL生成器；该代码创建于2026-02-07，不保证链接有效性，请使用前进行检查。
    /// </summary>
    public static class RandomAvatarUrl
    {
        private static readonly RandomX _randomX = new(777);

        #region Robot头像

        private const string RobotAvatarUrl = @"https://robohash.org/{0}.png";

        /// <summary>
        /// 获取随机机器人头像URL
        /// </summary>
        public static string GetRobot()
        {
            const int seedLen = 6;
            return string.Format(RobotAvatarUrl, _randomX.Seed(seedLen));
        }

        /// <summary>
        /// 获取指定种子字符串的机器人头像URL
        /// </summary>
        /// <param name="seed">种子字符串</param>
        public static string GetRobot(string seed)
        {
            return string.Format(RobotAvatarUrl, seed);
        }

        #endregion

        #region 像素头像

        private const string PixelAvatarUrl = @"https://api.dicebear.com/9.x/{0}/png?seed={1}";

        public static readonly string[] AllStyles = new string[]
        {
            // 像素类 (最适合你的程序员身份)
            "pixel-art",
            "pixel-art-neutral",

            // 机器人类 (适合 AI 助手)
            "bottts",
            "bottts-neutral",

            // 精致人物类 (适合用户头像)
            "adventurer",
            "adventurer-neutral",
            "avataaars",
            "avataaars-neutral",
            // "lorelei",
            "lorelei-neutral",
            "notionists",
            "open-peeps",
            "personas",

            // 卡通与趣味类
            "big-ears",
            "big-ears-neutral",
            "big-smile",
            "miniavs",
            "fun-emoji",

            // 抽象类 (适合默认占位符)
            "identicon",
            "initials",
            "shapes",
            "rings"
        };

        public static string GetPixel()
        {
            return string.Format(PixelAvatarUrl, AllStyles.GetRandom(), _randomX.Seed(6));
        }

        public static string GetPixel(string seed)
        {
            return string.Format(PixelAvatarUrl, AllStyles.GetRandom(), seed);
        }

        #endregion

        #region 真人头像

        public enum HumanType { Male, Female, Pixel }

        private const string HumanAvatarUrl = @"https://xsgames.co/randomusers/avatar.php?g={0}";

        /// <summary>
        /// 获取随机真人头像URL
        /// </summary>
        public static string GetHuman(HumanType type)
        {
            return string.Format(HumanAvatarUrl, type.ToString().ToLower());
        }

        /// <summary>
        /// 获取指定种子字符串的真人头像URL
        /// </summary>
        /// <param name="seed">种子字符串</param>
        public static string GetHuman(HumanType type, string seed)
        {
            return string.Format(HumanAvatarUrl, type.ToString().ToLower(), seed);
        }

        #endregion
    }
}