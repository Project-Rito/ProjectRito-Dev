using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLFrameworkEngine
{
    public interface IInstanceDrawable
    {
        GLTransform Transform { get; set; }

        void DrawModel(GLContext control, Pass pass, List<GLTransform> transforms = null);

        bool GroupsWith(IInstanceDrawable drawable);
    }
}
