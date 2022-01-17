using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using Toolbox.Core;

namespace MapStudio.UI
{
    /// <summary>
    /// Translation handler for translating given keys into localized text.
    /// </summary>
    public class TranslationSource : INotifyPropertyChanged
    {
        private static readonly TranslationSource instance = new TranslationSource();

        private Dictionary<string, string> Translation = new Dictionary<string, string>();

        /// <summary>
        /// The language key to determine what language file to use.
        /// </summary>
        public static string LanguageKey = "English";

        /// <summary>
        /// The instance of the translation source.
        /// </summary>
        public static TranslationSource Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Checks if the translation lookup has a given key or not.
        /// </summary>
        public static bool HasKey(string key) {
            if (key == null) return false;

            return Instance.Translation.ContainsKey(key);
        }

        /// <summary>
        /// Returns the localized text from a given key.
        /// </summary>
        public static string GetText(string key) {
            return Instance[key];
        }

        /// <summary>
        /// Returns the localized text from a given key.
        /// </summary>
        public string this[string key]
        {
            get 
            {
                //Return the key by itself if not contained in the translation list.
                if (!HasKey(key))
                    return key;

                return Translation[key];
            }
        }

        /// <summary>
        /// Gets a list of language file paths in the "Languages" folder.
        /// </summary>
        public static List<string> GetLanguages()
        {
            List<string> languages = new List<string>();
            foreach (var file in Directory.GetDirectories("Languages")) {
                languages.Add(new DirectoryInfo(file).Name);
            }
            return languages;
        }

        /// <summary>
        /// Updates the current language and reloads the translation list.
        /// </summary>
        public void Update(string key)
        {
            LanguageKey = key;
            Reload();
        }

        /// <summary>
        // Reloads the translation list with the current language key.
        /// </summary>
        public void Reload() {
            Translation.Clear();
            foreach (var file in Directory.GetFiles($"{Runtime.ExecutableDir}\\Languages/{LanguageKey}"))
                Load(file);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        /// <summary>
        // Loads the language file and adds the keys with localized text to the translation lookup.
        /// </summary>
        public void Load(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.StartsWith("#") || !(line.Contains("-") || line.Contains(":"))) // If this line contains nothing of value skip it
                        continue;


                    if (line.Contains(":") && !line.Contains("-")) // If this line indicates it's a text block, read out the text block. Prefer text lines.
                    {
                        var key = line.Split(':')[0].Trim();
                        var value = ReadTextBlock(reader);
                        if (!Translation.ContainsKey(key))
                            Translation.Add(key, value);
                        else
                            Translation[key] = value;
                    }
                    else // This must be a text line translation. Process it as such.
                    {
                        var entries = line.Split('-');
                        //Remove comments at end if used
                        var value = entries[1].Split('#').FirstOrDefault().Trim();
                        var key = entries[0].Trim();
                        if (!Translation.ContainsKey(key))
                            Translation.Add(key, value);
                        else
                            Translation[key] = value;
                    }
                }
            }
        }

        private string ReadTextBlock(StreamReader reader)
        {
            string text = "";
            string line = "";
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (line.Split('#')[0] == ":")
                    break;

                text += line + "\n";
            }

            text.Trim();
            return text;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
