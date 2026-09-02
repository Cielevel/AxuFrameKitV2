using System;
using System.Collections.Generic;
using UnityEngine;
using AxuFrameKit._Core.Advanced.ObjectPool;
using AxuFrameKit.Common.CustomAttribute;

namespace AxuFrameKit.Common.Utility.ObjectPool.GameObjectPool
{
    [CreatorEntry("框架/对象池/GameObjectStrPools")]
    public class GameObjectStrPools : GameObjectPools<string> { }

    [Serializable]
    public class ObjectUnit : ObjectUnit<string> { }
}