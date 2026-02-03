using System.Collections.Generic;
using AxuFrameKit.Common.UI.Kit.DataBind;
using AxuFrameKit.Common.UI.Kit.ListView;
using UnityEngine;
using UnityEngine.UI;

namespace TestAndSample.Sample_Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView 演示控制器
    /// 展示两种使用模式：
    /// 1. 自定义模式 - 继承 UIListViewItem 实现 OnUpdateData
    /// 2. DataBind 模式 - 使用 UIListViewDataBindItem + UIDataUnit 自动绑定
    /// </summary>
    public class ListViewDemoController : MonoBehaviour
    {
        [Header("ListView 引用")]
        [SerializeField] private UIListView listViewCustom;
        [SerializeField] private UIListView listViewDataBind;

        [Header("预制体")]
        [SerializeField] private GameObject customItemPrefab;
        [SerializeField] private GameObject dataBindItemPrefab;

        [Header("测试数据")]
        [SerializeField] private List<Sprite> testIcons;

        private UIListViewAdapter<ItemData> _adapter;

        private void Start()
        {
            // 模式1：使用自定义 Item 类
            SetupCustomMode();

            // 模式2：使用 DataBind 模式
            SetupDataBindMode();
        }

        #region 模式1：自定义模式

        /// <summary>
        /// 设置自定义模式
        /// 使用 SimpleListViewItem（继承 UIListViewItem）
        /// </summary>
        private void SetupCustomMode()
        {
            if (listViewCustom == null)
                return;

            // 创建测试数据
            var dataList = new List<ItemData>();
            for (int i = 0; i < 100; i++)
            {
                dataList.Add(new ItemData
                {
                    Id = i,
                    Title = $"物品 {i}",
                    Description = $"这是物品 {i} 的描述",
                    Icon = testIcons != null && testIcons.Count > 0 ? testIcons[i % testIcons.Count] : null,
                    Count = Random.Range(1, 100),
                    IsRare = i % 5 == 0
                });
            }

            // 方法1：直接使用 SetDataSource
            listViewCustom.SetDataSource(dataList);

            // 方法2：使用 Adapter（推荐，支持自动刷新）
            // _adapter = new UIListViewAdapter<ItemData>(dataList);
            // listViewCustom.SetAdapter(_adapter);

            // 监听事件
            listViewCustom.OnItemClicked += OnCustomItemClicked;
            listViewCustom.OnItemSelectionChanged += OnCustomItemSelectionChanged;
        }

        private void OnCustomItemClicked(UIListViewItem item, int index)
        {
            var data = item.Data as ItemData;
            Debug.Log($"[自定义模式] 点击了 Item {index}: {data?.Title}");
        }

        private void OnCustomItemSelectionChanged(UIListViewItem item, bool selected)
        {
            Debug.Log($"[自定义模式] Item {item.DataIndex} 选中状态: {selected}");
        }

        #endregion

        #region 模式2：DataBind 模式

        /// <summary>
        /// 设置 DataBind 模式
        /// 使用 UIListViewDataBindItem + UIDataUnit 数组
        /// </summary>
        private void SetupDataBindMode()
        {
            if (listViewDataBind == null)
                return;

            // 创建 DataBind 数据列表
            var bindDataList = new List<IUIDataUnit[]>();

            for (int i = 0; i < 100; i++)
            {
                int index = i;

                // 方式1：使用 ListViewDataBuilder
                var data = new ListViewDataBuilder()
                    .SetData("text_title_string", $"Item {index}")
                    .SetData("text_desc_string", $"Description {index}")
                    .SetData("text_index_string", $"Index {index}")
                    .Build();

                bindDataList.Add(data);

                // 方式2：使用 ListViewData 静态方法（适用于简单数据）
                var simpleData = ListViewData.Id("title", $"物品 {i}");
                bindDataList.Add(simpleData);

                // 方式3：实现 IUIDataBindable 接口的类
                // var bindableData = new BindableItemData { Title = $"物品 {i}", ... };
                // bindDataList.Add(bindableData.ToDataUnits());

                // 方式4：直接创建
                // bindDataList.Add(new IUIDataUnit[]
                // {
                //     UIData.Id<string>("text_title_string", $"Item {index}"),
                //     UIData.Id<string>("text_desc_string", $"Description {index}"),
                //     UIData.Id<string>("text_index_string", $"Index {index}")
                // });
            }

            // 设置数据
            listViewDataBind.SetBindData(bindDataList);

            // 监听事件
            listViewDataBind.OnItemClicked += OnDataBindItemClicked;
        }

        private void OnDataBindItemClicked(UIListViewItem item, int index)
        {
            Debug.Log($"[DataBind模式] 点击了 Item {index}");
        }

        #endregion

        #region 公共方法（供UI按钮调用）

        [ContextMenu("添加数据")]
        public void AddData()
        {
            // 自定义模式：追加数据
            var newData = new ItemData
            {
                Id = Random.Range(1000, 9999),
                Title = $"新物品 {Random.Range(1, 100)}",
                Description = "动态添加的数据",
                Count = Random.Range(1, 100)
            };

            // 如果有 adapter，使用 adapter 添加
            _adapter?.Add(newData);

            // 或者直接追加
            // listViewCustom.AppendData(new List<ItemData> { newData });
        }

        [ContextMenu("清空数据")]
        public void ClearData()
        {
            _adapter?.Clear();
            listViewCustom?.Clear();
            listViewDataBind?.Clear();
        }

        [ContextMenu("滚动到随机位置")]
        public void ScrollToRandom()
        {
            int randomIndex = Random.Range(0, 100);
            listViewCustom?.ScrollToItem(randomIndex, true);
            listViewDataBind?.ScrollToItem(randomIndex, true);
        }

        #endregion
    }

    #region 数据类

    /// <summary>
    /// 普通数据类（用于自定义模式）
    /// </summary>
    public class ItemData
    {
        public int Id;
        public string Title;
        public string Description;
        public Sprite Icon;
        public int Count;
        public bool IsRare;
    }

    /// <summary>
    /// 可绑定数据类（用于 DataBind 模式）
    /// 实现 IUIDataBindable 接口，支持自动转换
    /// </summary>
    public class BindableItemData : IUIDataBindable
    {
        public string Title;
        public string Description;
        public int Count;
        public bool IsRare;
        public int Index;

        public IUIDataUnit[] ToDataUnits()
        {
            return new ListViewDataBuilder()
                .Set("title", Title)
                .Set("description", Description)
                .Set("count", Count)
                .Set("isRare", IsRare)
                .Set("index", Index)
                .Build();
        }
    }

    #endregion
}
