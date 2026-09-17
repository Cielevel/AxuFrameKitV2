using System.Linq;
using AxuFrameKit.Common.Editor.KitBoxRevV2.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AxuFrameKit.Common.Editor.KitBoxRevV2.Groups.CopierGroup
{
    public class MaterialCopierRevPage : IKitPageRevV2
    {
        public string Title => "材质球关联";
        private IKitBoxRevV2 _kitBox;

        private ObjectField _sourceField;
        private ObjectField _targetField;
        private ScrollView _logView;
        private Button _copyButton;

        public void Init(IKitBoxRevV2 kitBox) => _kitBox = kitBox;

        public VisualElement CreateContent()
        {
            var root = new VisualElement();
            root.AddToClassList("page-container");

            var hint = new Label("根据子物体名称一一对应，将源模型的材质球同步至目标模型。");
            hint.AddToClassList("page-hint");
            root.Add(hint);

            // --- Slots ---
            var slotContainer = new VisualElement();
            slotContainer.AddToClassList("row-space-between");
            slotContainer.AddToClassList("mb-25");
            root.Add(slotContainer);

            _sourceField = CreateObjectSlot(slotContainer, "源材质", "从场景拖入具有正确材质的模型");

            var arrow = new Label("→");
            arrow.style.fontSize = 24;
            arrow.style.alignSelf = Align.Center;
            arrow.AddToClassList("text-accent");
            slotContainer.Add(arrow);

            _targetField = CreateObjectSlot(slotContainer, "目标材质", "从场景拖入需要同步材质的模型");

            // --- Action ---
            _copyButton = new Button(ExecuteCopy);
            _copyButton.text = "开始同步材质关联";
            _copyButton.AddToClassList("btn-primary");
            _copyButton.SetEnabled(false);
            root.Add(_copyButton);

            // Validation logic
            _sourceField.RegisterValueChangedCallback(_ => Validate());
            _targetField.RegisterValueChangedCallback(_ => Validate());

            // --- Log ---
            var logHeader = new Label("操作日志");
            logHeader.AddToClassList("section-header");
            root.Add(logHeader);

            _logView = new ScrollView();
            _logView.AddToClassList("log-view");
            root.Add(_logView);

            var clearBtn = new Button(() => _logView.Clear());
            clearBtn.text = "清除日志";
            clearBtn.AddToClassList("btn-secondary");
            clearBtn.AddToClassList("mt-5");
            clearBtn.style.alignSelf = Align.FlexEnd;
            root.Add(clearBtn);

            return root;
        }

        private ObjectField CreateObjectSlot(VisualElement parent, string label, string tooltip)
        {
            var box = new VisualElement();
            box.AddToClassList("slot-container");

            var title = new Label(label);
            title.AddToClassList("slot-title");
            box.Add(title);

            var field = new ObjectField { objectType = typeof(GameObject), allowSceneObjects = true };
            field.tooltip = tooltip;
            box.Add(field);

            parent.Add(box);
            return field;
        }

        private void Validate()
        {
            _copyButton.SetEnabled(_sourceField.value != null && _targetField.value != null);
        }

        private void ExecuteCopy()
        {
            var src = _sourceField.value as GameObject;
            var target = _targetField.value as GameObject;

            if (src == null || target == null) return;

            Log("开始同步材质关联...", "info");

            int success = 0;
            int total = 0;
            var srcRenderers = src.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in srcRenderers)
            {
                total++;
                string path = GetRelativePath(renderer.transform, src.transform);
                var targetTransform = target.transform.Find(path);

                if (targetTransform != null && targetTransform.TryGetComponent<Renderer>(out var targetRenderer))
                {
                    Undo.RecordObject(targetRenderer, "Copy Materials");
                    targetRenderer.sharedMaterials = renderer.sharedMaterials;
                    Log($"同步成功：{path}", "ok");
                    success++;
                }
                else
                {
                    Log($"找不到目标节点：{path}", "bad");
                }
            }

            Log($"完成！总计: {total} | 成功: {success} | 失败: {total - success}", success == total ? "ok" : "warn");
        }

        private string GetRelativePath(Transform child, Transform root)
        {
            if (child == root) return "";
            string path = child.name;
            Transform current = child.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private void Log(string msg, string kind = "info")
        {
            var label = new Label(msg);
            label.AddToClassList($"log-{kind}");
            _logView.Add(label);
            _logView.ScrollTo(label);
        }

        public void Refresh()
        {
            _sourceField.value = null;
            _targetField.value = null;
            _logView.Clear();
        }

        // 本页无全局事件订阅，生命周期钩子留空
        public void OnEnter() { }
        public void OnExit() { }

        public void DrawIMGUI() { }
    }
}
