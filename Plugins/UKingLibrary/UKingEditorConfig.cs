using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKingLibrary
{
    public class UKingEditorConfig
    {
        /// <summary>
        /// The name of the editor.
        /// </summary>
        public string Editor;
        public bool IsValid { get { return Editor == "UKingEditor"; } }

        public string FolderName;
        public Dictionary<string, List<string>> OpenMapUnits = new Dictionary<string, List<string>> { { "MainField", new List<string>() }, { "AocField", new List<string>() } };
    }
}
