using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit.Common.CustomAttribute;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AxuFrameKit.Modules.LiveStream
{
    [CreatorEntry("直播互动/配置/GiftConfigSo")]
    public class GiftConfigSo : ScriptableObject
    {
        [SerializeField] private List<GiftDataUnit> _giftDataUnitList;

        private void OnValidate()
        {
            int index = 0;
            _giftDataUnitList?.ForEach(unit => unit.elementName = $"({index++}){unit.Name}|price:{unit.Price}");
        }

#if UNITY_EDITOR
        [ContextMenu("从选中图片生成礼物列表")]
        private void GenerateGiftsFromSelection()
        {
            // Get selected objects in Project window
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogWarning("[GiftConfigSo] 请先在 Project 窗口中选择 Sprite 或 Texture2D 资源！");
                return;
            }

            // Initialize list if null
            if (_giftDataUnitList == null)
            {
                _giftDataUnitList = new List<GiftDataUnit>();
            }

            // Clear existing list
            _giftDataUnitList.Clear();

            // Collect sprites from selection (support both Sprite and Texture2D)
            foreach (var obj in selectedObjects)
            {
                Sprite sprite = null;
                string assetName = obj.name;

                if (obj is Sprite s)
                {
                    sprite = s;
                }
                else if (obj is Texture2D texture)
                {
                    // Try to get sprite from texture asset
                    string path = AssetDatabase.GetAssetPath(texture);
                    var loadedAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var loaded in loadedAssets)
                    {
                        if (loaded is Sprite loadedSprite)
                        {
                            sprite = loadedSprite;
                            break; // Only take the first sprite from each texture
                        }
                    }

                    if (sprite == null)
                    {
                        Debug.LogWarning($"[GiftConfigSo] Texture2D '{texture.name}' 没有关联的 Sprite，请确保 Texture Type 设置为 Sprite。已跳过。");
                        continue;
                    }
                }
                else
                {
                    // Not a valid image asset
                    continue;
                }

                // Create new GiftDataUnit
                var giftUnit = new GiftDataUnit
                {
                    Name = assetName,
                    Img = sprite,
                    Price = 0 // Default price, can be edited later
                };

                _giftDataUnitList.Add(giftUnit);
            }

            // Mark dirty for save
            EditorUtility.SetDirty(this);

            if (_giftDataUnitList.Count > 0)
            {
                Debug.Log($"[GiftConfigSo] 已从选中图片生成 {_giftDataUnitList.Count} 个礼物。请手动设置 Price。");
            }
            else
            {
                Debug.LogWarning("[GiftConfigSo] 选中的对象中没有有效的 Sprite！");
            }
        }

        [ContextMenu("清空礼物列表")]
        private void ClearGiftList()
        {
            if (_giftDataUnitList == null)
            {
                _giftDataUnitList = new List<GiftDataUnit>();
            }

            int count = _giftDataUnitList.Count;
            _giftDataUnitList.Clear();

            EditorUtility.SetDirty(this);
            Debug.Log($"[GiftConfigSo] 已清空 {count} 个礼物。");
        }
#endif
    }
}
