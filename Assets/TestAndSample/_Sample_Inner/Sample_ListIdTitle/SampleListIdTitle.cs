using System.Collections.Generic;
using UnityEngine;

namespace TestAndSample._Sample_Inner.Sample_ListIdTitle
{
    public class SampleListIdTitle : MonoBehaviour
    {
        [System.Serializable]
        public class Unit
        {
            public string Id => title;
            public string title;
        }

        public List<Unit> units;
    }
}
