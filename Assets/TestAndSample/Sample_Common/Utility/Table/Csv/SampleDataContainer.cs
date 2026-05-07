using System.Threading.Tasks;
using AxuFrameKit.Common.CustomAttribute;
using AxuFrameKit.Common.Utility.Table.CSV;
using UnityEngine;

namespace TestAndSample.Sample_Common.Utility.Table.Csv
{
    [InspectorTitle("测试Container")]
    [InspectorIntro("这是一个测试用的Container")]
    [CreateAssetMenu(fileName = "SampleDataContainer", menuName = "AxuSample/Utility/Table/Csv/SampleDataContainer")]
    public class SampleDataContainer : CsvContainerSo<SampleData>
    {
        public override Task Init() => Task.CompletedTask;
    }

    public class SampleData
    {
        [CsvColumn("唯一id")]
        public int Id { get; set; }
        [CsvColumn("名称")]
        public string Name { get; set; }
        [CsvColumn("值")]
        public float Value { get; set; }
    }
}