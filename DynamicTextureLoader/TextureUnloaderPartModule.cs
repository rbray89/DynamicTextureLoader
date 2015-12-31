using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DynamicTextureLoader
{
    class TextureUnloaderPartModule : PartModule
    {
        Dictionary<string, List<TexRefCnt>> internalCache = new Dictionary<string, List<TexRefCnt>>();
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

        private Part fetchInternalPart()
        {
            UnityEngine.Object obj = UnityEngine.Object.Instantiate(part.partInfo.partPrefab);
            Part newPart = (Part)obj;

            newPart.gameObject.SetActive(true);
            newPart.gameObject.name = part.partInfo.partPrefab.name;
            newPart.partInfo = part.partInfo;

            if (newPart.partInfo.internalConfig.HasData)
            {
                newPart.CreateInternalModel();
                newPart.internalModel.SetVisible(false);
                newPart.internalModel.enabled = false;
            }
            return newPart;
        }

        private void Load()
        {
            if (!loaded)
            {
                string partUrl = this.part.partInfo.partUrl;

                Loader.Log("Loading: " + partUrl);
                
                foreach (Renderer mr in part.FindModelComponents<Renderer>())
                {
                    Loader.Log("Renderer: " + mr.name);
                    TexRefCnt.LoadFromRenderer(mr);
                }
                if (!internalCache.ContainsKey(partUrl))
                {
                    bool hasIVA = part.internalModel != null;
                    
                    if (part.internalModel != null)
                    {
                        List<TexRefCnt> list = new List<TexRefCnt>();
                        foreach (Renderer mr in part.internalModel.FindModelComponents<Renderer>())
                        {
                            TexRefCnt.LoadFromRenderer(mr, list);
                        }
                        internalCache[partUrl] = list;
                        if (!hasIVA)
                        {
                            GameDatabase.DestroyImmediate(part.internalModel);
                            part.internalModel = null;
                        }
                    }
                }
                else
                {
                    List<TexRefCnt> list = internalCache[partUrl];
                    TexRefCnt.LoadFromList(list);
                }
                loaded = true;
            }
        }

        public void Unload(bool force = false)
        {
            if (loaded || force)
            {

                string partUrl = this.part.partInfo.partUrl;
                Loader.Log("Unloading: " + partUrl);
                foreach (Renderer mr in part.FindModelComponents<Renderer>())
                {
                    Loader.Log("Renderer: " + mr.name);
                    TexRefCnt.UnLoadFromRenderer(mr, force);
                }

                if (!internalCache.ContainsKey(partUrl))
                {
                    bool hasIVA = part.internalModel != null;
                    if (part.partInfo.internalConfig.HasData && !hasIVA)
                    {
                        part.CreateInternalModel();
                        part.internalModel.SetVisible(false);
                        part.internalModel.enabled = false;
                    }
                    if (part.internalModel != null)
                    {
                        List<TexRefCnt> list = new List<TexRefCnt>();
                        foreach (Renderer mr in part.internalModel.FindModelComponents<Renderer>())
                        {
                            TexRefCnt.UnLoadFromRenderer(mr, force, list);
                        }
                        internalCache[partUrl] = list;
                        part.internalModel = null;
                        part.InternalModelName = "";
                    }
                }
                else
                {
                    List<TexRefCnt> list = internalCache[partUrl];
                    TexRefCnt.UnLoadFromList(list, force);
                }
                loaded = false;
            }
            
        }
    }
}
