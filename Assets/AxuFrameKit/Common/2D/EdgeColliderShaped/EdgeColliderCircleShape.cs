#if ODIN_INSPECTOR
using System.Collections.Generic;
using AxU_NextGen.Common.CustomAttribute;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AxU_NextGen.Common._2D.EdgeColliderShaped
{
    [RequireComponent(typeof(EdgeCollider2D))]
    [InspectorIntro("挂载到EdgeCollider上，然后通过右键组件或者button（odin），将以输入的参数转为EdgeCollider的圆形点位")]
    public class EdgeColliderCircleShape : CustomMonoBehaviour
    {
        [field: SerializeField] public int Solution { get; private set; } = 20;

        [field: SerializeField] public float Radius { get; private set; } = 10;

        [field: SerializeField] public bool DrawGizmos { get; private set; } = false;
        
#if ODIN_INSPECTOR
        [Button]
#endif
        [ContextMenu("生成圆形Edge碰撞")]
        public void ShapeEdgeToCircle()
        {
            var edgeCollider = GetComponent<EdgeCollider2D>();

            float angleStep = 2 * Mathf.PI / Solution;

            float halfRadius = Radius * 0.5f;

            List<Vector2> pointList = new(Solution + 1);
            for (int i = 0; i < Solution; i++)
            {
                float x = transform.position.x + halfRadius * Mathf.Cos(angleStep * i);
                float y = transform.position.y + halfRadius * Mathf.Sin(angleStep * i);
                pointList.Add(new Vector2(x, y));
            }
            
            pointList.Add(pointList[0]);

            edgeCollider.SetPoints(pointList);
        }

        private void OnDrawGizmos()
        {
            if (DrawGizmos == false)
                return;

            Gizmos.DrawWireSphere(transform.position, Radius * 0.5f);
        }
    }
}
#endif