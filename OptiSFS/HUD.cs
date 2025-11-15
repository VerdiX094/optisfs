using SFS.UI.ModGUI;
using UITools;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using TMPro;

namespace OptiSFS
{
    public class HUD : MonoBehaviour
    {
        private Window win;
        private Label label;

        private int frames = 0;
        private float lastUpdate = 0;
        private float fps = 0;

        public static Dictionary<string, double> times = new Dictionary<string, double>();
        
        void Start()
        {
            if (!Entrypoint.DevelopmentMode) return;
            
            Transform holder = Builder.CreateHolder(Builder.SceneToAttach.BaseScene, "FPS HUD").transform;
            
            win = Builder.CreateWindow(holder, Builder.GetRandomID(), 360, 480, draggable: true, titleText: "FPS HUD");
            win.CreateLayoutGroup(Type.Vertical);
            win.RegisterPermanentSaving("moe.verdix.optisim_HUD");
            
            lastUpdate = Time.realtimeSinceStartup;
            
            label = Builder.CreateLabel(win, 328, 14400, text: "");
            label.AutoFontResize = false;
            label.FontSize *= 0.8f;
            label.TextAlignment = TextAlignmentOptions.TopJustified;
        }

        void Update()
        {
            if (!Entrypoint.DevelopmentMode) return;
            
            frames++;
            if (Input.GetKeyDown(KeyCode.Backslash))
                Entrypoint.PatchEnabled ^= true;

            if (Time.realtimeSinceStartup - lastUpdate > 0.5)
            {
                lastUpdate = Time.realtimeSinceStartup;
                fps = frames * 2;
                frames = 0;
            }
            
            StringBuilder sb = new StringBuilder();

            foreach (var key in times.Keys)
            {
                sb.AppendLine($"{key}: {times[key].Round(3)}ms");
            }
            
            label.Text = $"FPS: {fps}\nPatch {(Entrypoint.PatchEnabled ? "ON" : "OFF")}\n{sb}";
        }
    }
}