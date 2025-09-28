using System.Collections.Generic;
using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using UITools;
using UnityEngine;

namespace OptiSFS
{
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
            {"", new FolderPath(ModFolder).ExtendToFile("OptiSFS.dll")}
        };
        
        public static bool ENABLED = true;

        public static float AERO_DT = 0f;
        
        public override void Early_Load()
        {
            if (ENABLED)
                new Harmony(ModNameID).PatchAll();
            
            if (!SurfaceEndXRadixSort.Test()) Debug.Log("SURFACE SORT TEST FAILED");

            new GameObject().AddComponent<HUD>();
        }

        public override void Load()
        {
        }
    }
}