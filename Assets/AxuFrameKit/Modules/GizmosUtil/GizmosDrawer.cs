using System;
using System.Collections.Generic;
using UnityEngine;

namespace AxuFrameKit.Modules.GizmosUtil
{
    public partial class GizmosDrawer : MonoBehaviour
    {
        [SerializeReference] public List<BaseDrawer> _drawerList;

        private void OnDrawGizmos()
        {
            if (_drawerList is not { Count: > 0 }) 
                return;
            
            foreach (var drawer in _drawerList)
                drawer.Draw();
        }

        [Serializable]
        public abstract class BaseDrawer // 必须是 public
        {
            public Color gizmosColor = Color.white;

            public virtual void Draw()
            {
                UnityEngine.Gizmos.color  = gizmosColor;
                // UnityEngine.Gizmos.matrix = default;
            }
        }
    }
}