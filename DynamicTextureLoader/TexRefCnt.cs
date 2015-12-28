using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DynamicTextureLoader
{
    class TexRefCnt
    {
        public static Dictionary<string, TexRefCnt> textureDictionary = new Dictionary<string, TexRefCnt>();
        public static Queue<TexRefCnt> unloadQueue = new Queue<TexRefCnt>();

        public static void LoadFromRenderer(Renderer renderer)
        {
            foreach (Material material in renderer.materials)
            {
                LoadFromMaterial(material);
            }
        }

        public static void LoadFromMaterial(Material material)
        {
            LoadFromTexture(material, Loader._MainTex_PROPERTY);
            LoadFromTexture(material, Loader._Emissive_PROPERTY);
            LoadFromTexture(material, Loader._BumpMap_PROPERTY);
        }

        public static void LoadFromTexture(Material material, int id)
        {
            Texture2D texture = (Texture2D)material.GetTexture(id);
            if (texture != null)
            {
                TexRefCnt texRef;
                if (textureDictionary.ContainsKey(texture.name))
                {
                    texRef = textureDictionary[texture.name];
                }
                else
                {
                    texRef = new TexRefCnt(texture);
                    textureDictionary[texture.name] = texRef;
                }
                texRef.count++;
                if (texRef.count > 0 && texRef.texInfo != null && texRef.unloaded && HighLogic.LoadedSceneIsGame)
                {
                    UnLoadFromQueue();
                    TextureConverter.Reload(texRef.texInfo, true);
                    texRef.unloaded = false;
                }
            }
        }

        private static void UnLoadFromQueue()
        {
            while(unloadQueue.Count > 0)
            {
                TexRefCnt texRef = unloadQueue.Dequeue();
                if(!texRef.unloaded && texRef.count <= 0)
                {
                    TextureConverter.Minimize(texRef.texInfo);
                    texRef.unloaded = true;
                }
            }
        }

        public static void UnLoadFromRenderer(Renderer renderer, bool force)
        {
            foreach (Material material in renderer.materials)
            {
                UnLoadFromMaterial(material, force);
            }
        }

        public static void UnLoadFromMaterial(Material material, bool force)
        {
            UnLoadFromTexture(material, Loader._MainTex_PROPERTY, force);
            UnLoadFromTexture(material, Loader._Emissive_PROPERTY, force);
            UnLoadFromTexture(material, Loader._BumpMap_PROPERTY, force);
        }

        public static void UnLoadFromTexture(Material material, int id, bool force)
        {
            Texture2D texture = (Texture2D)material.GetTexture(id);
            if (texture != null)
            {

                TexRefCnt texRef;
                if (textureDictionary.ContainsKey(texture.name))
                {
                    texRef = textureDictionary[texture.name];
                }
                else
                {
                    texRef = new TexRefCnt(texture);
                    textureDictionary[texture.name] = texRef;
                }

                if (texRef.count > 0)
                {
                    texRef.count--;
                }
                if (texRef.count <= 0 && texRef.texInfo != null && !texRef.unloaded)
                {
                    if (force)
                    {
                        TextureConverter.Minimize(texRef.texInfo);
                        texRef.unloaded = true;
                    }
                    else
                    {
                        unloadQueue.Enqueue(texRef);
                    }
                }
            }
        }

        int count = 0;
        GameDatabase.TextureInfo texInfo;
        bool unloaded = false;
        public TexRefCnt(Texture2D tex)
        {
            texInfo = GameDatabase.Instance.GetTextureInfo(tex.name);
            if(texInfo == null)
            {
                KSPLog.print("texInfo for "+tex.name+" is null!");
            }
        }
    }
}
