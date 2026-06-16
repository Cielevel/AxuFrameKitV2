using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit._Core.Advanced.ObjectPool;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    [CreateAssetMenu(fileName = "GameObjectPool-Str", menuName = "AxuFrameKit/Utility/GameObjectPools-Str")]
    public class GameObjectStrPools : GameObjectPools<string> { }

    [Serializable]
    public class ObjectUnit : ObjectUnit<string> { }
}