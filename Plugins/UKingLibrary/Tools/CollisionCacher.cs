using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nintendo.Byml;
using Toolbox.Core;
using Toolbox.Core.IO;
using CafeLibrary;
using HKX2;
using MapStudio.UI;

namespace UKingLibrary
{
    public class CollisionCacher
    {
        public static void CacheAll(string cacheDir)
        {
            CacheDungeonCollision(cacheDir);
            CacheFieldCollision(cacheDir);
            TinyFileDialog.MessageBoxInfoOk(TranslationSource.GetText($"BAKED_COLLISION_CACHED"));
        }

        public static void CacheFieldCollision(string cacheDir)
        {
            Directory.CreateDirectory(cacheDir);

            Dictionary<string, Dictionary<string, List<BymlFile>>> fields = new Dictionary<string, Dictionary<string, List<BymlFile>>>();
            foreach (string fieldName in GlobalData.FieldNames)
            {
                Dictionary<string, List<BymlFile>> field = new Dictionary<string, List<BymlFile>>();
                foreach (string sectionName in GlobalData.SectionNames)
                {
                    foreach (string muuntEnding in GlobalData.MuuntEndings)
                    {
                        string path = PluginConfig.GetContentPath($"Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin");
                        field.TryAdd(sectionName, new List<BymlFile>());
                        STFileLoader.Settings mubinSettings = STFileLoader.TryDecompressFile(File.OpenRead(path), Path.GetFileName(path));
                        if (mubinSettings.Stream == null)
                        {
                            StudioLogger.WriteError($"Trouble reading {path}!");
                            continue;
                        }
                        field[sectionName].Add(BymlFile.FromBinary(mubinSettings.Stream));
                    }
                }
                fields.Add(fieldName, field);
            }

            Dictionary<string, BakedCollisionShapeCacheable[]> actorShapes = new Dictionary<string, BakedCollisionShapeCacheable[]>();
            foreach (string fieldName in GlobalData.FieldNames)
            {
                foreach (KeyValuePair<string, Dictionary<string, List<BymlFile>>> field in fields)
                {
                    foreach (KeyValuePair<string, List<BymlFile>> section in field.Value)
                    {
                        List<MapCollisionLoader> collisionLoaders = new List<MapCollisionLoader>(4);
                        for (int compoundIdx = 0; compoundIdx < 4; compoundIdx++)
                        {
                            string path = PluginConfig.GetContentPath($"Physics/StaticCompound/{fieldName}/{section.Key}-{compoundIdx}.shksc");
                            MapCollisionLoader loader = new MapCollisionLoader();
                            loader.Load(File.OpenRead(path), Path.GetFileName(path));
                            collisionLoaders.Add(loader);
                        }


                        foreach (BymlFile muunt in section.Value)
                        {
                            if (muunt.RootNode.Hash["Objs"].Array != null)
                            {
                                foreach (IDictionary<string, BymlNode> obj in muunt.RootNode.Hash["Objs"].Array.Select(x => x.Hash))
                                {
                                    if (!actorShapes.ContainsKey(obj["UnitConfigName"].String))
                                    {
                                        foreach (MapCollisionLoader collisionLoader in collisionLoaders)
                                        {
                                            BakedCollisionShapeCacheable[] infos = collisionLoader.GetCacheables(obj["HashId"].UInt);
                                            if (infos != null)
                                                actorShapes.TryAdd(obj["UnitConfigName"].String, infos);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, BakedCollisionShapeCacheable[]> actor in actorShapes)
            {
                MapCollisionLoader collisionLoader = new MapCollisionLoader();
                collisionLoader.CreateForCaching();
                foreach (BakedCollisionShapeCacheable info in actor.Value)
                    collisionLoader.AddShape(info, 0, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, System.Numerics.Vector3.One);
                collisionLoader.Save(File.Create(Path.Join(cacheDir, $"{actor.Key}.hksc")));
            }
        }

        public static void CacheDungeonCollision(string cacheDir)
        {
            Directory.CreateDirectory(cacheDir);

            Dictionary<string, BakedCollisionShapeCacheable[]> actorShapes = new Dictionary<string, BakedCollisionShapeCacheable[]>();
            foreach (string packDir in PluginConfig.GetContentPaths($"Pack/"))
            {
                foreach (string packfile in Directory.EnumerateFiles(packDir).Where((path) => 
                    {
                        string name = Path.GetFileName(path);

                        if (!name.EndsWith(".pack"))
                            return false;
                        if ((name == "Title.pack" || name.StartsWith("TitleBG") || name.StartsWith("Bootup")))
                            return false;

                        return true;
                    }))
                {
                    STFileLoader.Settings packSettings = STFileLoader.TryDecompressFile(File.OpenRead(packfile), packfile);
                    if (packSettings.Stream == null)
                    {
                        StudioLogger.WriteError($"Trouble reading {packfile}!");
                        continue;
                    }
                    SARC pack = new SARC();
                    pack.Load(packSettings.Stream, packfile);

                    string dungeonName = Path.GetFileNameWithoutExtension(packfile);

                    byte[] data;

                    MapCollisionLoader collisionLoader = null;
                    if (pack.SarcData.Files.TryGetValue($"Physics/StaticCompound/CDungeon/{dungeonName}.shksc", out data))
                    {
                        collisionLoader = new MapCollisionLoader();
                        collisionLoader.Load(new MemoryStream(data), $"{dungeonName}.shksc");
                    } 
                    if (pack.SarcData.Files.TryGetValue($"Physics/StaticCompound/MainFieldDungeon/{dungeonName}.shksc", out data))
                    {
                        collisionLoader = new MapCollisionLoader();
                        collisionLoader.Load(new MemoryStream(data), $"{dungeonName}.shksc");
                    }

                    if (collisionLoader == null)
                        continue;

                    foreach (string muuntEnding in GlobalData.MuuntEndings)
                    {
                        STFileLoader.Settings muuntSettings = null;
                        if (pack.SarcData.Files.TryGetValue($"Map/CDungeon/{dungeonName}/{dungeonName}_{muuntEnding}.smubin", out data))
                            muuntSettings = STFileLoader.TryDecompressFile(new MemoryStream(data), $"{dungeonName}_{muuntEnding}.smubin");
                        if (pack.SarcData.Files.TryGetValue($"Map/MainFieldDungeon/{dungeonName}/{dungeonName}_{muuntEnding}.smubin", out data))
                            muuntSettings = STFileLoader.TryDecompressFile(new MemoryStream(data), $"{dungeonName}_{muuntEnding}.smubin");

                        if (muuntSettings == null)
                            continue;

                        BymlFile muunt = BymlFile.FromBinary(muuntSettings.Stream);

                        if (muunt.RootNode.Hash["Objs"].Array != null)
                        {
                            foreach (IDictionary<string, BymlNode> obj in muunt.RootNode.Hash["Objs"].Array.Select(x => x.Hash))
                            {
                                BakedCollisionShapeCacheable[] infos = collisionLoader.GetCacheables(obj["HashId"].UInt);
                                if (infos != null)
                                    actorShapes.TryAdd(obj["UnitConfigName"].String, infos);
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, BakedCollisionShapeCacheable[]> actor in actorShapes)
            {
                MapCollisionLoader collisionLoader = new MapCollisionLoader();
                collisionLoader.CreateForCaching();

                bool failure = false;
                foreach (BakedCollisionShapeCacheable info in actor.Value)
                    failure |= !collisionLoader.AddShape(info, 0, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, System.Numerics.Vector3.One);
                if (failure)
                    continue; // Do not cache if part of the collision is not obtainable
                collisionLoader.Save(File.Create(Path.Join(cacheDir, $"{actor.Key}.hksc")));
            }
        }
    }
}