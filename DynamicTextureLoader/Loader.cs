using System;
using System.Collections.Generic;
using System.IO;
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
        static bool unloaded = false;
        static internal void Log(string s)
        {
            KSPLog.print( "DynamicTextureLoader: "+s);
        }
        static int partLoadedIndex = 0;
        static int textureLoadedIndex = 0;
        ConfigNode moduleNode;
        private void Update()
        {

            List<GameDatabase.TextureInfo> textures = GameDatabase.Instance.databaseTexture;
            int i = textureLoadedIndex;
            for (; textures != null && i < textures.Count; i++)
            {
                GameDatabase.TextureInfo tex = textures[i];
                if (tex != null && tex.file != null && tex.file.url != null && tex.file.url != "")
                {
                    string scaled = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/scaledTexCache/" + tex.file.url;
                    if (File.Exists(scaled))
                    {
                        TextureConverter.Minimize(tex);
                    }
                }
            }
            textureLoadedIndex = i;

            List<AvailablePart> parts = PartLoader.LoadedPartsList;
            i = partLoadedIndex;
            for ( ; parts != null && i < parts.Count; i++)
            {
                AvailablePart ap = parts[i];
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
            partLoadedIndex = i;
        }

        private void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING && !loaded)
            {
                loaded = true;
                _MainTex = Shader.PropertyToID("_MainTex");
                _BumpMap = Shader.PropertyToID("_BumpMap");
                _Emissive = Shader.PropertyToID("_Emissive");

                moduleNode = new ConfigNode("MODULE");
                moduleNode.SetValue("name", typeof(TextureUnloaderPartModule).Name, true);
            }

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER && !unloaded)
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
                unloaded = true;
            }
        }
    }
}
