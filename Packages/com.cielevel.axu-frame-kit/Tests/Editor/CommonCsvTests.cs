using AxuFrameKit.Common.Utility.Table.CSV;
using NUnit.Framework;

namespace AxuFrameKit.Tests.Editor
{
    /// <summary>
    /// [Axu/Common/Util] CsvParser 解析：基础类型转换与数组字段、引号字段（逗号/转义引号）。
    /// </summary>
    [Category("Axu/Common/Util")]
    public class CommonCsvTests
    {
        /// <summary>按列名映射的测试行（无 CsvColumn 特性，走成员名匹配）。</summary>
        public sealed class HeroRow
        {
            public int Id;
            public string Name;
            public float Hp;
            public bool Active;
            public string[] Tags;
        }

        public sealed class DescRow
        {
            public int Id;
            public string Desc;
        }

        [Test]
        public void CsvParser_基础表_类型转换与数组字段()
        {
            // Arrange：默认选项（UTF8、逗号分隔、含表头、Trim、数组分隔符 '|'）
            const string csv = "Id,Name,Hp,Active,Tags\n" +
                               "1,阿修,100.5,yes,main|boss\n" +
                               "2,Bot,3.25,false,\n";

            // Act
            var result = new CsvParser().Parse<HeroRow>(csv);

            // Assert
            Assert.That(result.IsSuccess, Is.True, string.Join(";", result.Errors));
            Assert.That(result.Data, Has.Count.EqualTo(2));
            Assert.That(result.Data[0].Id, Is.EqualTo(1));
            Assert.That(result.Data[0].Name, Is.EqualTo("阿修"));
            Assert.That(result.Data[0].Hp, Is.EqualTo(100.5f).Within(0.0001f));
            Assert.That(result.Data[0].Active, Is.True, "'yes' 应解析为 true");
            Assert.That(result.Data[0].Tags, Is.EqualTo(new[] { "main", "boss" }));
            Assert.That(result.Data[1].Active, Is.False, "'false' 应解析为 false");
            Assert.That(result.Data[1].Tags, Is.Null, "空数组字段应映射为 null");
        }

        [Test]
        public void CsvParser_引号字段_逗号与转义引号()
        {
            // Arrange：限定符内的逗号不拆分；两个连续限定符转义为一个引号
            const string csv = "Id,Desc\n" +
                               "1,\"含,逗号\"\n" +
                               "2,\"含\"\"转义\"\"引号\"\n";

            // Act
            var result = new CsvParser().Parse<DescRow>(csv);

            // Assert
            Assert.That(result.IsSuccess, Is.True, string.Join(";", result.Errors));
            Assert.That(result.Data, Has.Count.EqualTo(2));
            Assert.That(result.Data[0].Desc, Is.EqualTo("含,逗号"));
            Assert.That(result.Data[1].Desc, Is.EqualTo("含\"转义\"引号"));
        }
    }
}
