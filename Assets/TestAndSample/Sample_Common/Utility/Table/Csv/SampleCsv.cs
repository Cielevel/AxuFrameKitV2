using System;
using UnityEngine;
using AxuFrameKit.Common.Utility.Table.CSV;

namespace TestAndSample.Sample_Common.Utility.Table.Csv
{
    public class SampleCsv : MonoBehaviour
    {
        public SampleDataContainer container;

        public TextAsset textAsset;

        private void Start()
        {
            foreach (var data in container)
            {
                Debug.Log($"{data.Id}, {data.Name}, {data.Value}");
            }

            Debug.Log("================================");

            var list = SimpleCsvTool.CsvToList<SampleData>(textAsset.text);

            foreach (var data in list)
            {
                Debug.Log($"{data.Id}, {data.Name}, {data.Value}");
            }
        }
    }
}
