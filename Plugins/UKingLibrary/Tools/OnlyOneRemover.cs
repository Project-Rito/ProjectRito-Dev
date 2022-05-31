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
    public class OnlyOneRemover
    {
        public static void Remove(string actorName, string fieldName, string savePath)
        {
            Dictionary<string, Dictionary<string, BymlFileData>> field = new Dictionary<string, Dictionary<string, BymlFileData>>();

            // Load all the muunts in the field
            foreach (string sectionName in GlobalData.SectionNames)
            {
                foreach (string muuntEnding in GlobalData.MuuntEndings)
                {
                    string path = File.Exists(Path.Join(savePath, $"aoc/0010/Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin")) 
                        ? Path.Join(savePath, $"aoc/0010/Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin")
                        : PluginConfig.GetContentPath($"Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin");

                    field.TryAdd(sectionName, new Dictionary<string, BymlFileData>());
                    field[sectionName].Add(muuntEnding, ByamlFile.LoadN(STFileLoader.TryDecompressFile(File.OpenRead(path), Path.GetFileName(path)).Stream));
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, BymlFileData>> section in field)
            {
                foreach (string ending in section.Value.Keys) {
                    BymlFileData muunt = section.Value[ending];

                    bool modified = false;
                    foreach (Dictionary<string, dynamic> obj in muunt.RootNode["Objs"])
                    {
                        if (obj["UnitConfigName"] == actorName && obj.ContainsKey("OnlyOne"))
                        {
                            obj.Remove("OnlyOne");
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        Directory.CreateDirectory(Path.Join(savePath, $"aoc/0010/Map/{fieldName}/{section.Key}"));
                        Stream fileStream = File.Open(Path.Join(savePath, $"aoc/0010/Map/{fieldName}/{section.Key}/{section.Key}_{ending}.smubin"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

                        MemoryStream uncompressed = new MemoryStream();
                        ByamlFile.SaveN(uncompressed, muunt);
                        fileStream.Write(YAZ0.Compress(uncompressed.ToArray()));

                        fileStream.SetLength(fileStream.Position);
                        fileStream.Close();
                    }
                }
            }
        }
    }
}