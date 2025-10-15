using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SFS.Cameras;
using UnityEngine;

namespace OptiSFS
{
    public static class Utility
    {
        public static int CompareToCultureInvariant(this string a, string b)
        {
            return b == null ? 1 : CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.None);
        }
        
        public static (Vector2, float) GetCircle(this ConvexPolygon poly, bool squaredRadius = false)
        {
            Vector2 center = Vector2.zero; // actually a centroid, but idgaf, good enough
            float radiusSq = 0f;

            for (int i = 0; i < poly.points.Length; i++)
            {
                center += poly.points[i];
            }
            center /= poly.points.Length;

            for (int i = 0; i < poly.points.Length; i++)
            {
                Vector2 point = poly.points[i];
                radiusSq = Mathf.Max(radiusSq, (center - point).sqrMagnitude);
            }
            
            return (center, squaredRadius ? radiusSq : Mathf.Sqrt(radiusSq));
        }

        public static void Batched_DrawLines(List<Vector3> starts, List<Vector3> ends, float width, Color color,
            float sortingOrder)
        {
            GL.Begin(GL.QUADS);
            
            new Traverse(GLDrawer.main).Method("GetMaterial", sortingOrder).GetValue<Material>().SetPass(0);
            
            if (starts.Count != ends.Count) throw new ArgumentException("Start and end points do not match.");
            
            GL.Color(color);
            
            float w = width * 0.5f;
            
            Camera cam = ActiveCamera.main.activeCamera.Value.camera;
            Vector2 topLeft = cam.ViewportToWorldPoint(new Vector3(0, 1));
            Vector2 bottomRight = cam.ViewportToWorldPoint(new Vector3(1, 0));
            
            float xMin = Mathf.Min(topLeft.x, bottomRight.x) - w;
            float xMax = Mathf.Max(topLeft.x, bottomRight.x) + w;
            float yMin = Mathf.Min(bottomRight.y, topLeft.y) - w;
            float yMax = Mathf.Max(bottomRight.y, topLeft.y) + w;

            Rect worldRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            
            Vector2[] corners = new Vector2[4];
            
            for (int i = 0; i < starts.Count; i++)
            {
                var start  = starts[i];
                var end = ends[i];

                if (!LineIntersectsRect(start, end, worldRect, ref corners)) continue;
                
                Vector3 vector = Vector2.Perpendicular((start - end).normalized) * width * 0.5f;
                GL.Vertex(start - vector);
                GL.Vertex(end - vector);
                GL.Vertex(end + vector);
                GL.Vertex(start + vector);
            }

            GL.End();
        }
        
        static bool LineIntersectsRect(Vector3 p1, Vector3 p2, Rect r, ref Vector2[] corners)
        {
            Vector2 p1v = new Vector2(p1.x, p1.y);
            Vector2 p2v = new Vector2(p2.x, p2.y);
            if (r.Contains(p1v) || r.Contains(p2v)) return true;
            
            corners[0] = new Vector2(r.xMin, r.yMin);
            corners[1] = new Vector2(r.xMax, r.yMin);
            corners[2] = new Vector2(r.xMax, r.yMax);
            corners[3] = new Vector2(r.xMin, r.yMax);
                
            // Check intersection with each edge

            for (int i = 0; i < 4; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % 4];
                if (LinesIntersect(p1, p2, a, b)) return true;
            }

            return false;
        }

        static bool LinesIntersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
        {
            return ccw(A, C, D) != ccw(B, C, D) && ccw(A, B, C) != ccw(A, B, D);
                
            bool ccw(Vector2 a, Vector2 b, Vector2 c)
            {
                return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
            }
        }

        public static void Batched_DrawCircles(List<Vector3> positions, float radius, int resolution, Color color, float sortingOrder)
        {
            GL.Begin(GL.QUADS);
            new Traverse(GLDrawer.main).Method("GetMaterial", sortingOrder).GetValue<Material>().SetPass(0);
            GL.Color(color);
            Vector2[] array = new Vector2[resolution];
            float num = Mathf.PI * 2f / resolution;
            for (int i = 0; i < resolution; i++)
            {
                array[i] = new Vector2(radius * Mathf.Cos(num * i), radius * Mathf.Sin(num * i));
            }
            
            float w = radius;
            
            Camera cam = ActiveCamera.main.activeCamera.Value.camera;
            Vector2 topLeft = cam.ViewportToWorldPoint(new Vector3(0, 1));
            Vector2 bottomRight = cam.ViewportToWorldPoint(new Vector3(1, 0));
            
            float xMin = Mathf.Min(topLeft.x, bottomRight.x) - w;
            float xMax = Mathf.Max(topLeft.x, bottomRight.x) + w;
            float yMin = Mathf.Min(bottomRight.y, topLeft.y) - w;
            float yMax = Mathf.Max(bottomRight.y, topLeft.y) + w;

            Rect worldRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            
            foreach (Vector2 position in positions)
            {
                if (!worldRect.Contains(position)) continue;
                for (int j = 0; j < array.Length; j++)
                {
                    GL.Vertex(position);
                    GL.Vertex(position + array[(j + 1) % array.Length]);
                    GL.Vertex(position + array[j]);
                    GL.Vertex(position);
                }
            }
            GL.End();
        }
    }
}