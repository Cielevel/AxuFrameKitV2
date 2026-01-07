using UnityEngine;
using System.Collections.Generic;
using AxuFrameKit.Common.CustomAttribute;

namespace TestAndSample.Sample_Common_CustomAttribute.SampleListDrawer
{
    public class SampleListDrawer : MonoBehaviour
    {
        [ListDrawerSettings(ListElementLabelName = "Name")]
        public List<SampleListDrawerUnit> sampleList;

        [System.Serializable]
        public class SampleListDrawerUnit
        {
            public string Name;
            public int Value;
        }
    }
}
