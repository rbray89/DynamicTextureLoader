using System;
using System.Collections;
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
            KSPLog.print("DynamicTextureLoader: " + s);
        }
        static int partLoadedIndex = 0;
        ConfigNode moduleNode;
        private void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                List<AvailablePart> parts = PartLoader.LoadedPartsList;
                int i = partLoadedIndex;
                for (; parts != null && i < parts.Count; i++)
                {
                    AvailablePart ap = parts[i];
                    if (ap.partUrl != null && ap.partUrl != "" && ap.TechRequired != "Unresearcheable")
                    {
                        Part part = ap.partPrefab;
                        TextureUnloaderPartModule module = (TextureUnloaderPartModule)part.AddModule(typeof(TextureUnloaderPartModule).Name);
                        MethodInfo mI = typeof(PartModule).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                        mI.Invoke(module, null);
                        module.Load(moduleNode);
                        module.Unload(true);
                    }
                    else
                    {
                        Log(ap.name + " Not unloaded.");
                    }
                }
                partLoadedIndex = i;
            }
        }

        private void Start()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (HighLogic.LoadedScene == GameScenes.LOADING && !loaded)
            {
                Log("Version: " + assembly.GetName().Version);
                loaded = true;
                _MainTex = Shader.PropertyToID("_MainTex");
                _BumpMap = Shader.PropertyToID("_BumpMap");
                _Emissive = Shader.PropertyToID("_Emissive");

                moduleNode = new ConfigNode("MODULE");
                moduleNode.SetValue("name", typeof(TextureUnloaderPartModule).Name, true);
                
                Type gdType = typeof(GameDatabase);
                List<DatabaseLoader<GameDatabase.TextureInfo>> textureLoaders =
                    (from fld in gdType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                     where fld.FieldType == typeof(List<DatabaseLoader<GameDatabase.TextureInfo>>)
                     select (List<DatabaseLoader<GameDatabase.TextureInfo>>)fld.GetValue(GameDatabase.Instance)).FirstOrDefault();

                DatabaseLoaderTexture_DTL textureLoaderDTL = (DatabaseLoaderTexture_DTL)textureLoaders.First(t => t.GetType() == typeof(DatabaseLoaderTexture_DTL));

                DatabaseLoaderAttrib loaderAttrib = (DatabaseLoaderAttrib)Attribute.GetCustomAttribute(typeof(DatabaseLoaderTexture_DTL), typeof( DatabaseLoaderAttrib));
                foreach (var textureLoader in textureLoaders)
                {
                    if (textureLoader.GetType() != typeof(DatabaseLoaderTexture_DTL))
                    {
                        Log("Disabling " + textureLoader.GetType().Name);
                        textureLoaderDTL.setLoader(textureLoader);
                        textureLoader.extensions.RemoveAll(i => loaderAttrib.extensions.Contains(i));
                        Log(textureLoader.GetType().Name + " now has extensions: " + String.Join(", ", textureLoader.extensions.ToArray()));
                        
                    }
                    else
                    {
                        ((DatabaseLoaderTexture_DTL)textureLoader).fixExtensions(loaderAttrib.extensions.ToList());
                    }
                }
                
            }
            
            //unload any materials added by mods.
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER && !unloaded)
            {
                foreach (AvailablePart ap in PartLoader.LoadedPartsList)
                {

                    Part part = ap.partPrefab;
                    if (part != null)
                    {
                        TextureUnloaderPartModule module = (TextureUnloaderPartModule)part.FindModuleImplementing<TextureUnloaderPartModule>();
                        if (module != null)
                        {
                            module.Unload(true);
                        }
                    }
                }
                unloaded = true;
            }
            
            System.GC.Collect();
            //Resources.UnloadUnusedAssets();
            
        }
    }


    [DatabaseLoaderAttrib(new string[] { "mbm", "png", "tga", "jpg", "jpeg", "truecolor", "dds" })]
    public class DatabaseLoaderTexture_DTL : DatabaseLoader<GameDatabase.TextureInfo>
    {
        Dictionary<string,DatabaseLoader<GameDatabase.TextureInfo>> textureLoaders = new Dictionary<string, DatabaseLoader<GameDatabase.TextureInfo>>();
        private static Dictionary<string, Texture2D> texHashDictionary = new Dictionary<string, Texture2D>();

        public DatabaseLoaderTexture_DTL() : base()
        {

        }

        internal static GameDatabase.TextureInfo Load(UrlDir.UrlFile urlFile)
        {
            string hash = TexRefCnt.GetMD5String(urlFile.fullPath);
            GameDatabase.TextureInfo texInfo = new GameDatabase.TextureInfo(urlFile, null, false, false, false);
            bool hasMipmaps = updateToStockSettings(texInfo);
            texInfo.name = urlFile.url;

            string cached = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/ScaledTexCache/" + texInfo.file.name + "_hash_" + hash;
            /*
            if (texHashDictionary.ContainsKey(hash))
            {
                texInfo.texture = texHashDictionary[hash];

                foreach(UrlDir.UrlConfig config in GameDatabase.Instance.root.AllConfigs)
                {
                    ConfigNode model = config.config.GetNode("MODEL");
                    if (config.type == "PART" && model != null)
                    {
                        int i = 0;
                        string value = model.GetValue("texture", i);
                        while(value != null)
                        {
                            String replace = @"(.*),\s*" + Regex.Escape(urlFile.url);
                            if (Regex.IsMatch(value, replace))
                            {
                                model.SetValue("texture", Regex.Replace(value, replace, "$1, " + texInfo.texture.name), i);
                            }
                            i++;
                            value = model.GetValue("texture", i);
                        }
                        
                    }
                }

            }
            else */
            if (File.Exists(cached))
            {
                Loader.Log("Loaded From cache @" + cached);
                byte[] cache = System.IO.File.ReadAllBytes(cached);
                TextureFormat format = texInfo.isCompressed ? TextureFormat.DXT5 : TextureFormat.ARGB32;
                texInfo.texture = new Texture2D(32, 32, format, hasMipmaps);
                texInfo.texture.Apply(hasMipmaps, !texInfo.isReadable);
                texInfo.texture.LoadImage(cache);
                texHashDictionary[hash] = texInfo.texture;
                //add texture reference.

                texInfo.texture.name = texInfo.name;
                TexRefCnt texRef = new TexRefCnt(texInfo, hash, true);

            }
            else
            {
                TextureConverter.Reload(texInfo, false, default(Vector2), null, hasMipmaps);
                texHashDictionary[hash] = texInfo.texture;
                texInfo.texture.name = texInfo.name;
                TexRefCnt texRef = new TexRefCnt(texInfo, hash, false);
            }
            Loader.Log(texInfo.file.fileExtension + " c: " + texInfo.isCompressed + " n: " + texInfo.isNormalMap + " r: " + texInfo.isReadable + " m: " + (texInfo.texture.mipmapCount > 1));
            return texInfo;
        }

        private static bool updateToStockSettings(GameDatabase.TextureInfo texInfo)
        {
            texInfo.isNormalMap = texInfo.file.name.EndsWith("NRM");
            texInfo.isReadable = texInfo.file.fileExtension == "dds" || texInfo.file.fileExtension == "truecolor" || texInfo.isNormalMap ? false : true;
            texInfo.isCompressed = texInfo.file.fileExtension == "truecolor" ? false : true;
            bool hasMipmaps = texInfo.isNormalMap || texInfo.file.fileExtension == "dds" || texInfo.file.fileExtension == "tga";
            return hasMipmaps;
        }

        public override IEnumerator Load(UrlDir.UrlFile urlFile, FileInfo file)
        {
            GameDatabase.TextureInfo texInfo =
                Load(urlFile);
            if (texInfo != null)
            {
                obj = texInfo;
                successful = true;
            }
            yield return null;
        }

        internal void fixExtensions(List<string> list)
        {
            extensions = list;
        }

        internal void setLoader(DatabaseLoader<GameDatabase.TextureInfo> textureLoader)
        {
            foreach (string extension in textureLoader.extensions)
            {
                if (extensions.Contains(extension))
                {
                    this.textureLoaders[extension] = textureLoader;
                }
            }
        }
    }



}