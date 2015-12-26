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

        public static void LoadFromRenderer(MeshRenderer renderer)
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
                if (texRef.count == 1 && texRef.texInfo != null)
                {
                    TextureConverter.Reload(texRef.texInfo, true);
                }
            }
        }

        public static void UnLoadFromRenderer(MeshRenderer renderer)
        {
            foreach (Material material in renderer.materials)
            {
                UnLoadFromMaterial(material);
            }
        }

        public static void UnLoadFromMaterial(Material material)
        {
            UnLoadFromTexture(material, Loader._MainTex_PROPERTY);
            UnLoadFromTexture(material, Loader._Emissive_PROPERTY);
            UnLoadFromTexture(material, Loader._BumpMap_PROPERTY);
        }

        public static void UnLoadFromTexture(Material material, int id)
        {
            Texture2D texture = (Texture2D)material.GetTexture(id);
            if (texture != null && textureDictionary.ContainsKey(texture.name))
            {

                TexRefCnt texRef = textureDictionary[texture.name];
                if (texRef.count > 0)
                {
                    texRef.count--;
                }
                if (texRef.count <= 0 && texRef.texInfo != null)
                {
                    TextureConverter.Minimize(texRef.texInfo);
                }
            }
            else if (texture != null)
            {
                TexRefCnt texRef = new TexRefCnt(texture);
                textureDictionary[texture.name] = texRef;
                if (texRef.texInfo != null)
                {
                    TextureConverter.Minimize(texRef.texInfo);
                }
            }
        }

        int count = 0;
        GameDatabase.TextureInfo texInfo;
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
