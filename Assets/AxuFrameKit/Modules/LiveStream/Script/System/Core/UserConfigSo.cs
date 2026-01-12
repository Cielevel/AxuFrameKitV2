using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit._Core.Structure;
using AxuFrameKit._Core.Utility.RandomX;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AxuFrameKit.Modules.LiveStream
{
    [CreateAssetMenu(fileName = "UserConfig", menuName = "AxuFrameKit/LiveStream/UserConfig")]
    public class UserConfigSo : ScriptableObject
    {
        [Header("可右键一键添加")]

        [SerializeField] private int _generateCount;
        [SerializeField] private int _userIdLength;
        [SerializeField] private IntBound _userNicknameLength;

        [Space]

        [SerializeField] private List<UserDataUnit> _userDataUnitList;

        [ContextMenu("生成用户数据")]
        private void GenerateUserData()
        {
            _userDataUnitList.Clear();
            for (int i = 0; i < _generateCount; i++)
            {
                _userDataUnitList.Add(new UserDataUnit
                {
                    Uid = RandomId.Get(_userIdLength),
                    Nickname = RandomNickname.GetCombine(Random.Range(_userNicknameLength.min, _userNicknameLength.max))
                });
            }
        }

#if UNITY_EDITOR
        [ContextMenu("填充选中的Sprite到Avatar")]
        private void FillAvatarsFromSelection()
        {
            // Get selected objects in Project window
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogWarning("[UserConfigSo] 请先在 Project 窗口中选择 Sprite 或 Texture2D 资源！");
                return;
            }

            if (_userDataUnitList == null || _userDataUnitList.Count == 0)
            {
                Debug.LogWarning("[UserConfigSo] userDataUnitList 为空，请先生成用户数据！");
                return;
            }

            // Collect sprites from selection (support both Sprite and Texture2D)
            List<Sprite> sprites = new List<Sprite>();
            foreach (var obj in selectedObjects)
            {
                if (obj is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
                else if (obj is Texture2D texture)
                {
                    // Try to get sprite from texture asset
                    string path = AssetDatabase.GetAssetPath(texture);
                    var loadedSprites = AssetDatabase.LoadAllAssetsAtPath(path);
                    bool foundSprite = false;
                    foreach (var loaded in loadedSprites)
                    {
                        if (loaded is Sprite s)
                        {
                            sprites.Add(s);
                            foundSprite = true;
                            break; // Only take the first sprite from each texture
                        }
                    }
                    // If no sprite found, log warning
                    if (!foundSprite)
                    {
                        Debug.LogWarning($"[UserConfigSo] Texture2D '{texture.name}' 没有关联的 Sprite，请确保 Texture Type 设置为 Sprite。");
                    }
                }
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning("[UserConfigSo] 选中的对象中没有有效的 Sprite！");
                return;
            }

            // Fill avatars
            int fillCount = Mathf.Min(sprites.Count, _userDataUnitList.Count);
            for (int i = 0; i < fillCount; i++)
            {
                _userDataUnitList[i].Avatar = sprites[i];
            }

            // Mark dirty for save
            EditorUtility.SetDirty(this);

            Debug.Log($"[UserConfigSo] 已填充 {fillCount} 个 Avatar。" +
                      (sprites.Count < _userDataUnitList.Count
                          ? $" (剩余 {_userDataUnitList.Count - fillCount} 个用户未填充)"
                          : "") +
                      (sprites.Count > _userDataUnitList.Count
                          ? $" (多余 {sprites.Count - _userDataUnitList.Count} 个 Sprite 未使用)"
                          : ""));
        }

        [ContextMenu("清空所有Avatar")]
        private void ClearAllAvatars()
        {
            if (_userDataUnitList == null || _userDataUnitList.Count == 0)
            {
                Debug.LogWarning("[UserConfigSo] userDataUnitList 为空！");
                return;
            }

            foreach (var user in _userDataUnitList)
            {
                user.Avatar = null;
            }

            EditorUtility.SetDirty(this);
            Debug.Log($"[UserConfigSo] 已清空 {_userDataUnitList.Count} 个用户的 Avatar。");
        }
#endif
    }
}
