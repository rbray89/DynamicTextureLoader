using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DynamicTextureLoader
{
    class TextureUnloaderPartModule : PartModule
    {
        bool loaded = false;

        public override void OnAwake()
        {
            if (HighLogic.LoadedSceneIsGame)
            {
                Load();
            }
        }

        public void OnDestroy()
        {
            Unload();
        }

        private void Load()
        {
            if (!loaded)
            {
                Loader.Log("Loading: " + part.name);
                foreach (Renderer mr in part.FindModelComponents<Renderer>())
                {
                    TexRefCnt.LoadFromRenderer(mr);
                }
                loaded = true;
            }
        }

        public void Unload(bool force = false)
        {
            if (loaded || force)
            {
                Loader.Log("Unloading: " + part.name);
                foreach (Renderer mr in part.FindModelComponents<Renderer>())
                {
                    TexRefCnt.UnLoadFromRenderer(mr, force);
                }
                loaded = false;
            }
            
        }
    }
}
