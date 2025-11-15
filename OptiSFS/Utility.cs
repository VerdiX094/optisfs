using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SFS.Cameras;
using UnityEngine;

namespace OptiSFS
{
    public struct Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
    public static class Utility
    {
        public static int CompareToCultureInvariant(this string a, string b)
        {
            return b == null ? 1 : CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.None);
        }
        
        public static Circle GetCircle(this ConvexPolygon poly, bool squaredRadius = false)
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
                radiusSq = Mathf.Max(radiusSq, (center - poly.points[i]).sqrMagnitude);
            }
            
            return new Circle(center, squaredRadius ? radiusSq : Mathf.Sqrt(radiusSq));
        }

        public static Rect GetCameraBounds(float z = 0f)
        {
            Camera cam = ActiveCamera.Camera.camera;

            if (cam == null)
            {
                Debug.LogError("ActiveCamera.Camera.camera is null!");
                return Rect.zero;
            }

            float height = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * Mathf.Abs(z - cam.transform.position.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

            float width = height * cam.aspect;

            Vector3 center = new Vector3(cam.transform.position.x, cam.transform.position.y, z);
            
            Vector3 half = new Vector3(width / 2f, height / 2f, 0f);
            Vector3 min = center - half;
            Vector3 max = center + half;

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}