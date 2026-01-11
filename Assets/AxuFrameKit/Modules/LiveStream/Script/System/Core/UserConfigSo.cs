using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit._Core.Structure;
using AxuFrameKit._Core.Utility.RandomX;

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
    }
}
