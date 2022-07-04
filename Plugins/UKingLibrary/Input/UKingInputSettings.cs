using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKingLibrary
{
    public class UKingInputSettings
    {
        public static UKingInputSettings INPUT = new UKingInputSettings();

        public Scene3D Scene = new Scene3D();

        public class Scene3D
        {
            public string PassthroughAreas = "Alt";
        }
    }
}
