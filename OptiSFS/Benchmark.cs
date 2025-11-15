using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using SFS.World;
using HarmonyLib;
using SFS.Parts;
using SFS.World.Drag;

namespace OptiSFS
{
    public static class Benchmark
    {
        private static readonly Dictionary<MethodInfo, string> methods = new Dictionary<MethodInfo, string>()
        {
            // SFS.World.ElementDrawer.RegisterElement(int priority, Vector2, Vector2, TextMesh, bool)
            { typeof(ElementDrawer).GetMethod("RegisterElement"), "RegElem" },
            
            // SFS.World.Drag.AeroModule::SortDragSurfacesByEndX()
            { typeof(AeroModule).GetMethod("SortDragSurfacesByEndX"), "SortDrag" },
            
            // SFS.World.Drag.Aero_Rocket::GetDragSurfaces(PartHolder, Matrix2x2)
            { typeof(Aero_Rocket).GetMethod("GetDragSurfaces", types: new[] { typeof(PartHolder), typeof(Matrix2x2) }), "DragSurfs" },
        };
        
        public static void ApplyPatches()
        {
            Harmony benchmarkHarmony = new Harmony("moe.verdix.optisfs.bench");
            
            foreach (var key in methods.Keys)
            {
                benchmarkHarmony.Patch(key, new HarmonyMethod(typeof(Benchmark).GetMethod("Prefix")), new HarmonyMethod(typeof(Benchmark).GetMethod("Postfix")));
            }
        }
        
        [HarmonyPriority(Priority.First)]
        public static void Prefix(ref Stopwatch __state)
        {
            __state = Stopwatch.StartNew();
        }
        
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref Stopwatch __state, MethodBase __originalMethod)
        {
            __state.Stop();
            
            if (methods.TryGetValue(__originalMethod as MethodInfo, out string label))
            {
                HUD.times[label] = __state.Elapsed.TotalMilliseconds;
            }
        }
    }
}