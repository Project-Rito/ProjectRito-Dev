using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Nintendo.Byml;
using Toolbox.Core.IO;
using HKX2;

namespace UKingLibrary
{
    public class OnlyOneRemover
    {
        public static void Remove(string actorName, string fieldName, string savePath)
        {
            Dictionary<string, Dictionary<string, BymlFile>> field = new Dictionary<string, Dictionary<string, BymlFile>>();

            // Load all the muunts in the field
            foreach (string sectionName in GlobalData.SectionNames)
            {
                foreach (string muuntEnding in GlobalData.MuuntEndings)
                {
                    string path = File.Exists(Path.Join(savePath, $"aoc/0010/Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin")) 
                        ? Path.Join(savePath, $"aoc/0010/Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin")
                        : PluginConfig.GetContentPath($"Map/{fieldName}/{sectionName}/{sectionName}_{muuntEnding}.smubin");

                    field.TryAdd(sectionName, new Dictionary<string, BymlFile>());
                    field[sectionName].Add(muuntEnding, BymlFile.FromBinary(STFileLoader.TryDecompressFile(File.OpenRead(path), Path.GetFileName(path)).Stream));
                }
            }

            foreach (KeyValuePair<string, Dictionary<string, BymlFile>> section in field)
            {
                foreach (string ending in section.Value.Keys) {
                    BymlFile muunt = section.Value[ending];

                    bool modified = false;
                    foreach (IDictionary<string, BymlNode> obj in muunt.RootNode.Hash["Objs"].Array.Select(x => x.Hash))
                    {
                        if (obj["UnitConfigName"].String == actorName && obj.ContainsKey("OnlyOne"))
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
                        uncompressed.Write(muunt.ToBinary());
                        fileStream.Write(YAZ0.Compress(uncompressed.ToArray()));

                        fileStream.SetLength(fileStream.Position);
                        fileStream.Close();
                    }
                }
            }
        }
    }
}