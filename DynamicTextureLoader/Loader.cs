using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
                        module.Unload(true, false);
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


                /*
                List<LoadingSystem> list = LoadingScreen.Instance.loaders;

                if (list != null)
                {

                    GameObject aGameObject = new GameObject("ModuleManager");
                    TexPatchLoader loader = aGameObject.AddComponent<TexPatchLoader>();
                    int index = list.FindIndex(l => l.GetType() == typeof(PartLoader));
                    list.Insert(index, loader);
                    //list.Insert(2, loader);
                }
                */
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

    internal class TexPatchLoader// : LoadingSystem
    {
        private static Dictionary<string, string> texMapDictionary = new Dictionary<string, string>();
        static bool xrefed = false;
        public static string GetReplacement(string url)
        {
            loadDictionary();
            if (texMapDictionary.ContainsKey(url))
            {
                return texMapDictionary[url];
            }
            return null;
        }

        public static void loadDictionary()
        {
            if (!xrefed)
            {
                DatabaseLoaderAttrib loaderAttrib = (DatabaseLoaderAttrib)Attribute.GetCustomAttribute(typeof(DatabaseLoaderTexture_DTL), typeof(DatabaseLoaderAttrib));
                foreach (UrlDir.UrlConfig config in GameDatabase.Instance.root.AllConfigs.ToArray())
                {
                    ConfigNode[] models = config.config.GetNodes("MODEL");
                    if ((config.type == "PART" || config.type == "INTERNAL") && models != null)
                    {
                        foreach (ConfigNode model in models)
                        {
                            int i = 0;
                            string modelUrl = model.GetValue("model");
                            String modelCapture = @"(.*/)([^/\\]+)$";
                            if (Regex.IsMatch(modelUrl, modelCapture))
                            {
                                modelUrl = Regex.Match(modelUrl, modelCapture).Groups[1].Value;
                                string value = model.GetValue("texture", i);
                                while (value != null)
                                {
                                    String capture = @"(.*[^\s])\s*,\s*(.*[^\s])";
                                    if (Regex.IsMatch(value, capture))
                                    {
                                        Match match = Regex.Match(value, capture);
                                        string realUrl = match.Groups[2].Value;
                                        string dummyUrl = modelUrl + match.Groups[1].Value;
                                        Loader.Log("stashing " + dummyUrl + " -> " + realUrl);
                                        if (texMapDictionary.ContainsKey(dummyUrl) && texMapDictionary[dummyUrl] != realUrl)
                                        {
                                            Loader.Log("Dummy has multiple refs!");
                                        }
                                        else
                                        {
                                            texMapDictionary[dummyUrl] = realUrl;
                                        }

                                    }
                                    i++;
                                    value = model.GetValue("texture", i);
                                }
                            }
                        }
                    }
                }
                xrefed = true;
            }
        }

        /*
        bool ready = false;
        public override bool IsReady()
        {
            
            return ready;
        }
        public override void StartLoad()
        {
            foreach (UrlDir.UrlConfig config in GameDatabase.Instance.root.AllConfigs.ToArray())
            {
                ConfigNode model = config.config.GetNode("MODEL");
                if ((config.type == "PART" || config.type == "INTERNAL") && model != null)
                {
                   // model.values.RemoveValues("texture");
                }
            }
            ready = true;
        }*/
    }

    [DatabaseLoaderAttrib(new string[] { "truecolor", "tga", "png", "mbm", "jpg", "jpeg", "dds" })]
    public class DatabaseLoaderTexture_DTL : DatabaseLoader<GameDatabase.TextureInfo>
    {
        Dictionary<string,DatabaseLoader<GameDatabase.TextureInfo>> textureLoaders = new Dictionary<string, DatabaseLoader<GameDatabase.TextureInfo>>();
        private static Dictionary<string, Texture2D> texHashDictionary = new Dictionary<string, Texture2D>();

        public DatabaseLoaderTexture_DTL() : base()
        {

        }

        internal static GameDatabase.TextureInfo Load(UrlDir.UrlFile urlFile)
        {
            UrlDir.UrlFile file = urlFile;
            string hash = TexRefCnt.GetMD5String(file.fullPath);
            string urlReplace = TexPatchLoader.GetReplacement(file.url);
            
            if (urlReplace != null)
            {
             //   file = getReplacementFile(urlReplace);
             //   hash = TexRefCnt.GetMD5String(file.fullPath);
                GameDatabase.TextureInfo dummy = new GameDatabase.TextureInfo(file, new Texture2D(2, 2), false, false, false);
                dummy.name = file.url;
                dummy.texture.name = dummy.name;
                return dummy;
            }

            GameDatabase.TextureInfo texInfo = new GameDatabase.TextureInfo(file, null, false, false, false);
            bool hasMipmaps = updateToStockSettings(texInfo);
            string cached = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/ScaledTexCache/" + Path.GetFileName(file.url) + "_hash_" + hash;

            Loader.Log("hash: "+hash);

            if (texHashDictionary.ContainsKey(hash))
            {
                Loader.Log("Loaded From hash");
                texInfo.texture = texHashDictionary[hash];
            }
            else if (File.Exists(cached))
            {
                Loader.Log("Loaded From cache @" + cached);
                byte[] cache = System.IO.File.ReadAllBytes(cached);
                TextureFormat format = texInfo.isCompressed ? TextureFormat.DXT5 : TextureFormat.ARGB32;
                texInfo.texture = new Texture2D(32, 32, format, hasMipmaps);
                if(texInfo.file.fileExtension == "dds")
                {
                    //texInfo.isReadable = true;
                }
                texInfo.texture.Apply(hasMipmaps, !texInfo.isReadable);
                texInfo.texture.LoadImage(cache);
                texHashDictionary[hash] = texInfo.texture;
                //add texture reference.
                TexRefCnt texRef = new TexRefCnt(texInfo, hash, true);
                texInfo.texture.name = file.url;
            }
            else
            {
                TextureConverter.Reload(texInfo, false, default(Vector2), null, hasMipmaps);
                texHashDictionary[hash] = texInfo.texture;
                TexRefCnt texRef = new TexRefCnt(texInfo, hash, false);
                texInfo.texture.name = file.url;
            }

            Loader.Log(texInfo.file.fileExtension + " c: " + texInfo.isCompressed + " n: " + texInfo.isNormalMap + " r: " + texInfo.isReadable + " m: " + (texInfo.texture.mipmapCount > 1));
            
            texInfo.name = urlFile.url;
            Loader.Log(texInfo.name + " -> " + texInfo.texture.name);
            return texInfo;
        }

        private static UrlDir.UrlFile getReplacementFile(string urlReplace)
        {
            Loader.Log("mapping to " + urlReplace);
            DatabaseLoaderAttrib loaderAttrib = (DatabaseLoaderAttrib)Attribute.GetCustomAttribute(typeof(DatabaseLoaderTexture_DTL), typeof(DatabaseLoaderAttrib));
            string fileBase = UrlDir.ApplicationRootPath + "GameData/" + urlReplace;
            foreach (String ext in loaderAttrib.extensions)
            {
                string file = fileBase + "." + ext;
                if (File.Exists(file))
                {
                    Loader.Log("Getting replacement from " + file);
                    return new UrlDir.UrlFile(GameDatabase.Instance.root, new FileInfo(file));
                }
            }
            return null;
        }

        private static bool updateToStockSettings(GameDatabase.TextureInfo texInfo)
        {
            texInfo.isNormalMap = texInfo.file.name.EndsWith("NRM");
            texInfo.isReadable = texInfo.file.fileExtension == "dds" || texInfo.file.fileExtension == "truecolor" || texInfo.isNormalMap ? false : true;
            texInfo.isCompressed = texInfo.file.fileExtension == "truecolor" || texInfo.file.fileExtension == "tga" ? false : true;
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