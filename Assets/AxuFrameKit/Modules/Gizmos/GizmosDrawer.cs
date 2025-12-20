using System;
using System.Collections.Generic;
using AxuFrameKit.Common.CustomAttribute.PolymorphicList;
using UnityEngine;

namespace AxuFrameKit.Modules.Gizmos
{
    public class GizmosDrawer : MonoBehaviour
    {
        [SerializeReference]
        [PolymorphicList] 
        private List<BaseDrawer> _drawers = new List<BaseDrawer>();

        [Serializable]
        public abstract class BaseDrawer { } // 必须是 public

        [Serializable]
        public class LineDrawerV3 : BaseDrawer // 必须是 public
        {
            public Vector3 start;
            public Vector3 end;
        }
        
        [Serializable]
        public class LineDrawerTsf : BaseDrawer // 必须是 public
        {
            public Transform start;
            public Transform end;
        }
    }
}