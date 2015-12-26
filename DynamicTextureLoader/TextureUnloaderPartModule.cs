using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DynamicTextureLoader
{
    class TextureUnloaderPartModule : PartModule
    {
        bool unloaded = false;

        public override void OnStart(StartState state)
        {
            Load();
        }

        public override void OnInactive()
        {
            Unload();
        }

        public void OnDestroy()
        {
            Unload();
        }

        private void Load()
        {
            Loader.Log("Loading: " + part.name);
            foreach (MeshRenderer mr in part.FindModelComponents<MeshRenderer>())
            {
                TexRefCnt.LoadFromRenderer(mr);
            }
        }

        public void Unload(bool force = false)
        {

            Loader.Log("Unloading: " + part.name);
            if (!unloaded || force)
            {
                foreach (MeshRenderer mr in part.FindModelComponents<MeshRenderer>())
                {
                    TexRefCnt.UnLoadFromRenderer(mr);
                }
                unloaded = true;
            }
            
        }
    }
}
