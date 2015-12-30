using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
                }
                texRef.count++;
                if (texRef.count > 0 && texRef.texInfo != null && texRef.unloaded && 
                    (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                {
                    UnLoadFromQueue();
                    Reload(texRef.texInfo);
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
                    Minimize(texRef.texInfo);
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
                }

                if (texRef.count > 0)
                {
                    texRef.count--;
                }
                if (texRef.count <= 0 && texRef.texInfo != null && !texRef.unloaded)
                {
                    if (force)
                    {
                        Minimize(texRef.texInfo);
                        texRef.unloaded = true;
                    }
                    else
                    {
                        unloadQueue.Enqueue(texRef);
                    }
                }
            }
        }

        static String GetMD5String(String file)
        {
            string MD5String = null;
            if (File.Exists(file))
            {
                FileStream stream = File.OpenRead(file);
                MD5 md5 = MD5.Create();
                byte[] hash = md5.ComputeHash(stream);
                stream.Close();
                MD5String = BitConverter.ToString(hash);
            }
            return MD5String;
        }

        static Dictionary<string, Texture2D> texHashDictionary = new Dictionary<string, Texture2D>();
        internal static GameDatabase.TextureInfo Load(UrlDir.UrlFile urlFile)
        {
            string hash = GetMD5String(urlFile.fullPath);
            bool hasMipmaps = urlFile.fileExtension == "png" ? false : true;
            bool isNormalMap = urlFile.name.EndsWith("NRM");
            bool isReadable = urlFile.fileExtension == "dds" || isNormalMap ? false : true;
            bool isCompressed = urlFile.fileExtension == "tga" ? false : true;
            GameDatabase.TextureInfo texInfo = new GameDatabase.TextureInfo(urlFile, null, isNormalMap, isReadable, isCompressed);
            texInfo.name = urlFile.url;

            string cached = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/ScaledTexCache/" + urlFile.url + "_hash_" + hash;
            if (texHashDictionary.ContainsKey(hash))
            {
                texInfo.texture = texHashDictionary[hash];
            }
            else if (File.Exists(cached))
            {
                Loader.Log("Loaded From cache @" + cached);
                byte[] cache = System.IO.File.ReadAllBytes(cached);
                TextureFormat format = isCompressed ? TextureFormat.DXT5 : TextureFormat.ARGB32;
                texInfo.texture = new Texture2D(32, 32, format, hasMipmaps);
                texInfo.texture.Apply(hasMipmaps, !isReadable);
                texInfo.texture.LoadImage(cache);
                texHashDictionary[hash] = texInfo.texture;
                //add texture reference.
                new TexRefCnt(texInfo.texture, true);
            }
            else
            {
                Loader.Log("Caching @" + cached);
                TextureConverter.Reload(texInfo, false, default(Vector2), null, hasMipmaps);
                texHashDictionary[hash] = texInfo.texture;
            }
            return texInfo;
        }

        public static void Reload(GameDatabase.TextureInfo texInfo)
        {
            if (texInfo.texture != null)
            {
                Loader.Log("Reloading " + texInfo.texture.name);
                string hash = GetMD5String(texInfo.file.fullPath);
                string cached = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/TexCache/" + texInfo.file.url + "_hash_" + hash;
                if (File.Exists(cached))
                {
                    Loader.Log("Loaded From cache @" + cached);
                    byte[] cache = System.IO.File.ReadAllBytes(cached);
                    texInfo.texture.LoadImage(cache);
                }
                else
                {

                    Loader.Log("Caching @" + cached);
                    TextureConverter.Reload(texInfo, true, default(Vector2), cached);

                }
                Resources.UnloadUnusedAssets();
            }
        }

        public static void Minimize(GameDatabase.TextureInfo texInfo)
        {
            Vector2 scaleSize = new Vector2(32, 32);

            if (texInfo.texture != null && (texInfo.texture.width > scaleSize.x || texInfo.texture.height > scaleSize.y))
            {
                Loader.Log("Freeing " + texInfo.texture.name);
                string hash = GetMD5String(texInfo.file.fullPath);
                string cached = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/ScaledTexCache/" + texInfo.file.url + "_hash_" + hash;

                if (File.Exists(cached))
                {
                    Loader.Log("Loaded From cache @" + cached);
                    byte[] cache = System.IO.File.ReadAllBytes(cached);
                    texInfo.texture.LoadImage(cache);
                }
                else
                {
                    Loader.Log("Caching @" + cached);
                    TextureConverter.Reload(texInfo, true, scaleSize, cached);

                }
                Resources.UnloadUnusedAssets();
            }
        }


        int count = 0;
        GameDatabase.TextureInfo texInfo;
        bool unloaded = false;
        public TexRefCnt(Texture2D tex, bool unloaded = false)
        {
            texInfo = GameDatabase.Instance.GetTextureInfo(tex.name);
            if(texInfo == null)
            {
                Loader.Log("texInfo for "+tex.name+" is null!");
            }
            textureDictionary[tex.name] = this;
            this.unloaded = unloaded;
        }
    }
}
