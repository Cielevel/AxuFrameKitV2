using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AxuFrameKit.Common.Utility.Static
{
    public static class Type2Name
    {
        private static readonly Dictionary<Type, string> _baseTypeToName = new()
        {
            { typeof(string), "string" },
            { typeof(int), "int" },
            { typeof(float), "float" },
            { typeof(bool), "bool" },
            { typeof(double), "double" },
            { typeof(long), "long" },
            { typeof(short), "short" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(ushort), "ushort" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(sbyte), "sbyte" },
            { typeof(decimal), "decimal" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(long), "long" },
            { typeof(short), "short" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(ushort), "ushort" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(sbyte), "sbyte" },
            { typeof(decimal), "decimal" },
            { typeof(Vector2), "Vector2" },
            { typeof(Vector3), "Vector3" },
            { typeof(Vector4), "Vector4" },
            { typeof(Quaternion), "Quaternion" },
            { typeof(Color), "Color" },
            { typeof(Rect), "Rect" },
            { typeof(Bounds), "Bounds" },
            { typeof(AnimationCurve), "AnimationCurve" },
            { typeof(Gradient), "Gradient" },
            { typeof(LayerMask), "LayerMask" },
            { typeof(Matrix4x4), "Matrix4x4" },
            { typeof(RectOffset), "RectOffset" },
            { typeof(Vector2Int), "Vector2Int" },
            { typeof(Color32), "Color32" },
            { typeof(RectInt), "RectInt" },
            { typeof(Vector3Int), "Vector3Int" },
            { typeof(BoundsInt), "BoundsInt" },
            { typeof(Image), "Image" },
            { typeof(Text), "Text" },
            { typeof(Button), "Button" },
            { typeof(Slider), "Slider" },
            { typeof(Scrollbar), "Scrollbar" },
            { typeof(ScrollRect), "ScrollRect" },
            { typeof(Toggle), "Toggle" },
            { typeof(Dropdown), "Dropdown" },
            { typeof(TMP_Text), "TMP_Text" },
            { typeof(TMP_InputField), "TMP_InputField" },
            { typeof(TMP_Dropdown), "TMP_Dropdown" },
        };

        public static string GetName(Type type)
        {
            if (_baseTypeToName.TryGetValue(type, out var name))
                return name;
            return type.Name;
        }
    }
}