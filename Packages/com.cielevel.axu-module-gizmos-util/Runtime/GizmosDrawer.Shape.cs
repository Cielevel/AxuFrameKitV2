using System;
using UnityEngine;

namespace AxuFrameKit.Modules.GizmosUtil
{
    public partial class GizmosDrawer
    {
        #region Sphere

        [Serializable]
        public class SphereDrawerV3 : BaseDrawer
        {
            public Vector3 center;
            public float radius = 1f;
            
            public override void Draw()
            {
                base.Draw();
                
                Gizmos.DrawSphere(center, radius);
            }
        }
        
        [Serializable]
        public class SphereDrawerTsf : BaseDrawer
        {
            public Transform center;
            public float radius = 1f;
            
            public override void Draw()
            {
                if (center == null) return;
                
                base.Draw();
                
                Gizmos.DrawSphere(center.position, radius);
            }
        }
        
        [Serializable]
        public class WireSphereDrawerV3 : BaseDrawer
        {
            public Vector3 center;
            public float radius = 1f;
            
            public override void Draw()
            {
                base.Draw();
                
                Gizmos.DrawWireSphere(center, radius);
            }
        }
        
        [Serializable]
        public class WireSphereDrawerTsf : BaseDrawer
        {
            public Transform center;
            public float radius = 1f;
            
            public override void Draw()
            {
                base.Draw();
                
                Gizmos.DrawWireSphere(center.position, radius);
            }
        }

        #endregion
        
        #region Cube
        
        [Serializable]
        public class CubeDrawerV3 : BaseDrawer
        {
            public Vector3 center;
            public Vector3 rotationEuler = Vector3.zero;
            public Vector3 size = Vector3.one;
            
            public override void Draw()
            {
                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(rotationEuler), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = prevMatrix;
            }
        }

        [Serializable]
        public class CubeDrawerTsf : BaseDrawer
        {
            public Transform center;
            public Vector3 size = Vector3.one;
            
            public override void Draw()
            {
                if (center == null) return;
                
                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center.position, center.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.matrix = prevMatrix;
            }
        }
        
        [Serializable]
        public class WireCubeDrawerV3 : BaseDrawer
        {
            public Vector3 center;
            public Vector3 rotationEuler = Vector3.zero;
            public Vector3 size = Vector3.one;
            
            public override void Draw()
            {
                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(rotationEuler), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = prevMatrix;
            }
        }
        
        [Serializable]
        public class WireCubeDrawerTsf : BaseDrawer
        {
            public Transform center;
            public Vector3 size = Vector3.one;
            
            public override void Draw()
            {
                if (center == null) return;
                
                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center.position, center.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = prevMatrix;
            }
        }

        #endregion

        #region Rectangle-2D

        [Serializable]
        public class RectangleDrawerV3 : BaseDrawer
        {
            public Vector3 center;
            public Vector2 size = Vector2.one;
            public Vector3 rotationEuler = Vector3.zero;
            
            public override void Draw()
            {
                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(rotationEuler), Vector3.one);
        
                float hx = size.x * 0.5f;
                float hy = size.y * 0.5f;
                var p0 = new Vector3(-hx, 0f, -hy);
                var p1 = new Vector3(hx, 0f, -hy);
                var p2 = new Vector3(hx, 0f, hy);
                var p3 = new Vector3(-hx, 0f, hy);
        
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
                Gizmos.DrawLine(p3, p0);
        
                Gizmos.matrix = prevMatrix;
            }
        }
        
        [Serializable]
        public class RectangleDrawerTsf : BaseDrawer
        {
            public Transform center;
            public Vector2 size = Vector2.one;

            public override void Draw()
            {
                base.Draw();

                if (center == null) return;

                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center.position, center.rotation, Vector3.one);

                float hx = size.x * 0.5f;
                float hy = size.y * 0.5f;
                var   p0 = new Vector3(-hx, 0f, -hy);
                var   p1 = new Vector3(hx, 0f, -hy);
                var   p2 = new Vector3(hx, 0f, hy);
                var   p3 = new Vector3(-hx, 0f, hy);

                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
                Gizmos.DrawLine(p3, p0);

                Gizmos.matrix = prevMatrix;
            }
        }

        #endregion

        #region Circle-2D

        [Serializable]
        public class CircleDrawerV3 : BaseDrawer
        {
            public Vector3 center;
            public Vector3 rotationEuler = Vector3.zero;
            public float radius = 1f;
            public int segments = 36;
            
            public override void Draw()
            {
                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(rotationEuler), Vector3.one);
                
                float angleStep = 360f / segments;
                Vector3 prevPoint = new Vector3(radius, 0f, 0f);
                
                for (int i = 1; i <= segments; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 newPoint = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    Gizmos.DrawLine(prevPoint, newPoint);
                    prevPoint = newPoint;
                }
                
                Gizmos.matrix = prevMatrix;
            }
        }
        
        [Serializable]
        public class CircleDrawerTsf : BaseDrawer
        {
            public Transform center;
            public float radius = 1f;
            public int segments = 36;
            
            public override void Draw()
            {
                if (center == null) return;
                
                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center.position, center.rotation, Vector3.one);
                
                float   angleStep = 360f / segments;
                Vector3 prevPoint = new Vector3(radius, 0f, 0f);
                
                for (int i = 1; i <= segments; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 newPoint = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    Gizmos.DrawLine(prevPoint, newPoint);
                    prevPoint = newPoint;
                }
                
                Gizmos.matrix = prevMatrix;
            }
        }

        #endregion
        
        #region Capsule

        [Serializable]
        public class CapsuleDrawerTsf : BaseDrawer
        {
            public Transform center;
            public float radius = 0.5f;
            public float height = 2f;
            public int segments = 16;

            public override void Draw()
            {
                if (center == null) return;
                
                base.Draw();

                Vector3 centerPos = center.position;

                float cylinderHeight = Mathf.Max(0, height - radius * 2);
                Vector3 top = centerPos + Vector3.up * (cylinderHeight * 0.5f);
                Vector3 bottom = centerPos - Vector3.up * (cylinderHeight * 0.5f);

                // 绘制圆柱体部分
                DrawCircle(top, radius, segments);
                DrawCircle(bottom, radius, segments);

                // 绘制连接线
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    Gizmos.DrawLine(bottom + offset, top + offset);
                }

                // 绘制半球
                DrawHemisphere(top, radius, Vector3.up, segments);
                DrawHemisphere(bottom, radius, Vector3.down, segments);
            }

            private void DrawCircle(Vector3 pos, float r, int seg)
            {
                float angleStep = 360f / seg;
                Vector3 prevPoint = pos + new Vector3(r, 0f, 0f);

                for (int i = 1; i <= seg; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 newPoint = pos + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                    Gizmos.DrawLine(prevPoint, newPoint);
                    prevPoint = newPoint;
                }
            }

            private void DrawHemisphere(Vector3 pos, float r, Vector3 direction, int seg)
            {
                int halfSegments = seg / 2;
                for (int j = 0; j < 4; j++)
                {
                    float rotAngle = j * 90f;
                    Quaternion rot = Quaternion.Euler(0, rotAngle, 0);

                    Vector3 prevPoint = pos;
                    for (int i = 1; i <= halfSegments; i++)
                    {
                        float angle = i * (90f / halfSegments) * Mathf.Deg2Rad;
                        float y = Mathf.Sin(angle) * r * (direction.y > 0 ? 1 : -1);
                        float xz = Mathf.Cos(angle) * r;

                        Vector3 point = pos + direction * y + rot * new Vector3(xz, 0, 0);
                        if (i > 1)
                            Gizmos.DrawLine(prevPoint, point);
                        prevPoint = point;
                    }
                }
            }
        }
        
        [Serializable]
        public class CapsuleDrawerV3 : BaseDrawer
        {
            public Vector3 center;
            public float radius = 0.5f;
            public float height = 2f;
            public int segments = 16;

            public override void Draw()
            {
                base.Draw();

                float cylinderHeight = Mathf.Max(0, height - radius * 2);
                Vector3 top = center + Vector3.up * (cylinderHeight * 0.5f);
                Vector3 bottom = center - Vector3.up * (cylinderHeight * 0.5f);

                // 绘制圆柱体部分
                DrawCircle(top, radius, segments);
                DrawCircle(bottom, radius, segments);

                // 绘制连接线
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    Gizmos.DrawLine(bottom + offset, top + offset);
                }

                // 绘制半球
                DrawHemisphere(top, radius, Vector3.up, segments);
                DrawHemisphere(bottom, radius, Vector3.down, segments);
            }

            private void DrawCircle(Vector3 pos, float r, int seg)
            {
                float angleStep = 360f / seg;
                Vector3 prevPoint = pos + new Vector3(r, 0f, 0f);

                for (int i = 1; i <= seg; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    Vector3 newPoint = pos + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                    Gizmos.DrawLine(prevPoint, newPoint);
                    prevPoint = newPoint;
                }
            }

            private void DrawHemisphere(Vector3 pos, float r, Vector3 direction, int seg)
            {
                int halfSegments = seg / 2;
                for (int j = 0; j < 4; j++)
                {
                    float rotAngle = j * 90f;
                    Quaternion rot = Quaternion.Euler(0, rotAngle, 0);

                    Vector3 prevPoint = pos;
                    for (int i = 1; i <= halfSegments; i++)
                    {
                        float angle = i * (90f / halfSegments) * Mathf.Deg2Rad;
                        float y = Mathf.Sin(angle) * r * (direction.y > 0 ? 1 : -1);
                        float xz = Mathf.Cos(angle) * r;

                        Vector3 point = pos + direction * y + rot * new Vector3(xz, 0, 0);
                        if (i > 1)
                            Gizmos.DrawLine(prevPoint, point);
                        prevPoint = point;
                    }
                }
            }
        }

        #endregion
    }
}
