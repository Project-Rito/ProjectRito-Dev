using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ByamlExt.Byaml;
using Toolbox.Core;
using Toolbox.Core.IO;
using CafeLibrary;
using HKX2;

namespace UKingLibrary
{
    public class CollisionCacher
    {
        public static void CacheAll(string cacheDir)
        {
            CacheDungeonCollision(cacheDir);
            CacheFieldCollision(cacheDir);
            
        }

        public static void CacheFieldCollision(string cacheDir)
        {
            Directory.CreateDirectory(cacheDir);

            Dictionary<string, Dictionary<string, List<BymlFileData>>> fields = new Dictionary<string, Dictionary<string, List<BymlFileData>>>();
            foreach (string fieldName in GlobalData.FieldNames)
            {
                Dictionary<string, List<BymlFileData>> field = new Dictionary<string, List<BymlFileData>>();
                foreach (string sectionName in GlobalData.SectionNames)
                {
                    foreach (string muuntEnding in GlobalData.MuuntEndings)
                    {
                        string path = PluginConfig.GetContentPath($"Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin");
                        field.TryAdd(sectionName, new List<BymlFileData>());
                        field[sectionName].Add(ByamlFile.LoadN(STFileLoader.TryDecompressFile(File.OpenRead(path), Path.GetFileName(path)).Stream));
                    }
                }
                fields.Add(fieldName, field);
            }

            Dictionary<string, BakedCollisionShapeCacheable[]> actorShapes = new Dictionary<string, BakedCollisionShapeCacheable[]>();
            foreach (string fieldName in GlobalData.FieldNames)
            {
                foreach (KeyValuePair<string, Dictionary<string, List<BymlFileData>>> field in fields)
                {
                    foreach (KeyValuePair<string, List<BymlFileData>> section in field.Value)
                    {
                        List<MapCollisionLoader> collisionLoaders = new List<MapCollisionLoader>(4);
                        for (int compoundIdx = 0; compoundIdx < 4; compoundIdx++)
                        {
                            string path = PluginConfig.GetContentPath($"Physics/StaticCompound/{fieldName}/{section.Key}-{compoundIdx}.shksc");
                            MapCollisionLoader loader = new MapCollisionLoader();
                            loader.Load(File.OpenRead(path), Path.GetFileName(path));
                            collisionLoaders.Add(loader);
                        }


                        foreach (BymlFileData muunt in section.Value)
                        {
                            foreach (Dictionary<string, dynamic> obj in muunt.RootNode["Objs"])
                            {
                                if (!actorShapes.ContainsKey(obj["UnitConfigName"]))
                                {
                                    foreach (MapCollisionLoader collisionLoader in collisionLoaders)
                                    {
                                        BakedCollisionShapeCacheable[] infos = collisionLoader.GetCacheables(obj["HashId"]);
                                        if (infos != null)
                                            actorShapes.Add(obj["UnitConfigName"], infos);
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

                        BymlFileData muunt = ByamlFile.LoadN(muuntSettings.Stream);

                        foreach (Dictionary<string, dynamic> obj in muunt.RootNode["Objs"])
                        {
                            BakedCollisionShapeCacheable[] infos = collisionLoader.GetCacheables(obj["HashId"]);
                            if (infos != null)
                                actorShapes.TryAdd(obj["UnitConfigName"], infos);
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
    }
}