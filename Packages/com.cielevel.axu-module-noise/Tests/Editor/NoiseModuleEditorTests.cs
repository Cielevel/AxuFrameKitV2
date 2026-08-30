using System;
using AxuFrameKit.Modules.Noise.SimpleNoise;
using NUnit.Framework;
// 注意：包内命名空间 AxuFrameKit.Modules.Noise.FastNoiseLite 与其中类 FastNoiseLite 同名，
// 测试命名空间的外层名称查找会先命中同名命名空间（CS0118），必须用 using 别名绑定到类本身。
using Fnl = AxuFrameKit.Modules.Noise.FastNoiseLite.FastNoiseLite;

namespace AxuFrameKit.Modules.Noise.Tests.Editor
{
    /// <summary>
    /// [Axu/Module/Noise] 噪声模块 EditMode 测试（零依赖家族试点）：
    /// 覆盖 SimpleNoise（NoiseCreator 纯 C# 噪声）与 FastNoiseLite 包装的
    /// 确定性（同输入同输出）、种子敏感性（不同 seed 输出不同）与值域合理性。
    /// 两个被测类型均为纯 C#（无引擎依赖），测试数据全部固定，不依赖随机源。
    /// </summary>
    [Category("Axu/Module/Noise")]
    public class NoiseModuleEditorTests
    {
        private const int GridSize = 24;

        /// <summary>以非整数偏移采样一片 Perlin 网格（避开格点 0 值，便于比较分布）。</summary>
        private static double[] SamplePerlinGrid(double offset)
        {
            var samples = new double[GridSize * GridSize];
            int k = 0;
            for (int i = 0; i < GridSize; i++)
                for (int j = 0; j < GridSize; j++)
                    samples[k++] = NoiseCreator.Perlin(i * 0.617 + offset, j * 0.523 + offset);
            return samples;
        }

        [Test]
        public void NoiseCreator_Perlin_同种子两次初始化_输出完全一致()
        {
            // Arrange & Act：同一种子重灌排列表后采样同一片网格
            NoiseCreator.Init(20260830);
            var first = SamplePerlinGrid(0.13);
            NoiseCreator.Init(20260830);
            var second = SamplePerlinGrid(0.13);

            // Assert：逐点完全一致（确定性）
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void NoiseCreator_Perlin_不同种子_输出分布不同()
        {
            // Arrange & Act
            NoiseCreator.Init(1);
            var seedOne = SamplePerlinGrid(0.13);
            NoiseCreator.Init(2);
            var seedTwo = SamplePerlinGrid(0.13);

            // Assert：NUnit 对数组做逐元素比较，任一点不同即失败
            Assert.That(seedTwo, Is.Not.EqualTo(seedOne), "不同种子应产生不同的排列表与噪声输出");
        }

        [Test]
        public void NoiseCreator_Perlin_整数格点_输出恒为0()
        {
            // Arrange
            NoiseCreator.Init(7);

            // Assert：格点处相对坐标为 0，梯度点积与插值结果恒为 0（含负坐标与排列表尾部环绕）
            Assert.That(NoiseCreator.Perlin(0.0, 0.0), Is.EqualTo(0.0));
            Assert.That(NoiseCreator.Perlin(3.0, 5.0), Is.EqualTo(0.0));
            Assert.That(NoiseCreator.Perlin(-2.0, 7.0), Is.EqualTo(0.0));
            Assert.That(NoiseCreator.Perlin(255.0, 255.0), Is.EqualTo(0.0));
        }

        [Test]
        public void NoiseCreator_FractalNoise_常规分形参数_输出落在0到1()
        {
            // Arrange
            NoiseCreator.Init(99);

            // Act & Assert：4 octaves / persistence 0.5 / lacunarity 2.0 的网格采样全部在 [0,1]
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    double value = NoiseCreator.FractalNoise(i * 0.37, j * 0.29, 4, 0.5, 2.0);
                    Assert.That(value, Is.InRange(0.0, 1.0), $"坐标({i},{j}) 的分形噪声越界: {value}");
                }
            }
        }

        [Test]
        public void NoiseCreator_WhiteNoise_同输入确定且输出在0到1()
        {
            // Act & Assert
            for (int i = 0; i < 200; i++)
            {
                double x = i * 0.71;
                double y = i * 1.13;

                double first = NoiseCreator.WhiteNoise(x, y);
                double second = NoiseCreator.WhiteNoise(x, y);

                Assert.That(second, Is.EqualTo(first), $"坐标({x},{y}) 白噪声输出不确定");
                Assert.That(first, Is.InRange(0.0, 1.0));
            }
        }

        [Test]
        public void FastNoiseLite_同种子确定且不同种子不同_六类噪声值域有界()
        {
            foreach (Fnl.NoiseType type in Enum.GetValues(typeof(Fnl.NoiseType)))
            {
                // Arrange：同种子两次 + 异种子一次
                var baseNoise = CreateNoise(100, type);
                var sameSeed = CreateNoise(100, type);
                var otherSeed = CreateNoise(101, type);
                bool seedDiffers = false;
                double min = double.MaxValue;
                double max = double.MinValue;

                // Act
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        float x = i * 3.1f;
                        float y = j * 2.7f;
                        double value = baseNoise.GetNoise(x, y);
                        min = Math.Min(min, value);
                        max = Math.Max(max, value);

                        if (otherSeed.GetNoise(x, y) != value)
                            seedDiffers = true;

                        // Assert（确定性）：同种子逐点一致
                        Assert.That(sameSeed.GetNoise(x, y), Is.EqualTo(value),
                            $"{type} 同种子输出应逐点一致 ({x},{y})");
                    }
                }

                // Assert：种子敏感 + 值域有界（FastNoiseLite 各噪声类型约定 [-1,1]）
                Assert.That(seedDiffers, Is.True, $"{type} 不同种子应产生不同输出");
                Assert.That(min, Is.GreaterThanOrEqualTo(-1.0), $"{type} 最小值越界: {min}");
                Assert.That(max, Is.LessThanOrEqualTo(1.0), $"{type} 最大值越界: {max}");
            }
        }

        private static Fnl CreateNoise(int seed, Fnl.NoiseType type)
        {
            var noise = new Fnl();
            noise.SetSeed(seed);
            noise.SetNoiseType(type);
            noise.SetFrequency(0.05f);
            return noise;
        }
    }
}
