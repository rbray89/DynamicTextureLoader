using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DynamicTextureLoader
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class Loader : MonoBehaviour
    {

        public static int _MainTex_PROPERTY { get { return _MainTex; } }
        private static int _MainTex;
        public static int _BumpMap_PROPERTY { get { return _BumpMap; } }
        private static int _BumpMap;
        public static int _Emissive_PROPERTY { get { return _Emissive; } }
        private static int _Emissive;

        static bool loaded = false;
        static internal void Log(string s)
        {
            KSPLog.print( "DynamicTextureLoader: "+s);
        }
        private void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !loaded)
            {
                loaded = true;
                _MainTex = Shader.PropertyToID("_MainTex");
                _BumpMap = Shader.PropertyToID("_BumpMap");
                _Emissive = Shader.PropertyToID("_Emissive");

                ConfigNode moduleNode = new ConfigNode("MODULE");
                moduleNode.SetValue("name", typeof(TextureUnloaderPartModule).Name, true);

                foreach (AvailablePart ap in PartLoader.LoadedPartsList)
                {
                    if (ap.partUrl != null && ap.partUrl != "")
                    {
                        Part part = ap.partPrefab;
                        TextureUnloaderPartModule module = (TextureUnloaderPartModule)part.AddModule(typeof(TextureUnloaderPartModule).Name);
                        MethodInfo mI = typeof(PartModule).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                        mI.Invoke(module, null);
                        module.Load(moduleNode);
                        module.Unload(true);
                    }
                }
            }

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                foreach (AvailablePart ap in PartLoader.LoadedPartsList)
                {
                    if (ap.partUrl != null && ap.partUrl != "")
                    {
                        Part part = ap.partPrefab;
                        TextureUnloaderPartModule module = (TextureUnloaderPartModule)part.FindModuleImplementing<TextureUnloaderPartModule>();
                        module.Unload(true);
                    }
                }
            }
        }
    }
}
