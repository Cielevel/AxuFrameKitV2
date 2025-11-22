using System;
using AxuFrameKit._Core.Structure;
using UnityEngine;

namespace TestAndSample.Sample_Core_Structure
{
    public class SampleIntBound : MonoBehaviour
    {
        public IntBound bound1;
        public IntBound bound2;

        public TestIntBoundInClass test;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Debug.Log("Random: " + bound1.Random(true, true));
        }
    }

    [Serializable]
    public class TestIntBoundInClass
    {
        public IntBound bound1;
    }
}