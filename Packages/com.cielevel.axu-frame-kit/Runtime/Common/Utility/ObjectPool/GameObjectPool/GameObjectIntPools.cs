using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    [CreatorEntry("框架/对象池/GameObjectIntPools")]
    public class GameObjectIntPools : GameObjectPools<int> { }

    [Serializable]
    public class ObjectUnitInt : ObjectUnit<int> { }
}