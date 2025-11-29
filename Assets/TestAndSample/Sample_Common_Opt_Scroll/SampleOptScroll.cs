using System.Collections.Generic;
using AxuFrameKit.Common.UI.Kit.Scroll;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TestAndSample.Sample_Common_Opt_Scroll
{
    public class SampleOptScroll : MonoBehaviour
    {
        public LoopVerticalScrollRect loopList;
    
        // 模拟数据源
        private List<string> myDataList = new List<string>();

        void Start()
        {
            // 1. 生成1000条模拟数据
            for (int i = 0; i < 100; i++)
            {
                myDataList.Add($"DT #{i + 1}");
            }

            // 2. 绑定回调函数 (核心步骤)
            // 当列表滚动时，LoopVerticalScrollRect 会调用这个方法让你更新UI
            loopList.onUpdateItem = OnUpdateItem;

            // 3. 初始化列表
            loopList.RefillCells(myDataList.Count);
        }

        // 更新Item的具体逻辑
        void OnUpdateItem(Transform itemTrans, int index)
        {
            // 这里的 itemTrans 就是从对象池里取出来的GameObject
            // 你可以使用 GetComponent 或者是缓存好的引用脚本来更新UI
        
            // 示例：查找子物体 Text 并赋值
            // (在实际项目中，建议在Prefab上挂一个脚本直接赋值，不要频繁使用Find)
            TMP_Text textComp = itemTrans.Find("TEXT").GetComponent<TMP_Text>();
        
            // 从数据源拿数据
            string data = myDataList[index];
        
            textComp.text = $"{data} (Index: {index})";
        
            // 也可以根据index改变颜色，模拟交替显示
            Image bg = itemTrans.GetComponent<Image>();
            if (bg != null)
                bg.color = index % 2 == 0 ? Color.white : new Color(0.9f, 0.9f, 0.9f);
        }
    
        // 模拟添加数据按钮
        public void OnAddBtnClick()
        {
            myDataList.Add($"新道具 {Random.Range(0, 100)}");
            // 保持位置刷新
            loopList.RefillCells(myDataList.Count, false);
        }
    }
}