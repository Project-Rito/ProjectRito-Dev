using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ByamlExt.Byaml;
using Toolbox.Core.IO;
using HKX2;

namespace UKingLibrary
{
    public class CollisionCacher
    {
        public static void Cache(string savePath)
        {
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
                                        if (obj["UnitConfigName"] == "TwnObj_Village_HatenoHouse_A_L_01")
                                            Console.WriteLine();
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
            Directory.CreateDirectory(savePath);
            foreach (KeyValuePair<string, BakedCollisionShapeCacheable[]> actor in actorShapes)
            {
                MapCollisionLoader collisionLoader = new MapCollisionLoader();
                collisionLoader.CreateForCaching();
                foreach (BakedCollisionShapeCacheable info in actor.Value)
                    collisionLoader.AddShape(info, 0, System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, System.Numerics.Vector3.One);
                collisionLoader.Save(File.Create(Path.Join(savePath, $"{actor.Key}.hksc")));

            }
        }
    }
}