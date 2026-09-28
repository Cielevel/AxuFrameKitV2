using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream.Tests.Editor
{
    /// <summary>
    /// 控制台面板测试装置（Rig）：按产品层级搭建 UIPanelLiveStreamConsole 所需的全部
    /// 序化引用（用户列 / 礼物列 / 操作区 / 收起目标）。
    /// 测试 asmdef 未显式引用 UnityEngine.UI / Unity.TextMeshPro，uGUI 与 TMP 组件统一经反射构造。
    /// 搭建顺序为「根 inactive → 注入全部序列化引用 → Activate」以规避 Awake 期
    /// EnsureReference 缺失报错污染日志（EditMode 下激活即触发 Awake，但 Start 不执行）。
    /// </summary>
    internal sealed class LiveStreamConsoleUiRig
    {
        private static readonly Dictionary<string, Type> UiTypeCache = new Dictionary<string, Type>();

        public GameObject Root;
        public UIPanelLiveStreamConsole Panel;
        public RectTransform UserListContent;
        public UIConsoleUserItem UserItemTemplate;
        public RectTransform GiftListContent;
        public UIConsoleGiftItem GiftItemTemplate;
        public object CommentInput;      // TMP_InputField（测试程序集无编译期引用，经反射访问）
        public object LikeCountInput;    // TMP_InputField
        public object SendCommentButton; // Button
        public object LikeButton;        // Button
        public object SendGiftButton;    // Button
        public object CollapseButton;    // Button
        public GameObject BodyRoot;
        public bool Activated;

        /// <summary>
        /// 激活根节点并手动触发面板 Awake 接线。
        /// 本编辑器（Unity 6000.0）EditMode 下激活 GameObject 不会自动调用非 ExecuteAlways
        /// 脚本的 Awake，故等价运行时接线（按钮 onClick、点赞输入过滤）需手动调用一次；
        /// Activated 标记保证只调用一次，避免监听重复叠加。
        /// </summary>
        public void Activate()
        {
            if (Activated)
            {
                return;
            }

            Activated = true;
            Root.SetActive(true);
            LiveStreamTestReflection.Call(Panel, "Awake");
        }

        /// <summary>当前面板条目集合（Setup 克隆产物，取面板私有列表快照）。</summary>
        public List<UIConsoleUserItem> UserItems =>
            new List<UIConsoleUserItem>((List<UIConsoleUserItem>)LiveStreamTestReflection.GetField(Panel, "_userItems"));

        /// <summary>当前礼物条目集合（Setup 克隆产物，取面板私有列表快照）。</summary>
        public List<UIConsoleGiftItem> GiftItems =>
            new List<UIConsoleGiftItem>((List<UIConsoleGiftItem>)LiveStreamTestReflection.GetField(Panel, "_giftItems"));

        public void SetUserSelected(UIConsoleUserItem item, bool isOn) => SetToggleOn(item, isOn);

        public void SetGiftSelected(UIConsoleGiftItem item, bool isOn) => SetToggleOn(item, isOn);

        public void SetGiftItemCount(UIConsoleGiftItem item, string text) =>
            InputSetText(LiveStreamTestReflection.GetField(item, "_countInput"), text);

        public string GetGiftItemInputText(UIConsoleGiftItem item) =>
            (string)LiveStreamTestReflection.GetProperty(LiveStreamTestReflection.GetField(item, "_countInput"), "text");

        public void SetCommentText(string text) => InputSetText(CommentInput, text);

        public void SetLikeCountText(string text) => InputSetText(LikeCountInput, text);

        public string GetCommentInputText() => (string)LiveStreamTestReflection.GetProperty(CommentInput, "text");

        public string GetLikeCountInputText() => (string)LiveStreamTestReflection.GetProperty(LikeCountInput, "text");

        /// <summary>模拟按钮点击（onClick.Invoke，触发面板 Awake 期接线的事件回调）。</summary>
        public void Click(object button)
        {
            var onClick = LiveStreamTestReflection.GetProperty(button, "onClick");
            var invoke = onClick.GetType().GetMethod("Invoke", Type.EmptyTypes);
            Assert.That(invoke, Is.Not.Null, "UnityEvent.Invoke 不存在");
            invoke.Invoke(onClick, null);
        }

        // ---------------------------------------------------------------- 搭建

        public static LiveStreamConsoleUiRig Build()
        {
            ResolveUiTypesOrFail();

            var rig = new LiveStreamConsoleUiRig();

            // 根节点带 Canvas 承载 uGUI/TMP；整体先建为 inactive，注入完所有引用再激活
            rig.Root = new GameObject("LiveStreamConsoleRoot", typeof(RectTransform), typeof(Canvas));
            rig.Root.SetActive(false);
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(rig.Root.transform, false);
            rig.Panel = panelGo.AddComponent<UIPanelLiveStreamConsole>();

            // —— 用户列：Content + 模板条目（Toggle 在条目根上，其余子物体） ——
            rig.UserListContent = CreateRectChild(panelGo.transform, "UserListContent");
            var userItemGo = CreateRectChild(rig.UserListContent, "UserItemTemplate").gameObject;
            userItemGo.SetActive(false);
            rig.UserItemTemplate = userItemGo.AddComponent<UIConsoleUserItem>();
            LiveStreamTestReflection.SetField(rig.UserItemTemplate, "_toggle",
                AddComponentByType(userItemGo, "UnityEngine.UI.Toggle", "UnityEngine.UI"));
            LiveStreamTestReflection.SetField(rig.UserItemTemplate, "_avatar",
                CreateUiChild(rig.UserItemTemplate.transform, "Avatar", "UnityEngine.UI.Image"));
            LiveStreamTestReflection.SetField(rig.UserItemTemplate, "_nicknameText",
                CreateTmpText(rig.UserItemTemplate.transform, "Nickname"));
            LiveStreamTestReflection.SetField(rig.UserItemTemplate, "_uidText",
                CreateTmpText(rig.UserItemTemplate.transform, "Uid"));

            // —— 礼物列：Content + 模板条目（含数量输入框） ——
            rig.GiftListContent = CreateRectChild(panelGo.transform, "GiftListContent");
            var giftItemGo = CreateRectChild(rig.GiftListContent, "GiftItemTemplate").gameObject;
            giftItemGo.SetActive(false);
            rig.GiftItemTemplate = giftItemGo.AddComponent<UIConsoleGiftItem>();
            LiveStreamTestReflection.SetField(rig.GiftItemTemplate, "_toggle",
                AddComponentByType(giftItemGo, "UnityEngine.UI.Toggle", "UnityEngine.UI"));
            LiveStreamTestReflection.SetField(rig.GiftItemTemplate, "_icon",
                CreateUiChild(rig.GiftItemTemplate.transform, "Icon", "UnityEngine.UI.Image"));
            LiveStreamTestReflection.SetField(rig.GiftItemTemplate, "_nameText",
                CreateTmpText(rig.GiftItemTemplate.transform, "Name"));
            LiveStreamTestReflection.SetField(rig.GiftItemTemplate, "_priceText",
                CreateTmpText(rig.GiftItemTemplate.transform, "Price"));
            LiveStreamTestReflection.SetField(rig.GiftItemTemplate, "_countInput",
                CreateTmpInputField(rig.GiftItemTemplate.transform, "CountInput"));

            // —— 操作区 ——
            rig.CommentInput = CreateTmpInputField(panelGo.transform, "CommentInput");
            rig.LikeCountInput = CreateTmpInputField(panelGo.transform, "LikeCountInput");
            rig.SendCommentButton = CreateUiChild(panelGo.transform, "SendCommentButton", "UnityEngine.UI.Button");
            rig.LikeButton = CreateUiChild(panelGo.transform, "LikeButton", "UnityEngine.UI.Button");
            rig.SendGiftButton = CreateUiChild(panelGo.transform, "SendGiftButton", "UnityEngine.UI.Button");
            rig.CollapseButton = CreateUiChild(panelGo.transform, "CollapseButton", "UnityEngine.UI.Button");
            rig.BodyRoot = new GameObject("BodyRoot");
            rig.BodyRoot.transform.SetParent(panelGo.transform, false);

            // —— 注入面板序列化引用 ——
            LiveStreamTestReflection.SetField(rig.Panel, "_userListContent", rig.UserListContent);
            LiveStreamTestReflection.SetField(rig.Panel, "_userItemTemplate", rig.UserItemTemplate);
            LiveStreamTestReflection.SetField(rig.Panel, "_giftListContent", rig.GiftListContent);
            LiveStreamTestReflection.SetField(rig.Panel, "_giftItemTemplate", rig.GiftItemTemplate);
            LiveStreamTestReflection.SetField(rig.Panel, "_commentInput", rig.CommentInput);
            LiveStreamTestReflection.SetField(rig.Panel, "_likeCountInput", rig.LikeCountInput);
            LiveStreamTestReflection.SetField(rig.Panel, "_sendCommentButton", rig.SendCommentButton);
            LiveStreamTestReflection.SetField(rig.Panel, "_likeButton", rig.LikeButton);
            LiveStreamTestReflection.SetField(rig.Panel, "_sendGiftButton", rig.SendGiftButton);
            LiveStreamTestReflection.SetField(rig.Panel, "_collapseButton", rig.CollapseButton);
            LiveStreamTestReflection.SetField(rig.Panel, "_bodyRoot", rig.BodyRoot);

            return rig;
        }

        // ---------------------------------------------------------------- 内部工具

        private static void ResolveUiTypesOrFail()
        {
            Assert.That(ResolveType("UnityEngine.UI.Toggle", "UnityEngine.UI"), Is.Not.Null,
                "无法解析 UnityEngine.UI.Toggle（测试环境缺 uGUI 程序集）");
            Assert.That(ResolveType("UnityEngine.UI.Image", "UnityEngine.UI"), Is.Not.Null,
                "无法解析 UnityEngine.UI.Image");
            Assert.That(ResolveType("UnityEngine.UI.Button", "UnityEngine.UI"), Is.Not.Null,
                "无法解析 UnityEngine.UI.Button");
            Assert.That(ResolveType("TMPro.TextMeshProUGUI", "Unity.TextMeshPro"), Is.Not.Null,
                "无法解析 TMPro.TextMeshProUGUI（测试环境缺 TMP 程序集）");
            Assert.That(ResolveType("TMPro.TMP_InputField", "Unity.TextMeshPro"), Is.Not.Null,
                "无法解析 TMPro.TMP_InputField");
        }

        private static Type ResolveType(string fullName, string assemblyName)
        {
            var cacheKey = $"{assemblyName}:{fullName}";
            if (UiTypeCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var type = Type.GetType($"{fullName}, {assemblyName}");
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    type = assembly.GetType(fullName);
                    if (type != null)
                    {
                        break;
                    }
                }
            }

            UiTypeCache[cacheKey] = type;
            return type;
        }

        private static object AddComponentByType(GameObject go, string fullName, string assemblyName)
        {
            var type = ResolveType(fullName, assemblyName);
            Assert.That(type, Is.Not.Null, $"无法解析组件类型 {fullName}");
            return go.AddComponent(type);
        }

        private static RectTransform CreateRectChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static object CreateUiChild(Transform parent, string name, string componentFullName)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            return AddComponentByType(go, componentFullName, "UnityEngine.UI");
        }

        private static object CreateTmpText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            return AddComponentByType(go, "TMPro.TextMeshProUGUI", "Unity.TextMeshPro");
        }

        private static object CreateTmpInputField(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var input = AddComponentByType(go, "TMPro.TMP_InputField", "Unity.TextMeshPro");

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            LiveStreamTestReflection.SetField(input, "m_TextViewport", (RectTransform)viewport.transform);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            textGo.transform.SetParent(go.transform, false);
            var text = AddComponentByType(textGo, "TMPro.TextMeshProUGUI", "Unity.TextMeshPro");
            LiveStreamTestReflection.SetField(input, "m_TextComponent", text);

            return input;
        }

        private static void InputSetText(object input, string text) =>
            LiveStreamTestReflection.InvokeWithArgs(input, "SetTextWithoutNotify", text);

        private static void SetToggleOn(object item, bool isOn)
        {
            var toggle = LiveStreamTestReflection.GetField(item, "_toggle");
            Assert.That(toggle, Is.Not.Null, "条目未挂 Toggle，无法设置选中态");
            LiveStreamTestReflection.SetProperty(toggle, "isOn", isOn);
        }
    }
}
