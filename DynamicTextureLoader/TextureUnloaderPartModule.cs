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
        StartState startState;
        public override void OnStart(StartState state)
        {
            startState = state;
            Load();
        }

        public override void OnActive()
        {
            Load();
        }
        
        public override void OnInactive()
        {
            Unload();
        }
        
        public void OnDestroy()
        {
            if (startState == StartState.Editor)
            {
                Unload();
            }
        }

        private void Load()
        {
            if (!loaded)
            {
                Loader.Log("Loading: " + part.name);
                foreach (MeshRenderer mr in part.FindModelComponents<MeshRenderer>())
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
                foreach (MeshRenderer mr in part.FindModelComponents<MeshRenderer>())
                {
                    TexRefCnt.UnLoadFromRenderer(mr);
                }
                loaded = false;
            }
            
        }
    }
}
