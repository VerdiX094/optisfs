using SFS.UI.ModGUI;
using UITools;
using UnityEngine;

namespace OptiSFS
{
    public class HUD : MonoBehaviour
    {
        private Window win;
        private Label label;

        private int frames = 0;
        private float lastUpdate = 0;
        private float fps = 0;

        private float aeroDT = 0f;
        
        void Start()
        {
            if (!Entrypoint.DEV_HUD) return;
            
            Transform holder = Builder.CreateHolder(Builder.SceneToAttach.BaseScene, "FPS HUD").transform;
            
            win = Builder.CreateWindow(holder, Builder.GetRandomID(), 240, 208, draggable: true, titleText: "FPS HUD");
            win.CreateLayoutGroup(Type.Vertical);
            win.RegisterPermanentSaving("moe.verdix.optisim_HUD");
            
            lastUpdate = Time.realtimeSinceStartup;
            
            label = Builder.CreateLabel(win, 224, 144, text: "");
            label.FontSize *= 0.8f;
        }

        void Update()
        {
            if (!Entrypoint.DEV_HUD) return;
            
            frames++;
            if (Input.GetKeyDown(KeyCode.Backslash))
                Entrypoint.ENABLED ^= true;

            if (Time.realtimeSinceStartup - lastUpdate > 0.5)
            {
                lastUpdate = Time.realtimeSinceStartup;
                fps = frames * 2;
                frames = 0;
            }
            
            aeroDT = Entrypoint.AERO_DT;
            label.Text = $"FPS: {fps}\nPatch {(Entrypoint.ENABLED ? "ON" : "OFF")}\nAdt={aeroDT:0.00}ms";
        }
    }
}