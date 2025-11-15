using HarmonyLib;
using SFS.Builds;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using SFS.Cameras;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.World;
using UnityEngine.SceneManagement;

namespace OptiSFS
{
    [HarmonyPatch(typeof(Polygon), nameof(Polygon.Intersect), typeof(ConvexPolygon[]), typeof(ConvexPolygon[]),
        typeof(float))]
    public static class PolygonIntersectPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ConvexPolygon[] A, ConvexPolygon[] B, float overlapThreshold, ref bool __result)
        {
            if (!Entrypoint.PatchEnabled)
                return true;
            
            __result = FastPolygon.FastIntersect(A, B, overlapThreshold);
            
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildSelector), "I_GLDrawer.Draw")]
    public static class BuildSelectorDrawPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(BuildSelector __instance)
        {
            if (!Entrypoint.PatchEnabled)
                return true;

            List<Part> holdGrid = new List<Part>();
            List<Part> inactiveGrid = new List<Part>();
            List<Part> activeGrid = new List<Part>();
            
            if (!BuildManager.main.buildMenus.stagingMode.Value || !StagingDrawer.main.HasStageSelected())
            {
                foreach (Part item in __instance.selected)
                {
                    if (BuildManager.main.holdGrid.holdGrid.partsHolder.ContainsPart(item))
                        holdGrid.Add(item);
                    else if (BuildManager.main.buildGrid.inactiveGrid.partsHolder.ContainsPart(item))
                        inactiveGrid.Add(item);
                    else
                        activeGrid.Add(item);
                }

                float depth = __instance.renderSortingManager.GetGlobalDepth(1f, __instance.holdGridLayer);
                BuildSelector.DrawOutline(holdGrid, false, __instance.outlineColor, __instance.width, depth);
                
                depth = __instance.renderSortingManager.GetGlobalDepth(1f, __instance.inactiveBuildGridLayer);
                BuildSelector.DrawOutline(inactiveGrid, false, __instance.outlineColor, __instance.width, depth);
                
                depth = __instance.renderSortingManager.GetGlobalDepth(1f, __instance.activeBuildGridLayer);
                BuildSelector.DrawOutline(activeGrid, false, __instance.outlineColor, __instance.width, depth);
            }
            if (BuildManager.main.symmetryMode)
            {
                float globalDepth2 = __instance.renderSortingManager.GetGlobalDepth(1f, __instance.holdGridLayer);
                BuildSelector.DrawOutline(BuildManager.main.holdGrid.holdGrid.partsHolder.parts, symmetry: true, new Color(1f, 1f, 1f, 0.3f), __instance.width, globalDepth2);
            }
            
            return false;
        }
    }
    
    [HarmonyPatch(typeof(BuildSelector), "DrawOutline")]
    public static class OutlinePatch
    {
        static List<Vector3> starts = new List<Vector3>();
        static List<Vector3> ends = new List<Vector3>();
        static List<Vector3> circleCenters = new List<Vector3>();
        
        
        [HarmonyPrefix]
        public static bool Prefix(List<Part> parts, bool symmetry, Color color, float width, float depth = 1f)
        {
            if (!Entrypoint.PatchEnabled) 
                return true;
            
            var cam = ActiveCamera.main.activeCamera.Value.camera;
            float pixelSize = cam.ScreenToWorldPoint(Vector3.right).x - cam.ScreenToWorldPoint(Vector3.zero).x;
            
            
            //Stopwatch sw = Stopwatch.StartNew();
            
            starts = new List<Vector3>();
            ends = new List<Vector3>();
            circleCenters = new List<Vector3>();
            
            foreach (Part part in parts) // This probably can be optimized further by marking blueprint/parts as dirty, but that approach is good enough for now
            {
                foreach (PolygonData poly in part.GetModules<PolygonData>())
                {
                    if (!poly.Click) continue;

                    var verts = poly.polygon.GetVerticesWorld(poly.transform);
                    
                    float num = BuildManager.main.buildGrid.gridSize.centerX * 2f;
                    
                    for (int i = 0; i < verts.Length; i++)
                    {
                        var st = verts[i];
                        var en = verts[(i + 1) % verts.Length];
                        
                        if (symmetry)
                        {
                            starts.Add(new Vector3(-st.x + num, st.y));
                            ends.Add(new Vector3(-en.x + num, en.y));
                            circleCenters.Add(new Vector2(-st.x + num, st.y));
                            continue;
                        }
                        
                        starts.Add(st);
                        ends.Add(en);
                        circleCenters.Add(verts[i]);
                    }
                }
            }

            //sw.Stop();
            //MsgDrawer.main.Log(sw.Elapsed.TotalMilliseconds.ToString());
            
            //MsgDrawer.main.Log(pixelSize.ToString());
            
            if (pixelSize < width / 2) 
                FastGL.Batched_DrawCircles(circleCenters, width * 0.5f, pixelSize >= width / 4 ? 8 : 12, color, depth);
            FastGL.Batched_DrawLines(starts, ends, width, color, depth);

            return false;
        }
    }
    
    // NOTE: THIS LIKELY WILL CONFLICT WITH INFO OVERLOAD
    [HarmonyPatch(typeof(PolygonCollider), "BuildCollider")]
    public static class PolyBuildPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PolygonCollider __instance)
        {
            if (!Entrypoint.PatchEnabled) return true;
            
            if (SceneManager.GetActiveScene().name == "Build_PC") return false;
            
            Traverse tr = Traverse.Create(__instance);

            var poly = tr.Field<PolygonCollider2D>("collider_Polygon");
            var box = tr.Field<BoxCollider2D>("collider_Box");
            
            Vector2[] vertices = __instance.polygon.polygonFast.vertices;
            
            if (vertices.Length == 4 && vertices[0].x == vertices[1].x && vertices[1].y == vertices[2].y && vertices[2].x == vertices[3].x && vertices[3].y == vertices[0].y)
            {
                if (poly.Value != null)
                {
                    Object.Destroy(poly.Value);
                }
                if (box.Value == null)
                {
                    box.Value = __instance.gameObject.AddComponent<BoxCollider2D>();
                }
                box.Value.size = new Vector2(Mathf.Abs(vertices[2].x - vertices[0].x), Mathf.Abs(vertices[2].y - vertices[0].y));
                box.Value.offset = (vertices[0] + vertices[2]) * 0.5f;
            }
            else
            {
                if (box.Value != null)
                {
                    Object.Destroy(box.Value);
                }
                if (poly.Value == null)
                {
                    poly.Value = __instance.gameObject.AddComponent<PolygonCollider2D>();
                }
                poly.Value.points = vertices;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Part), nameof(Part.InitializePart))]
    public static class PartInitPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Part __instance)
        {
            if (!Entrypoint.PatchEnabled) return;
            if (SceneManager.GetActiveScene().name != "Build_PC") return;

            foreach (Collider2D col in __instance.GetComponentsInChildren<Collider2D>())
            {
                col.enabled = false;
            }
        }
    }
    
}