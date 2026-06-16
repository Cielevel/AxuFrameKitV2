using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit._Core.Advanced.ObjectPool;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    [CreateAssetMenu(fileName = "GameObjectPool-Int", menuName = "AxuFrameKit/Utility/GameObjectPools-Int")]
    public class GameObjectIntPools : GameObjectPools<int> { }

    [Serializable]
    public class ObjectUnitInt : ObjectUnit<int> { }
}