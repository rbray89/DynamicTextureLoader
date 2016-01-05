using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DynamicTextureLoader
{
    class TextureUnloaderPartModule : PartModule
    {
        static Dictionary<string, List<TexRefCnt>> texCache = new Dictionary<string, List<TexRefCnt>>();
        bool loaded = false;

        public override void OnAwake()
        {
            if (HighLogic.LoadedSceneIsEditor||HighLogic.LoadedSceneIsFlight)
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

                if (!texCache.ContainsKey(partUrl))
                {

                    List<TexRefCnt> list = new List<TexRefCnt>();
                    foreach (Renderer mr in part.FindModelComponents<Renderer>())
                    {
                        //Loader.Log("Renderer: " + mr.name);
                        TexRefCnt.LoadFromRenderer(mr, list);
                    }

                
                    if (part.partInfo.internalConfig.HasData && HighLogic.LoadedSceneIsGame)
                    {
                        Loader.Log("Creating internal cache...");
                        Part iPart = fetchInternalPart();
                        InternalModel internalModel = iPart.internalModel;
                        foreach (Renderer mr in internalModel.FindModelComponents<Renderer>())
                        {
                            //Loader.Log("ImRenderer: " + mr.name);
                            TexRefCnt.LoadFromRenderer(mr, list);
                        }
                        GameObject.DestroyImmediate(iPart);
                    }
                    else
                    {
                        Loader.Log(part.partInfo.internalConfig.HasData+" " +HighLogic.LoadedSceneIsGame);
                    }

                    texCache[partUrl] = list;
                }
                else
                {
                    Loader.Log("Loading from cache...");
                    List<TexRefCnt> list = texCache[partUrl];
                    TexRefCnt.LoadFromList(list);
                }
                loaded = true;
            }
        }

        public void Unload(bool force = false, bool cache = true)
        {
            if (loaded || force)
            {
                string partUrl = this.part.partInfo.partUrl;
                Loader.Log("Unloading: " + partUrl);

                if (!texCache.ContainsKey(partUrl))
                {
                    List<TexRefCnt> list = new List<TexRefCnt>();
                    foreach (Renderer mr in part.FindModelComponents<Renderer>())
                    {
                        //Loader.Log("Renderer: " + mr.name);
                        TexRefCnt.UnLoadFromRenderer(mr, force, list);
                    }

                    if (part.partInfo.internalConfig.HasData && HighLogic.LoadedSceneIsGame)
                    {
                        Part iPart = fetchInternalPart();
                        InternalModel internalModel = iPart.internalModel;
                        foreach (Renderer mr in internalModel.FindModelComponents<Renderer>())
                        {
                            //Loader.Log("ImRenderer: " + mr.name);
                            TexRefCnt.UnLoadFromRenderer(mr, force, list);
                        }
                        GameObject.DestroyImmediate(iPart);
                    }
                    if (cache)
                    {
                        texCache[partUrl] = list;
                    }
                }
                else
                {
                    Loader.Log("Unloading from cache...");
                    List<TexRefCnt> list = texCache[partUrl];
                    TexRefCnt.UnLoadFromList(list, force);
                }
                loaded = false;
            }
            
        }
    }
}
