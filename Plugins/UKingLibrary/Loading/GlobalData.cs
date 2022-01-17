using System.IO;
using System.Collections.Generic;
using ByamlExt.Byaml;

namespace UKingLibrary
{
    public class GlobalData
    {
        public static Dictionary<string, dynamic> Actors = new Dictionary<string, dynamic>();

        public static void LoadActorDatabase()
        {
            if (Actors.Count > 0)
                return;

            MapStudio.UI.ProcessLoading.Instance.Update(60, 100, "Loading actor database");

            //Todo cache these somewhere to load as decompressed next time
            var path = PluginConfig.GetContentPath("Actor\\ActorInfo.product.sbyml");
            var decompressed = Toolbox.Core.IO.YAZ0.Decompress(path);
            var actorData = ByamlFile.LoadN(new MemoryStream(decompressed));

            foreach (IDictionary<string, dynamic> actorInfo in actorData.RootNode["Actors"])
            {
                Actors.Add((string)actorInfo["name"], actorInfo);
            }
        }
    }
}
