using AxuFrameKit.Common.UI.Kit.DataBind;
using UnityEngine;

namespace TestAndSample.Sample_Common.UI.Kit.DataBind
{
    public class SampleUIBindData : MonoBehaviour
    {
        public UIBindParent bindParent1;

        private void Start()
        {
            bindParent1.SetData(UIData.Id<string>("text_name_string", "default"), UIData.Id<int>("text_age_int", 0), UIData.Id<float>("text_height_float", 0f), UIData.Id<Sprite>("img_default_sprite", null), UIData.Id<Color>("img_default_color", Color.white), UIData.Id<Material>("img_default_material", null));
        }
    }
}
