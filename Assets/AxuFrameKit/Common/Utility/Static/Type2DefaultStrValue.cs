using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AxuFrameKit.Common.Utility.Static
{
    public static class Type2DefaultStrValue
    {
        private static readonly Dictionary<Type, string> _type2StrValue = new()
        {
            { typeof(string), "" },
            { typeof(int), "0" },
            { typeof(float), "0f" },
            { typeof(bool), "false" },
            { typeof(double), "0d" },
            { typeof(long), "0L" },
            { typeof(short), "0s" },
            { typeof(byte), "0b" },
            { typeof(char), "'\0'" },
            { typeof(ushort), "0us" },
            { typeof(uint), "0u" },
            { typeof(ulong), "0ul" },
            { typeof(sbyte), "0sb" },
            { typeof(decimal), "0m" },
            { typeof(float), "0f" },
            { typeof(double), "0d" },
            { typeof(long), "0L" },
            { typeof(short), "0s" },
            { typeof(byte), "0b" },
            { typeof(char), "'\0'" },
            { typeof(ushort), "0us" },
            { typeof(uint), "0u" },
            { typeof(ulong), "0ul" },
            { typeof(sbyte), "0sb" },
            { typeof(decimal), "0m" },
            { typeof(Vector2), "(0f,0f)" },
            { typeof(Vector3), "(0f,0f,0f)" },
            { typeof(Vector4), "(0f,0f,0f,0f)" },
            { typeof(Quaternion), "(0f,0f,0f,0f)" },
            { typeof(Color), "(1f,1f,1f,1f)" },
            { typeof(Rect), "(0f,0f,0f,0f)" },
            { typeof(Bounds), "(0f,0f,0f,0f)" },
            { typeof(AnimationCurve), "new AnimationCurve()" },
            { typeof(Gradient), "new Gradient()" },
            { typeof(LayerMask), "LayerMask" },
            { typeof(Matrix4x4), "Matrix4x4" },
            { typeof(RectOffset), "RectOffset" },
            { typeof(Vector2Int), "(0,0)" },
            { typeof(Color32), "(0,0,0,0)" },
            { typeof(RectInt), "(0,0,0,0)" },
            { typeof(Vector3Int), "(0,0,0)" },
            { typeof(BoundsInt), "(0,0,0,0)" },
            { typeof(Image), "null" },
            { typeof(Text), "null" },
            { typeof(Button), "null" },
            { typeof(Slider), "null" },
            { typeof(Scrollbar), "null" },
            { typeof(ScrollRect), "null" },
            { typeof(Toggle), "null" },
            { typeof(Dropdown), "null" },
            { typeof(TMP_Text), "null" },
            { typeof(TMP_InputField), "null" },
            { typeof(TMP_Dropdown), "null" },
        };

        public static string GetDefaultValueStr(Type type)
        {
            if (_type2StrValue.TryGetValue(type, out var name))
                return name;
            return type.Name;
        }
    }
}