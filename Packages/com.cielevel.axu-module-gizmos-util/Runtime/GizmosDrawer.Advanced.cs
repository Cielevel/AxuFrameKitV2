using System;
using UnityEngine;

namespace AxuFrameKit.Modules.GizmosUtil
{
    // 在 GizmosDrawer 类中添加以下内容
    public partial class GizmosDrawer
    {
        #region Bezier Curves

        [Serializable]
        public class BezierQuadraticV3 : BaseDrawer
        {
            public Vector3 startPoint;
            public Vector3 controlPoint;
            public Vector3 endPoint;
            public int segments = 20;
            public bool drawControlLines = true;
            public Color controlLineColor = new Color(1f, 1f, 1f, 0.5f);

            public override void Draw()
            {
                base.Draw();

                // 绘制控制线
                if (drawControlLines)
                {
                    var prevColor = Gizmos.color;
                    Gizmos.color = controlLineColor;
                    Gizmos.DrawLine(startPoint, controlPoint);
                    Gizmos.DrawLine(controlPoint, endPoint);
                    Gizmos.color = prevColor;
                }

                // 绘制贝塞尔曲线
                Vector3 prevPoint = startPoint;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 point = CalculateQuadraticBezier(startPoint, controlPoint, endPoint, t);
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
            }

            private Vector3 CalculateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
            {
                float u = 1 - t;
                return u * u * p0 + 2 * u * t * p1 + t * t * p2;
            }
        }

        [Serializable]
        public class BezierQuadraticTsf : BaseDrawer
        {
            public Transform startPoint;
            public Transform controlPoint;
            public Transform endPoint;
            public int segments = 20;
            public bool drawControlLines = true;
            public Color controlLineColor = new Color(1f, 1f, 1f, 0.5f);

            public override void Draw()
            {
                if (startPoint == null || controlPoint == null || endPoint == null)
                    return;

                base.Draw();

                Vector3 p0 = startPoint.position;
                Vector3 p1 = controlPoint.position;
                Vector3 p2 = endPoint.position;

                // 绘制控制线
                if (drawControlLines)
                {
                    var prevColor = Gizmos.color;
                    Gizmos.color = controlLineColor;
                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p1, p2);
                    Gizmos.color = prevColor;
                }

                // 绘制贝塞尔曲线
                Vector3 prevPoint = p0;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 point = CalculateQuadraticBezier(p0, p1, p2, t);
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
            }

            private Vector3 CalculateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
            {
                float u = 1 - t;
                return u * u * p0 + 2 * u * t * p1 + t * t * p2;
            }
        }

        [Serializable]
        public class BezierCubicV3 : BaseDrawer
        {
            public Vector3 startPoint;
            public Vector3 controlPoint1;
            public Vector3 controlPoint2;
            public Vector3 endPoint;
            public int segments = 30;
            public bool drawControlLines = true;
            public Color controlLineColor = new Color(1f, 1f, 1f, 0.5f);

            public override void Draw()
            {
                base.Draw();

                // 绘制控制线
                if (drawControlLines)
                {
                    var prevColor = Gizmos.color;
                    Gizmos.color = controlLineColor;
                    Gizmos.DrawLine(startPoint, controlPoint1);
                    Gizmos.DrawLine(controlPoint2, endPoint);
                    Gizmos.color = prevColor;
                }

                // 绘制贝塞尔曲线
                Vector3 prevPoint = startPoint;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 point = CalculateCubicBezier(startPoint, controlPoint1, controlPoint2, endPoint, t);
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
            }

            private Vector3 CalculateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                float u = 1 - t;
                float uu = u * u;
                float uuu = uu * u;
                float tt = t * t;
                float ttt = tt * t;

                return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
            }
        }

        [Serializable]
        public class BezierCubicTsf : BaseDrawer
        {
            public Transform startPoint;
            public Transform controlPoint1;
            public Transform controlPoint2;
            public Transform endPoint;
            public int segments = 30;
            public bool drawControlLines = true;
            public Color controlLineColor = new Color(1f, 1f, 1f, 0.5f);

            public override void Draw()
            {
                if (startPoint == null || controlPoint1 == null || controlPoint2 == null || endPoint == null)
                    return;

                base.Draw();

                Vector3 p0 = startPoint.position;
                Vector3 p1 = controlPoint1.position;
                Vector3 p2 = controlPoint2.position;
                Vector3 p3 = endPoint.position;

                // 绘制控制线
                if (drawControlLines)
                {
                    var prevColor = Gizmos.color;
                    Gizmos.color = controlLineColor;
                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p2, p3);
                    Gizmos.color = prevColor;
                }

                // 绘制贝塞尔曲线
                Vector3 prevPoint = p0;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 point = CalculateCubicBezier(p0, p1, p2, p3, t);
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
            }

            private Vector3 CalculateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                float u = 1 - t;
                float uu = u * u;
                float uuu = uu * u;
                float tt = t * t;
                float ttt = tt * t;

                return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
            }
        }

        #endregion

        #region Arrow

        [Serializable]
        public class ArrowDrawerV3 : BaseDrawer
        {
            public Vector3 start;
            public Vector3 end;
            public float arrowHeadLength = 0.5f;
            public float arrowHeadAngle = 20f;

            public override void Draw()
            {
                base.Draw();

                // 绘制主线
                Gizmos.DrawLine(start, end);

                // 计算箭头
                Vector3 direction = (end - start).normalized;
                Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
                Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

                Gizmos.DrawLine(end, end + right * arrowHeadLength);
                Gizmos.DrawLine(end, end + left * arrowHeadLength);
            }
        }

        [Serializable]
        public class ArrowDrawerTsf : BaseDrawer
        {
            public Transform start;
            public Transform end;
            public float arrowHeadLength = 0.5f;
            public float arrowHeadAngle = 20f;

            public override void Draw()
            {
                if (start == null || end == null)
                    return;

                base.Draw();

                Vector3 startPos = start.position;
                Vector3 endPos = end.position;

                // 绘制主线
                Gizmos.DrawLine(startPos, endPos);

                // 计算箭头
                Vector3 direction = (endPos - startPos).normalized;
                Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
                Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

                Gizmos.DrawLine(endPos, endPos + right * arrowHeadLength);
                Gizmos.DrawLine(endPos, endPos + left * arrowHeadLength);
            }
        }

        #endregion

        #region Ray & Direction

        [Serializable]
        public class RayDrawerV3 : BaseDrawer
        {
            public Vector3 origin;
            public Vector3 direction = Vector3.forward;
            public float length = 5f;

            public override void Draw()
            {
                base.Draw();
                Gizmos.DrawRay(origin, direction.normalized * length);
            }
        }

        [Serializable]
        public class RayDrawerTsf : BaseDrawer
        {
            public Transform origin;
            public Vector3 localDirection = Vector3.forward;
            public float length = 5f;

            public override void Draw()
            {
                if (origin == null)
                    return;

                base.Draw();
                Vector3 worldDirection = origin.TransformDirection(localDirection);
                Gizmos.DrawRay(origin.position, worldDirection.normalized * length);
            }
        }

        #endregion

        #region Path & Polyline

        [Serializable]
        public class PathDrawerV3 : BaseDrawer
        {
            public Vector3[] points = Array.Empty<Vector3>();
            public bool closePath = false;

            public override void Draw()
            {
                if (points == null || points.Length < 2)
                    return;

                base.Draw();

                for (int i = 0; i < points.Length - 1; i++)
                {
                    Gizmos.DrawLine(points[i], points[i + 1]);
                }

                if (closePath && points.Length > 2)
                {
                    Gizmos.DrawLine(points[^1], points[0]);
                }
            }
        }

        [Serializable]
        public class PathDrawerTsf : BaseDrawer
        {
            public Transform[] points = Array.Empty<Transform>();
            public bool closePath = false;

            public override void Draw()
            {
                if (points == null || points.Length < 2)
                    return;

                base.Draw();

                for (int i = 0; i < points.Length - 1; i++)
                {
                    if (points[i] == null || points[i + 1] == null)
                        continue;
                    Gizmos.DrawLine(points[i].position, points[i + 1].position);
                }

                if (closePath && points.Length > 2 && points[0] != null && points[points.Length - 1] != null)
                {
                    Gizmos.DrawLine(points[^1].position, points[0].position);
                }
            }
        }

        #endregion

        #region Coordinate System

        [Serializable]
        public class CoordinateSystemDrawer : BaseDrawer
        {
            public Transform transform;
            public float axisLength = 1f;
            public bool useGizmosColor = false;

            public override void Draw()
            {
                if (transform == null)
                    return;

                var pos = transform.position;
                var rot = transform.rotation;

                if (useGizmosColor)
                {
                    base.Draw();
                    Gizmos.DrawRay(pos, rot * Vector3.right * axisLength);
                    Gizmos.DrawRay(pos, rot * Vector3.up * axisLength);
                    Gizmos.DrawRay(pos, rot * Vector3.forward * axisLength);
                }
                else
                {
                    // X轴 - 红色
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(pos, rot * Vector3.right * axisLength);

                    // Y轴 - 绿色
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(pos, rot * Vector3.up * axisLength);

                    // Z轴 - 蓝色
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(pos, rot * Vector3.forward * axisLength);
                }
            }
        }

        #endregion

        #region Frustum

        // [Serializable]
        // public class FrustumDrawer : BaseDrawer
        // {
        //     public Transform transform;
        //     public float fov = 60f;
        //     public float nearClip = 0.3f;
        //     public float farClip = 10f;
        //     public float aspect = 1.77f;
        //
        //     public override void Draw()
        //     {
        //         if (transform == null)
        //             return;
        //
        //         base.Draw();
        //
        //         Vector3 pos = transform.position;
        //         Quaternion rot = transform.rotation;
        //
        //         // 计算近平面和远平面的四个角
        //         float nearHeight = 2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * nearClip;
        //         float nearWidth = nearHeight * aspect;
        //         float farHeight = 2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * farClip;
        //         float farWidth = farHeight * aspect;
        //
        //         Vector3 nearCenter = pos + rot * Vector3.forward * nearClip;
        //         Vector3 farCenter = pos + rot * Vector3.forward * farClip;
        //
        //         Vector3[] nearCorners = new Vector3[4];
        //         Vector3[] farCorners = new Vector3[4];
        //
        //         // 近平面
        //         nearCorners[0] = nearCenter + rot * new Vector3(-nearWidth * 0.5f, nearHeight * 0.5f, 0);
        //         nearCorners[1] = nearCenter + rot * new Vector3(nearWidth * 0.5f, nearHeight * 0.5f, 0);
        //         nearCorners[2] = nearCenter + rot * new Vector3(nearWidth * 0.5f, -nearHeight * 0.5f, 0);
        //         nearCorners[3] = nearCenter + rot * new Vector3(-nearWidth * 0.5f, -nearHeight * 0.5f, 0);
        //
        //         // 远平面
        //         farCorners[0] = farCenter + rot * new Vector3(-farWidth * 0.5f, farHeight * 0.5f, 0);
        //         farCorners[1] = farCenter + rot * new Vector3(farWidth * 0.5f, farHeight * 0.5f, 0);
        //         farCorners[2] = farCenter + rot * new Vector3(farWidth * 0.5f, -farHeight * 0.5f, 0);
        //         farCorners[3] = farCenter + rot * new Vector3(-farWidth * 0.5f, -farHeight * 0.5f, 0);
        //
        //         // 绘制近平面
        //         for (int i = 0; i < 4; i++)
        //         {
        //             Gizmos.DrawLine(nearCorners[i], nearCorners[(i + 1) % 4]);
        //         }
        //
        //         // 绘制远平面
        //         for (int i = 0; i < 4; i++)
        //         {
        //             Gizmos.DrawLine(farCorners[i], farCorners[(i + 1) % 4]);
        //         }
        //
        //         // 绘制连接线
        //         for (int i = 0; i < 4; i++)
        //         {
        //             Gizmos.DrawLine(nearCorners[i], farCorners[i]);
        //         }
        //     }
        // }
        
        [Serializable]
        public class FrustumDrawer : BaseDrawer
        {
            public Transform transform;
            public float fov = 60f;
            public float nearClip = 0.3f;
            public float farClip = 10f;
            public float aspect = 1.77f;

            public override void Draw()
            {
                if (transform == null)
                    return;

                base.Draw();
                
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.DrawFrustum(Vector3.zero, fov, farClip, nearClip, aspect);
                Gizmos.matrix = prevMatrix;
            }
        }

        #endregion
    }
}