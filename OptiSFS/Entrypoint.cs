using System.Collections.Generic;
using HarmonyLib;
using ModLoader;
using SFS.IO;
using UITools;
using UnityEngine;

namespace OptiSFS
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Entrypoint : Mod, IUpdatable
    {
        public override string ModNameID => "moe.verdix.optisfs";

        public override string DisplayName => "OptiSFS";

        public override string Author => "VerdiX094";

        public override string MinimumGameVersionNecessary => "1.5.10.2";

        public override string ModVersion => "alpha-1";

        public override string Description => "Various optimizations for Spaceflight Simulator.";

        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>()
        {
            {"https://github.com/VerdiX094/optisfs/releases/latest/download/OptiSFS.dll", new FolderPath(ModFolder).ExtendToFile("OptiSFS.dll")}
        };
        
        public static bool PatchEnabled = true;

        public const bool DevelopmentMode = true;

        public static bool VersionHasRadixSort;
        
        public override void Early_Load()
        {
            VersionHasRadixSort = !Application.version.Contains("1.5.10");
            
            if (PatchEnabled)
                new Harmony(ModNameID).PatchAll();
            
            if (!SurfaceEndXRadixSort.Test()) Debug.Log("SURFACE SORT TEST FAILED");

            new GameObject().AddComponent<HUD>();
            
            if (DevelopmentMode)
                Benchmark.ApplyPatches();
        }
    }
}