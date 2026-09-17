using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AxuFrameKit.Common.UI.Framework;
using AxuTestFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AxuFrameKit.Tests.Runtime
{
    /// <summary>
    /// [Axu/Play] UI 框架 Manager 体系守护测试：惰性收集、按类型/ID 查找、注册注销、销毁残留过滤、Start 竞争回归。
    /// 全程运行时动态建 Canvas/Manager/Page，不依赖场景资产；私有序列化字段（Content/HideContentOnStart 等）
    /// 经反射注入（PlayMode 测试程序集无 UnityEditor，不能用 SerializedObject），测试后彻底清理。
    /// </summary>
    [Category("Axu/Play")]
    public class UIFrameworkPlayModeTests
    {
        /// <summary>测试专用封闭 Manager 子类（UIBaseManager&lt;T&gt; 为自引用泛型基类，需封闭后才能挂载）。</summary>
        private sealed class TestUiManager : UIBaseManager<TestUiManager> { }

        private sealed class TestPageA : UIBasePage { }

        private sealed class TestPageB : UIBasePage { }

        private sealed class TestAreaA : UIBaseArea { }

        /// <summary>带 OnShow 计数的页面，用于断言 null-Content 分支确实走到了 Show。</summary>
        private sealed class TestPageCounting : UIBasePage
        {
            public int ShowCalls;

            protected override void OnShow() => ShowCalls++;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AxuTestState.ResetStatics();
            yield return AxuWait.ForFrames(1);
        }

        /// <summary>动态建 UI 根物体（含 Canvas），子页面挂好后再由调用方 AddComponent Manager。</summary>
        private static GameObject NewUiRoot()
        {
            var root = new GameObject("UIRoot");
            root.AddComponent<Canvas>();
            return root;
        }

        /// <summary>
        /// 在 parent 下动态建页面并注入 Content（默认指向自身物体）/HideContentOnStart。
        /// 默认初始隐藏（activeSelf=false），保证显隐断言确定性；Start 竞争用例传 initiallyActive=true。
        /// </summary>
        private static TPage BuildPage<TPage>(Transform parent, string id, bool hideOnStart = false,
            bool initiallyActive = false) where TPage : UIBaseComponent
        {
            var go = new GameObject(typeof(TPage).Name);
            go.transform.SetParent(parent);
            var page = go.AddComponent<TPage>();
            page.ID = id;
            SetPrivateField(page, "<HideContentOnStart>k__BackingField", hideOnStart);
            // 以自身物体作为 Content，便于用 activeSelf 断言显隐
            SetPrivateField(page, "<Content>k__BackingField", go);
            go.SetActive(initiallyActive);
            return page;
        }

        /// <summary>反射注入 UIBaseComponent 的私有字段（含自动属性 backing field），字段缺失时直接判失败。</summary>
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = typeof(UIBaseComponent).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"UIBaseComponent 应存在私有字段 {fieldName}（反射注入依赖）");
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator ShowPage_Awake未执行列表未填充_惰性收集不NRE并可命中()
        {
            var root = NewUiRoot();
            try
            {
                root.SetActive(false); // 根未激活：AddComponent 不触发 Awake，复现“早于 Awake 调用”的旧缺陷前置态
                var page = BuildPage<TestPageA>(root.transform, "alpha");
                var area = BuildPage<TestAreaA>(root.transform, "area-a");
                var mgr = root.AddComponent<TestUiManager>();

                // Act：早于 Awake 调用各公开入口（旧实现在此 NRE）
                UIBasePage found = null;
                Assert.DoesNotThrow(() => found = mgr.GetPage<TestPageA>(), "Awake 前调用应触发惰性收集而非 NRE");
                Assert.DoesNotThrow(() => mgr.ShowPage<TestPageA>());
                Assert.DoesNotThrow(() => mgr.ShowArea<TestAreaA>());

                // Assert：惰性收集后可正常命中
                Assert.That(found, Is.SameAs(page));
                Assert.That(mgr.IsPageShow<TestPageA>(), Is.True);
                Assert.That(mgr.GetArea<TestAreaA>(), Is.SameAs(area));
            }
            finally
            {
                Object.Destroy(root);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator ShowPage_HidePage_IsPageShow_GetPage_子物体页面按类型与ID基本流()
        {
            var root = NewUiRoot();
            try
            {
                var pageA = BuildPage<TestPageA>(root.transform, "alpha");
                var pageB = BuildPage<TestPageB>(root.transform, "beta");
                var mgr = root.AddComponent<TestUiManager>(); // 子页面已就位，Awake 即完成收集

                // Show / IsPageShow：无参重载按类型命中
                mgr.ShowPage<TestPageA>();
                Assert.That(mgr.IsPageShow<TestPageA>(), Is.True);
                Assert.That(mgr.IsPageShow<TestPageB>(), Is.False, "ShowPage<TestPageA> 不应影响 TestPageB");

                // id 重载：类型 + id 联合命中；id 不匹配则不动作
                mgr.ShowPage<TestPageB>("beta");
                Assert.That(mgr.IsPageShow<TestPageB>("beta"), Is.True);
                Assert.DoesNotThrow(() => mgr.ShowPage<TestPageA>("gamma"));
                Assert.That(mgr.IsPageShow<TestPageA>(), Is.True, "id 未命中时不应改变现有状态");

                // Hide
                mgr.HidePage<TestPageA>();
                Assert.That(mgr.IsPageShow<TestPageA>(), Is.False);
                Assert.That(mgr.IsPageShow<TestPageB>(), Is.True);

                // GetPage
                Assert.That(mgr.GetPage<TestPageA>(), Is.SameAs(pageA));
                Assert.That(mgr.GetPage<TestPageB>("beta"), Is.SameAs(pageB));
                Assert.That(mgr.GetPage<TestPageA>("beta"), Is.Null, "GetPage 类型与 id 不符应返回 null");
            }
            finally
            {
                Object.Destroy(root);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator IsPageShow_ID重载同ID异类型_仅匹配指定T1不误中()
        {
            var root = NewUiRoot();
            try
            {
                BuildPage<TestPageA>(root.transform, "same");
                BuildPage<TestPageB>(root.transform, "same"); // 与 A 同 id
                var mgr = root.AddComponent<TestUiManager>();

                // 仅显示 A（列表中首个 id=same 的页面），查询 B：
                // 若 id 重载漏掉 T1 过滤会误中 A（activeSelf=true）而错误返回 true
                mgr.ShowPage<TestPageA>();
                Assert.That(mgr.IsPageShow<TestPageB>("same"), Is.False, "IsPageShow(id) 必须按 T1 过滤，不应误中同 id 的 TestPageA");
                Assert.That(mgr.IsPageShow<TestPageA>("same"), Is.True);

                // 反向：显示 B 后隐藏 A，两个重载各自命中
                mgr.ShowPage<TestPageB>("same");
                mgr.HidePage<TestPageA>("same");
                Assert.That(mgr.IsPageShow<TestPageA>("same"), Is.False);
                Assert.That(mgr.IsPageShow<TestPageB>("same"), Is.True);
            }
            finally
            {
                Object.Destroy(root);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator IsPageShow_Content为null_返回false不抛异常()
        {
            var root = NewUiRoot();
            try
            {
                var pageGo = new GameObject("NullContentPage");
                pageGo.transform.SetParent(root.transform);
                var page = pageGo.AddComponent<TestPageA>();
                page.ID = "null-content";
                SetPrivateField(page, "<HideContentOnStart>k__BackingField", false);
                // 不注入 Content，保持 null
                var mgr = root.AddComponent<TestUiManager>();
                Assert.That(mgr.GetPage<TestPageA>(), Is.SameAs(page), "Content 为 null 不影响页面被收集命中");

                bool result = true;
                Assert.DoesNotThrow(() => result = mgr.IsPageShow<TestPageA>());
                Assert.That(result, Is.False, "Content 为 null 时应安全返回 false");
                Assert.DoesNotThrow(() => mgr.ShowPage<TestPageA>(), "Content 为 null 时 Show 不应抛异常");
            }
            finally
            {
                Object.Destroy(root);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator Toggle_Content为null_不抛异常并走显示分支()
        {
            var root = NewUiRoot();
            try
            {
                var pageGo = new GameObject("ToggleNullPage");
                pageGo.transform.SetParent(root.transform);
                var page = pageGo.AddComponent<TestPageCounting>();
                page.ID = "toggle-null";
                var mgr = root.AddComponent<TestUiManager>();

                // Content 为 null：Toggle 的显隐判断走 else 分支 → Show，全程不抛
                Assert.DoesNotThrow(() => mgr.TogglePage<TestPageCounting>());
                Assert.That(page.ShowCalls, Is.EqualTo(1), "Content 为 null 时 Toggle 应走 Show 分支并触发 OnShow");

                Assert.DoesNotThrow(() => page.Toggle()); // 组件直调重载亦不抛
                Assert.That(page.ShowCalls, Is.EqualTo(2));
            }
            finally
            {
                Object.Destroy(root);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator RegisterPage_UnregisterPage_动态登记后可查注销后失联且判重跳过()
        {
            var root = NewUiRoot();
            GameObject outsideGo = null;
            GameObject outsideAreaGo = null;
            try
            {
                var mgr = root.AddComponent<TestUiManager>(); // Manager 先建，之后的物体不会被 Awake 快照收集

                outsideGo = new GameObject("OutsidePage");
                outsideGo.transform.SetParent(root.transform.parent); // 挂到与 root 同级，确保不在 Manager 子树下
                var page = outsideGo.AddComponent<TestPageA>();
                page.ID = "outside";
                SetPrivateField(page, "<Content>k__BackingField", outsideGo);
                Assert.That(mgr.GetPage<TestPageA>(), Is.Null, "非子物体且未登记的动态页面不应被找到");

                // 登记 → 可查可显
                mgr.RegisterPage(page);
                Assert.That(mgr.GetPage<TestPageA>(), Is.SameAs(page));
                mgr.ShowPage<TestPageA>();
                Assert.That(mgr.IsPageShow<TestPageA>(), Is.True);

                // 重复登记：判重跳过（反射查内部列表确认无重复项）
                mgr.RegisterPage(page);
                var listField = typeof(TestUiManager).BaseType.GetField("_uiPageList",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(listField, Is.Not.Null, "UIBaseManager._uiPageList 字段应存在（反射断言依赖）");
                var list = (List<UIBasePage>)listField.GetValue(mgr);
                Assert.That(list.FindAll(e => ReferenceEquals(e, page)).Count, Is.EqualTo(1), "重复 Register 应判重跳过");

                // 注销 → 失联
                mgr.UnregisterPage(page);
                Assert.That(mgr.GetPage<TestPageA>(), Is.Null, "注销后不应再被找到");
                Assert.That(mgr.IsPageShow<TestPageA>(), Is.False);

                // 区域侧对称验证
                outsideAreaGo = new GameObject("OutsideArea");
                var area = outsideAreaGo.AddComponent<TestAreaA>();
                SetPrivateField(area, "<Content>k__BackingField", outsideAreaGo);
                Assert.That(mgr.GetArea<TestAreaA>(), Is.Null);
                mgr.RegisterArea(area);
                Assert.That(mgr.GetArea<TestAreaA>(), Is.SameAs(area));
                mgr.UnregisterArea(area);
                Assert.That(mgr.GetArea<TestAreaA>(), Is.Null);
            }
            finally
            {
                Object.Destroy(root);
                if (outsideGo != null) Object.Destroy(outsideGo); // 动态页/区域不在 root 层级下，按引用单独清理
                if (outsideAreaGo != null) Object.Destroy(outsideAreaGo);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator ShowPage_页面已销毁_过滤残留项不抛MissingReference()
        {
            var root = NewUiRoot();
            try
            {
                var mgr = root.AddComponent<TestUiManager>();

                var pageGo = new GameObject("DeadPage");
                pageGo.transform.SetParent(root.transform);
                var page = pageGo.AddComponent<TestPageA>();
                page.ID = "dead";
                SetPrivateField(page, "<Content>k__BackingField", pageGo);

                mgr.RegisterPage(page); // Manager 建在子物体之前，动态页需显式登记
                mgr.ShowPage<TestPageA>();
                Assert.That(mgr.IsPageShow<TestPageA>(), Is.True);

                Object.Destroy(pageGo);
                yield return null; // 帧末真正销毁，列表残留 Unity 假 null 项（其 is T1 仍为 true）

                // 旧实现：Find 命中假 null 项 → ?.Show() → 访问已销毁 Content 抛 MissingReferenceException
                Assert.DoesNotThrow(() => mgr.ShowPage<TestPageA>(), "已销毁页面应被查找核心判活过滤，而非抛 MissingReferenceException");
                Assert.That(mgr.GetPage<TestPageA>(), Is.Null, "已销毁页面不应再被 GetPage 返回");
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator UIBaseComponent_Start竞争_实例化后立即Show不被HideOnStart覆盖()
        {
            var root = new GameObject("StartRaceRoot");
            try
            {
                // 两个 HideContentOnStart=true 的页面（初始激活，Start 会在下一帧执行）：一个立即手动 Show，一个保持未交互
                var shown = BuildPage<TestPageA>(root.transform, "shown", hideOnStart: true, initiallyActive: true);
                var hidden = BuildPage<TestPageA>(root.transform, "hidden", hideOnStart: true, initiallyActive: true);

                shown.Show(); // 实例化后立即手动显示（Start 尚未执行）
                yield return null; // 等一帧，Start 执行

                Assert.That(shown.Content.activeSelf, Is.True, "手动 Show 后 Start 不应因 HideContentOnStart 再隐藏（Start 竞争回归）");
                Assert.That(hidden.Content.activeSelf, Is.False, "未交互过的页面 Start 应照常执行开局隐藏");
            }
            finally
            {
                Object.Destroy(root);
            }
        }
    }
}
