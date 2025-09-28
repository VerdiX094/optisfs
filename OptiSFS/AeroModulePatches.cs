﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using SFS.World.Drag;
using UnityEngine;

namespace OptiSFS
{
    [HarmonyPatch(typeof(Aero_Rocket), "GetDragSurfaces", typeof(Matrix2x2))]
    public static class AeroBenchmark
    {
        [HarmonyPrefix]
        public static bool Prefix(AeroModule __instance, ref Stopwatch __state)
        {
            if (__instance is Aero_Rocket ar || true)
            {
                __state = Stopwatch.StartNew();
            }
            
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(AeroModule __instance, ref Stopwatch __state)
        {
            if ((__instance is Aero_Rocket ar && __state != null) || true)
            {
                __state.Stop();
                Entrypoint.AERO_DT = (float)__state.Elapsed.TotalMilliseconds;
            }
        }
    }
    
    [HarmonyPatch(typeof(AeroModule), "SortDragSurfacesByEndX")]
    public static class SortingPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(List<Surface> surfaces)
        {
            if (!Entrypoint.ENABLED)
                return true;
            
            SurfaceEndXRadixSort.Sort(ref surfaces);
            
            //surfaces.Sort((a, b) => a.line.end.x.CompareTo(b.line.end.x));
            return false;
        }
    }
    
    [HarmonyPatch(typeof(AeroModule), "ApplyProtectionZone")]
    public static class ProtectionZonePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(List<Surface> surfaces)
        {
            if (!Entrypoint.ENABLED)
                return true;
            
            var removeIndexes = new HashSet<int>();

            for (int i = 1; i < surfaces.Count - 1; i++)
            {
                Line2 line = surfaces[i].line;
                float num = line.start.y - surfaces[i - 1].line.end.y;

                if (num > 0.1f)
                {
                    float num2 = line.start.x - Mathf.Min(num * 0.2f, 0.4f);
                    for (int num3 = i - 1; num3 >= 0; num3--)
                    {
                        if (surfaces[num3].line.start.x > num2)
                        {
                            removeIndexes.Add(num3);
                            i--; // adjust loop to stay aligned
                            continue;
                        }

                        Surface value = surfaces[num3];
                        value.line.end = value.line.GetPositionAtX(num2);
                        surfaces[num3] = value;
                        break;
                    }
                }

                if (line.end.y - surfaces[i + 1].line.start.y > 0.1f)
                {
                    float num4 = line.end.x + Mathf.Min(num * 0.2f, 0.4f);
                    for (int num5 = i + 1; num5 < surfaces.Count; num5++)
                    {
                        if (surfaces[num5].line.end.x < num4)
                        {
                            removeIndexes.Add(num5);
                            continue;
                        }

                        Surface value2 = surfaces[num5];
                        value2.line.start = value2.line.GetPositionAtX(num4);
                        surfaces[num5] = value2;
                        break;
                    }
                }
            }

            // Second pass: also mark very small surfaces
            for (int num6 = 0; num6 < surfaces.Count; num6++)
            {
                if (surfaces[num6].line.SizeX < 0.1f)
                    removeIndexes.Add(num6);
            }

            // Remove all at once by compacting list
            if (removeIndexes.Count > 0)
            {
                int write = 0;
                for (int read = 0; read < surfaces.Count; read++)
                {
                    if (!removeIndexes.Contains(read))
                        surfaces[write++] = surfaces[read];
                }
                surfaces.RemoveRange(write, surfaces.Count - write);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(AeroModule), "RemoveHighSlopeSurfaces")]
    public static class SlopeSurfacesPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(List<Surface> surfaces, float maxSlope, ref List<Surface> __result)
        {
            if (!Entrypoint.ENABLED)
                return true;
            
            __result = new List<Surface>();

            for (int index = 0; index < surfaces.Count; index++)
            {
                var surface = surfaces[index];
                if (Mathf.Abs(surface.line.SizeY / surface.line.SizeX) < maxSlope && surface.line.SizeX > 0.1f)
                    __result.Add(surface);
            }
            
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Aero_Rocket), "GetDragSurfaces", typeof(PartHolder), typeof(Matrix2x2))]
    public static class DragSurfacesPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PartHolder partsHolder, Matrix2x2 rotate, ref List<Surface> __result)
        {
            if (!Entrypoint.ENABLED)
                return true;
            
            List<Surface> output = new List<Surface>();
            SurfaceData[] modules = partsHolder.GetModules<SurfaceData>();

            /*int size = 0;
            
            for (int a = 0; a < modules.Length; a++)
            {
                var module = modules[a];
                for (int b = 0; b < module.surfacesFast.Count; b++)
                {
                    size += module.surfacesFast[b].points.Length;
                }
            }*/
            
            output.Capacity = modules.Length * 5; // Generally every module has one set of surfaces and on average 5 surfaces in it
            
            for (int sdi = 0; sdi < modules.Length; sdi++)
            {   
                var surfaceData = modules[sdi];
                
                if (!surfaceData.Drag)
                    continue;
                
                Transform t = surfaceData.transform;
                Vector3 lossyScale = t.lossyScale;
                bool flip = lossyScale.x > 0f != lossyScale.y > 0f;
                HeatModuleBase heatModule = surfaceData.heatModule;
                
                var matrix = t.localToWorldMatrix;
                
                for (int ind = 0; ind < surfaceData.surfacesFast.Count; ind++)
                {
                    Surfaces item = surfaceData.surfacesFast[ind];
                    
                    int num = item.points.Length;

                    Vector2[] array = new Vector2[num];

                    for (int i = 0; i < num; i++)
                    {
                        array[i] = matrix.MultiplyPoint3x4(item.points[i]) * rotate;
                    }
                    
                    for (int num2 = 0; num2 < num - 1; num2++)
                        AddLine(array[num2], array[num2 + 1]);
                    
                    if (item.loop)
                        AddLine(array[num-1], array[0]);
                }
                void AddLine(Vector2 a, Vector2 b)
                {
                    if (flip)
                        (a, b) = (b, a);
                    
                    if (a.x < b.x) // a.x != b.x *should* be unnecessary here
                        output.Add(new Surface(heatModule, heatModule.valid, new Line2(a, b)));
                }
            }
            __result = output;
            return false;
        }
    }
}