using System;
using System.Collections.Generic;
using AxuFrameKit._Core.Extension;
using AxuFrameKit._Core.Utility.Common;

namespace AxuFrameKit._Core.Utility.RandomX
{
    // 随机名称可到：https://funnamegenerator.com/zh-cn/ 进行生成获取
    public static class RandomNickname
    {
        /// <summary>
        /// 获取一个内置中文名称
        /// </summary>
        public static string Get() => InnerNicknames.GetRandom();

        /// <summary>
        /// 获取count个内置中文名称（可重复）
        /// </summary>
        public static string[] Get(int count)
        {
            if (count > InnerNicknames.Length)
            {
                Logger.Warning("获取随机名称数组时，数量超出了内置名称数组！");
                return InnerNicknames;
            }

            if (count == InnerNicknames.Length)
                return InnerNicknames;

            HashSet<int> hs = new(count);
            string[] results = new string[count];

            for (int i = 0; i < count; i++)
            {
                var tuple = InnerNicknames.GetRandomAndIndex();
                if (hs.Add(tuple.index) == false)
                {
                    i -= 1;
                    continue;
                }

                results[i] = tuple.result;
            }

            return results;
        }

        /// <summary>
        /// 获取一个自定义长度的内置组合名称
        /// </summary>
        public static string GetCombine(int length, bool isFast = true)
        {
            var (firstCount, lastCount) = length.SplitHalf();
            var array1 = InnerFirstName.GetRandomUniqueItemsAlgo(firstCount);
            var array2 = InnerLastName.GetRandomUniqueItemsAlgo(lastCount);

            if (isFast)
            {
                Span<char> buffer = stackalloc char[array1.Length + array2.Length];

                array1.CopyTo(buffer);
                array2.CopyTo(buffer.Slice(array1.Length));

                return new string(buffer);
            }

            {
                char[] combined = new char[array1.Length + array2.Length];

                Array.Copy(array1, 0, combined, 0, array1.Length);
                Array.Copy(array2, 0, combined, array1.Length, array2.Length);

                return new string(combined);
            }
        }

        /// <summary>
        /// 获取多个自定义长度的内置组合名称（可重复）
        /// </summary>
        public static string[] GetCombine(params int[] lengths)
        {
            if (lengths == null || lengths.Length == 0)
                return null;

            var result = new string[lengths.Length];
            for (int i = 0; i < lengths.Length; i++)
                result[i] = GetCombine(lengths[i]);

            return result;
        }

        #region String Name

        private const string InnerFirstName =
            "白毕卞蔡曹岑常车陈成程池邓丁范方樊闫倪周费冯符元袁岳云曾詹张章赵郑钟周邹朱褚庄卓傅甘高葛龚古关郭韩何贺洪侯胡华黄霍姬简江姜蒋金康柯孔赖郎乐雷黎李连廉梁廖林凌刘柳龙" +
            "卢鲁陆路吕罗骆马梅孟莫母穆倪宁欧区潘彭蒲皮齐戚钱强秦丘邱饶任沈盛施石时史司徒苏孙谭汤唐陶田童涂王危韦卫魏温文翁巫邬吴伍武席夏萧谢辛邢徐许薛严颜杨叶易殷尤于余俞虞";

        private const string InnerLastName =
            "震南洛栩嘉光琛潇闻鹏宇斌威汉火科技梦琪忆柳之召腾飞慕青问兰尔岚元香初夏沛菡傲珊曼文乐菱痴珊恨玉惜香寒新柔语蓉海安夜蓉涵柏水桃醉蓝春语琴从彤" +
            "傲晴语菱碧彤元霜怜梦紫寒妙彤曼易南莲紫翠雨寒易烟如萱若南寻真晓亦向珊慕灵以蕊寻雁映易雪柳孤岚笑霜海云凝天沛珊寒云冰旋宛儿" +
            "绿真盼晓霜碧凡夏菡曼香若烟半梦雅绿冰蓝灵槐平安书翠翠风香巧代云梦曼幼翠友巧听寒梦柏醉易访旋亦玉凌萱访卉怀亦笑蓝春翠靖柏夜蕾" +
            "冰夏梦松书雪乐枫念薇靖雁寻春恨山从寒忆香觅波静曼凡旋以亦念露芷蕾千帅新波代真新蕾雁玉冷卉紫千琴恨天傲芙盼山怀蝶冰山柏翠萱恨松问旋" +
            "南白易问筠如霜半芹丹珍冰彤亦寒寒雁怜云寻文乐丹翠柔谷山之瑶冰露尔珍谷雪乐萱涵菡海莲傲蕾青槐洛冬易梦惜雪宛海之柔夏青妙菡春竹痴梦紫蓝晓巧幻柏" +
            "元风冰枫访蕊南春芷蕊凡蕾凡柔安蕾天荷含玉书雅琴书瑶春雁从安夏槐念芹怀萍代曼幻珊谷丝秋翠白晴海露代荷含玉书蕾听访琴灵雁秋春雪青乐瑶含烟涵双" +
            "平蝶雅蕊傲之灵薇绿春含蕾梦蓉初丹听听蓉语芙夏彤凌瑶忆翠幻灵怜菡紫南依珊妙竹访烟怜蕾映寒友绿冰萍惜霜凌香芷蕾雁卉迎梦元柏代萱紫真千青凌寒" +
            "紫安寒安怀蕊秋荷涵雁以山凡梅盼曼翠彤谷新巧冷安千萍冰烟雅友绿南松诗云飞风寄灵书芹幼蓉以蓝笑寒忆寒秋烟芷巧水香映之醉波幻莲夜山芷卉向彤小玉幼";

        private static readonly string[] InnerNicknames =
        {
            "钢刃",
            "奥里",
            "弗瑞克",
            "布鲁诺",
            "霜语",
            "铜骨",
            "杜林",
            "铁壁",
            "格罗姆",
            "巴林",
            "黑铁",
            "石肤",
            "铁手",
            "索林",
            "德瓦恩",
            "铁甲",
            "石骨",
            "铜甲",
            "石拳",
            "铁骨",
            "铁炉堡",
            "雷铸",
            "铜甲",
            "托里",
            "泽维尔",
            "托里",
            "欧文",
            "邦尼",
            "艾丹",
            "昆廷",
            "哈罗德",
            "黛西",
            "德瓦林",
            "马文",
            "奇力",
            "索林",
            "艾米丽",
            "弗瑞克",
            "杜林",
            "雷蒙德",
            "金利",
            "帕特里克",
            "加尔文",
            "泰勒",
            "阿道夫",
            "尼尔",
            "奥里",
            "布鲁诺",
            "虚无边界乐队",
            "梦境编织者乐团",
            "星月交错乐团",
            "晨曦微光乐团",
            "迷雾森林乐团",
            "月光下的秘密乐队",
            "光影交错乐队",
            "逆境重生乐团",
            "光影交错乐队",
            "流星雨之夜乐团",
            "时光隧道乐队",
            "夜幕下的舞者乐团",
            "花火瞬间乐队",
            "幽谷回声乐队",
            "梦回故里乐团",
            "晨风轻拂乐队",
            "星空旅人乐团",
            "梦境边缘乐队",
            "月影下的歌者乐团",
            "梦中梦乐队",
            "遗忘之城乐团",
            "破晓之光乐团",
            "时光倒流乐队",
            "时光碎片乐队",
            "珊瑚",
            "橙焰",
            "黄毛",
            "翠羽",
            "紫烟",
            "霜爪",
            "碧波",
            "雪羽",
            "绿眼",
            "黑影",
            "雷电",
            "云影",
            "玛瑙",
            "火心",
            "珍珠",
            "青云",
            "赤霞",
            "墨流苏",
            "翡翠",
            "蓝梦",
            "玉爪",
            "星尘",
            "灰烬",
            "冰翼",
        };

        #endregion
    }
}