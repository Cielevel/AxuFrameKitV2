using System;
using AxuFrameKit.Common.Utility.Save;
using UnityEngine;

namespace TestAndSample.Sample_Common_Save
{
    public class SampleCommonSimpleSave : MonoBehaviour
    {
        public string content = "This is a sample content to be saved.";

        public string key = "save_01";
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SimpleSave.SaveString(key, content, SimpleSave.SaveMode.EncryptedJsonFile);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log(SimpleSave.LoadString(key, SimpleSave.SaveMode.EncryptedJsonFile));
            }
        }
    }
}