using System;
using System.Net;
using System.IO;
using System.IO.Compression;
using MapStudio.UI;

namespace UKingLibrary
{
    public class ActorDocs
    {
        public static void Update()
        {
            using (var webClient = new WebClient())
                webClient.DownloadFile("https://github.com/Project-Rito/Botw-ActorDocs/archive/refs/heads/main.zip", "Botw-ActorDocs.zip");
            ZipFile.ExtractToDirectory("Botw-ActorDocs.zip", $"{Directory.GetCurrentDirectory()}");

            var enumOptions = new EnumerationOptions();
            enumOptions.RecurseSubdirectories = true;
            foreach (var file in Directory.GetFiles("Botw-ActorDocs-main/Languages", "*", enumOptions))
            {
                string targetFile = Path.Combine("Languages", Path.GetRelativePath("Botw-ActorDocs-main/Languages", file));
                if (File.Exists(targetFile))
                    File.Delete(targetFile);
                File.Move(file, targetFile);
            }

            // Cleanup
            File.Delete("Botw-ActorDocs.zip");
            foreach (var file in Directory.GetFiles("Botw-ActorDocs-main"))
                File.Delete(file);
            Directory.Delete("Botw-ActorDocs-main", true);

            TranslationSource.Instance.Reload();
        }
    }
}
