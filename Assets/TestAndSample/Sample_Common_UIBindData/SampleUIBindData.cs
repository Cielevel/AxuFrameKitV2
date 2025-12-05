using AxuFrameKit.Common.UI.Kit.DataBind;
using UnityEngine;

namespace TestAndSample.Sample_Common_UIBindData
{
    public class SampleUIBindData : MonoBehaviour
    {
        public UIBindParent bindParent1;

        private void Start()
        {
            bindParent1.SetData(UIData.Id("text_name", "this is name"), UIData.Id("text_age", 12), UIData.Id("text_height", 168.55f));
        }
    }
}
