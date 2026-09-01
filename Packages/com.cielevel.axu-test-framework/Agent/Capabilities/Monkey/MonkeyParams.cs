using Newtonsoft.Json;
using UnityEngine;

namespace AxuTestFramework.Agent.Capabilities.Monkey
{
    /// <summary>Monkey 能力参数（scenario.params）。</summary>
    public class MonkeyParams
    {
        /// <summary>目标场景路径（空 = 不加载、对当前场景施虐）。</summary>
        [JsonProperty("scene")] public string Scene = "";

        /// <summary>时长秒数（默认 60，clamp 0.1..7200）。</summary>
        [JsonProperty("durationSec")] public float DurationSec = 60f;

        /// <summary>每秒点按次数期望值（默认 2，clamp 0.1..30；实际间隔带随机抖动）。</summary>
        [JsonProperty("actionsPerSecond")] public float ActionsPerSecond = 2f;

        /// <summary>随机种子（必复现：同 seed + 同区域 + 同分辨率产出同坐标序列；launch.Seed 优先于此值）。</summary>
        [JsonProperty("seed")] public int Seed;

        /// <summary>操作区域（归一化 0-1 坐标域，{x,y,w,h}；null = 全屏）。</summary>
        [JsonProperty("region")] public MonkeyRegion Region;

        /// <summary>出现 Error/Exception 日志是否立即终止（默认 true；false 时仅记录计数继续施虐）。</summary>
        [JsonProperty("stopOnError")] public bool StopOnError = true;

        /// <summary>卡死检测阈值秒数：真实墙钟内 <see cref="Time.frameCount"/> 无推进即判疑似卡死（默认 5，clamp 0.5..600）。</summary>
        [JsonProperty("hangThresholdSec")] public float HangThresholdSec = 5f;

        /// <summary>clamp 后的时长秒数。</summary>
        public float ClampedDurationSec => Mathf.Clamp(DurationSec, 0.1f, 7200f);

        /// <summary>clamp 后的每秒操作数。</summary>
        public float ClampedActionsPerSecond => Mathf.Clamp(ActionsPerSecond, 0.1f, 30f);

        /// <summary>clamp 后的卡死阈值秒数。</summary>
        public float ClampedHangThresholdSec => Mathf.Clamp(HangThresholdSec, 0.5f, 600f);
    }

    /// <summary>操作区域（归一化 0-1 坐标域；x/y/w/h 均默认合法值，最终 clamp 到 [0,1] 且 w/h ≥ 0.01）。</summary>
    public class MonkeyRegion
    {
        /// <summary>区域左下角 x（0-1）。</summary>
        [JsonProperty("x")] public float X;

        /// <summary>区域左下角 y（0-1）。</summary>
        [JsonProperty("y")] public float Y;

        /// <summary>区域宽（0-1）。</summary>
        [JsonProperty("w")] public float W = 1f;

        /// <summary>区域高（0-1）。</summary>
        [JsonProperty("h")] public float H = 1f;

        /// <summary>clamp 后的安全区域（x+w/y+h 不越 [0,1]，w/h 不小于 0.01）。</summary>
        public (float X, float Y, float W, float H) Clamped
        {
            get
            {
                var w = Mathf.Clamp(W, 0.01f, 1f);
                var h = Mathf.Clamp(H, 0.01f, 1f);
                var x = Mathf.Clamp(X, 0f, 1f - w);
                var y = Mathf.Clamp(Y, 0f, 1f - h);
                return (x, y, w, h);
            }
        }
    }
}
