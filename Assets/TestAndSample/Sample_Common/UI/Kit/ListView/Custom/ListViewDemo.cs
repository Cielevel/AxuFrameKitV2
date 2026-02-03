using System.Collections.Generic;
using AxuFrameKit.Common.UI.Kit.ListView;
using UnityEngine;

namespace TestAndSample.Sample_Common.UI.Kit.ListView
{
    /// <summary>
    /// ListView使用示例
    /// 演示如何在代码中使用UIListView
    /// </summary>
    public class ListViewDemo : MonoBehaviour
    {
        [Header("ListView引用")]
        [SerializeField] private UIListView listView;

        [Header("测试数据")]
        [SerializeField] private int testDataCount = 100;
        [SerializeField] private Sprite testIcon;

        private List<SimpleItemData> _testData;

        private void Start()
        {
            SetupListView();
            GenerateTestData();
            BindData();
        }

        /// <summary>
        /// 设置ListView事件
        /// </summary>
        private void SetupListView()
        {
            // 监听Item点击事件
            listView.OnItemClicked += (item, index) =>
            {
                Debug.Log($"点击了第 {index} 个Item");
            };

            // 监听Item数据更新事件
            listView.OnItemUpdate += (item, data, index) =>
            {
                // 可以在这里做额外的数据处理
                // Debug.Log($"Item {index} 数据已更新");
            };

            // 监听选中状态变化
            listView.OnSelectionChanged += (selectedIndices) =>
            {
                Debug.Log($"当前选中了 {selectedIndices.Count} 个Item: {string.Join(", ", selectedIndices)}");
            };
        }

        /// <summary>
        /// 生成测试数据
        /// </summary>
        private void GenerateTestData()
        {
            _testData = new List<SimpleItemData>();

            for (int i = 0; i < testDataCount; i++)
            {
                _testData.Add(new SimpleItemData(
                    title: $"Item {i}",
                    description: $"This is {i} description",
                    icon: testIcon,
                    value: i * 10
                ));
            }
        }

        /// <summary>
        /// 绑定数据到ListView
        /// </summary>
        private void BindData()
        {
            listView.SetDataSource(_testData);
        }

        #region 公开方法 - 可在Inspector或其他地方调用

        /// <summary>
        /// 追加数据
        /// </summary>
        public void AppendData()
        {
            var newData = new List<SimpleItemData>();
            int startIndex = _testData.Count;

            for (int i = 0; i < 10; i++)
            {
                newData.Add(new SimpleItemData(
                    title: $"New Item {startIndex + i}",
                    description: $"This is {i} description",
                    icon: testIcon,
                    value: (startIndex + i) * 10
                ));
            }

            _testData.AddRange(newData);
            listView.AppendData(newData);

            Debug.Log($"追加了 {newData.Count} 条数据，当前总数: {_testData.Count}");
        }

        /// <summary>
        /// 刷新列表
        /// </summary>
        public void RefreshList()
        {
            listView.RefreshList(false);
            Debug.Log("列表已刷新");
        }

        /// <summary>
        /// 清空列表
        /// </summary>
        public void ClearList()
        {
            _testData.Clear();
            listView.Clear();
            Debug.Log("列表已清空");
        }

        /// <summary>
        /// 滚动到指定位置
        /// </summary>
        public void ScrollToItem(int index)
        {
            if (index >= 0 && index < _testData.Count)
            {
                listView.ScrollToItem(index, true);
                Debug.Log($"滚动到Item {index}");
            }
        }

        /// <summary>
        /// 选中指定Item
        /// </summary>
        public void SelectItem(int index)
        {
            listView.SelectItem(index);
        }

        /// <summary>
        /// 清空选中
        /// </summary>
        public void ClearSelection()
        {
            listView.ClearSelection();
            Debug.Log("清空选中");
        }

        /// <summary>
        /// 获取选中的Item数据
        /// </summary>
        public void PrintSelectedItems()
        {
            var selectedIndices = listView.GetSelectedIndices();
            Debug.Log($"当前选中了 {selectedIndices.Count} 个Item:");

            foreach (var index in selectedIndices)
            {
                if (index >= 0 && index < _testData.Count)
                {
                    var data = _testData[index];
                    Debug.Log($"  - [{index}] {data.Title}: {data.Description}");
                }
            }
        }

        #endregion
    }
}
