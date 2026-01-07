using System;
using UnityEngine;

namespace AxuFrameKit.Modules.LiveStream.Script.System.Unit
{
    [Serializable]
    public class GiftDataUnit
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public string ImgUrl { get; set; }
        [field: SerializeField] public Sprite Img { get; set; }
        [field: SerializeField] public int Price { get; set; }
    }
}