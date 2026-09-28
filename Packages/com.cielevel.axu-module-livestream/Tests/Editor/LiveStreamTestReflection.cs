using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream.Tests.Editor
{
    /// <summary>
    /// [Axu/Module/LiveStream] 测试反射基建：统一封装私有序列化字段注入、
    /// 非公开属性读写与无参私有方法调用（与既有测试的反射注入纪律一致）。
    /// </summary>
    internal static class LiveStreamTestReflection
    {
        internal static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"字段 {target.GetType().Name}.{name} 不存在");
            field.SetValue(target, value);
        }

        internal static object GetField(object target, string name)
        {
            var field = target.GetType().GetField(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"字段 {target.GetType().Name}.{name} 不存在");
            return field.GetValue(target);
        }

        internal static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"属性 {target.GetType().Name}.{name} 不存在");
            property.SetValue(target, value);
        }

        internal static object GetProperty(object target, string name)
        {
            var property = target.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"属性 {target.GetType().Name}.{name} 不存在");
            return property.GetValue(target);
        }

        internal static object Call(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"方法 {target.GetType().Name}.{methodName} 不存在");
            return method.Invoke(target, null);
        }

        internal static object InvokeWithArgs(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"方法 {target.GetType().Name}.{methodName} 不存在");
            return method.Invoke(target, args);
        }
    }
}
