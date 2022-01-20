using System;
using System.Collections.Generic;
using System.Text;

namespace MapStudio.UI
{
    public class AssetConfig
    {
        public float IconSize = 70.0f;

        //Todo need to figure out a good way to assign unique IDs to assets. 
        //Might be from a required file path field
        public Dictionary<string, AssetSettings> Settings = new Dictionary<string, AssetSettings>();

        public void UpdateCategory(AssetItem item, string category)
        {
            if (!Settings.ContainsKey(item.Name))
                Settings.Add(item.Name, new AssetSettings());

            Settings[item.Name].Categories = item.Categories;
        }

        public void ApplySettings(AssetItem item)
        {
            if (!Settings.ContainsKey(item.Name))
                return;

            var settings = Settings[item.Name];
            item.Categories = settings.Categories;
        }

        public class AssetSettings
        {
            public string[] Categories = new string[0];
        }
    }
}
