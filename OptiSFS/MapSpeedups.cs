using System;
using HarmonyLib;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace OptiSFS
{
    [HarmonyPatch(typeof(ElementDrawer), nameof(ElementDrawer.RegisterElement))]
    public static class ClearingBelowTooExpensive
    {
        public static bool Prefix(ElementDrawer __instance, int priority, Vector2 size, Vector2 position, TextMesh textMesh, bool clearBelow)
        {
            if (!Entrypoint.ENABLED)
                return true;
            
            ElementDrawer.Element element = new ElementDrawer.Element
            {
                priority = priority,
                size = size,
                position = position,
                textMesh = textMesh
            };
            if (!clearBelow)
                return false;
	
            float epsilon = Map.view.ToConstantSize(0.01f);
            
            // In theory these two won't do much anyway, but ig caching that still saves a couple hundred thousand CPU cycles
            float cos = Mathf.Cos(-GameCamerasManager.main.map_Camera.CameraRotationRadians);
            float sin = Mathf.Sin(-GameCamerasManager.main.map_Camera.CameraRotationRadians);

            bool notNull = element.textMesh != null; // pre-cache the registered element's TM null state, null comparisons are quite expensive with Unity objects
            
            int threw = 0;
            
            for (int i = 0; i < __instance.elements.Count; i++)
            {
                ElementDrawer.Element other = __instance.elements[i];
                
                Vector2 distance = element.position - other.position;
                distance = new Vector2(distance.x * cos - distance.y * sin, distance.x * sin + distance.y * cos);
                
                float boundsDistanceX = Mathf.Abs(distance.x) - element.size.x - other.size.x;
                if (boundsDistanceX > epsilon)
                    continue;
		
                float boundsDistanceY = Mathf.Abs(distance.y) - element.size.y - other.size.y;
		
                if (boundsDistanceY > epsilon)
                    continue;
		
                float alpha = Mathf.Max(boundsDistanceX, boundsDistanceY) / epsilon;
                
                if (element.priority > other.priority)
                {
                    try
                    {
                        if (alpha > 0f)
                        {
                            other.textMesh.color = new Color(1f, 1f, 1f, alpha);
                        }
                        else if (other.textMesh.gameObject.activeSelf)
                        {
                            other.textMesh.gameObject.SetActive(false);
                        }
                    }
                    catch (NullReferenceException) // in theory, it's more expensive than obj != null when something actually throws, but nothing did in testing
                    {
                        threw++;
                    }
                }
                else if (element.priority < other.priority && notNull)
                {
                    if (alpha > 0f)
                    {
                        element.textMesh.color = new Color(1f, 1f, 1f, alpha);
                    }
                    else if (element.textMesh.gameObject.activeSelf)
                    {
                        element.textMesh.gameObject.SetActive(false);
                    }
                }
            }
            __instance.elements.Add(element); // Why is this after the clearBelow check?

            if (threw != 0)
            {
                Debug.LogWarning(threw + " text meshes threw");
            }
            
            return false;
        }
    }
    
    /*// This optimization is way less important, but it's still an optimization ig
    [HarmonyPatch(typeof(MapManager), "LateUpdate")]
    public static class MapManager_ResetCache
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!Entrypoint.ENABLED) return;

            var planets = new Traverse(Map.environment).Field<MapPlanetEnvironment[]>("environments").Value;

            for (int i = 0; i < planets.Length; i++)
            {
                var env = planets[i];
                bool shouldEnable = env.planet.OrbitRadius / 5f >
                                    Map.view.view.distance.Value / Mathf.Max(Screen.width, Screen.height);
                if (shouldEnable != env.terrain.gameObject.activeSelf) // Only if larger than 5 pixels
                {
                    env.terrain.gameObject.SetActive(shouldEnable);
                }
            }
        }
    }*/
}