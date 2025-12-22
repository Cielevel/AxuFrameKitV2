using System;
using UnityEngine;

namespace AxuFrameKit.Modules.GizmosUtil
{
    public partial class GizmosDrawer
    {
        #region Line
        
        [Serializable]
        public class LineDrawerV3 : BaseDrawer
        {
            public Vector3 start;
            public Vector3 end;
            
            public override void Draw()
            {
                base.Draw();

                Gizmos.DrawLine(start, end);
            }
        }
        
        [Serializable]
        public class LineDrawerTsf : BaseDrawer
        {
            public Transform start;
            public Transform end;
            
            public override void Draw()
            {
                if (start == null || end == null)
                    return;
                
                base.Draw();
                
                Gizmos.DrawLine(start.position, end.position);
            }
        }
        
        #endregion
    }
}