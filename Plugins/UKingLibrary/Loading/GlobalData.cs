using System.IO;
using System.Collections.Generic;
using ByamlExt.Byaml;

namespace UKingLibrary
{
    public class GlobalData
    {
        public static Dictionary<string, dynamic> Actors = new Dictionary<string, dynamic>();

        public static readonly string[] FieldNames = new string[]
            {
                "MainField",
                "AocField"
            };

        public static readonly string[] SectionNames = new string[]
            {
                "A-1",
                "A-2",
                "A-3",
                "A-4",
                "A-5",
                "A-6",
                "A-7",
                "A-8",
                "B-1",
                "B-2",
                "B-3",
                "B-4",
                "B-5",
                "B-6",
                "B-7",
                "B-8",
                "C-1",
                "C-2",
                "C-3",
                "C-4",
                "C-5",
                "C-6",
                "C-7",
                "C-8",
                "D-1",
                "D-2",
                "D-3",
                "D-4",
                "D-5",
                "D-6",
                "D-7",
                "D-8",
                "E-1",
                "E-2",
                "E-3",
                "E-4",
                "E-5",
                "E-6",
                "E-7",
                "E-8",
                "F-1",
                "F-2",
                "F-3",
                "F-4",
                "F-5",
                "F-6",
                "F-7",
                "F-8",
                "G-1",
                "G-2",
                "G-3",
                "G-4",
                "G-5",
                "G-6",
                "G-7",
                "G-8",
                "H-1",
                "H-2",
                "H-3",
                "H-4",
                "H-5",
                "H-6",
                "H-7",
                "H-8",
                "I-1",
                "I-2",
                "I-3",
                "I-4",
                "I-5",
                "I-6",
                "I-7",
                "I-8",
                "J-1",
                "J-2",
                "J-3",
                "J-4",
                "J-5",
                "J-6",
                "J-7",
                "J-8",
            };

        public static readonly string[] MuuntEndings = new string[]
        {
            "Static", 
            "Dynamic"
        };

    public static void LoadActorDatabase()
        {
            if (Actors.Count > 0)
                return;

            MapStudio.UI.ProcessLoading.Instance.Update(60, 100, "Loading actor database");

            //Todo cache these somewhere to load as decompressed next time
            var paths = PluginConfig.GetContentPaths("Actor\\ActorInfo.product.sbyml");
            foreach (var path in paths)
            {
                var decompressed = Toolbox.Core.IO.YAZ0.Decompress(path);
                var actorData = ByamlFile.LoadN(new MemoryStream(decompressed));

                foreach (IDictionary<string, dynamic> actorInfo in actorData.RootNode["Actors"])
                {
                    Actors[(string)actorInfo["name"]] = actorInfo;
                }
            }
        }
    }
}
