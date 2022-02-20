using System.Collections.Generic;

namespace GLFrameworkEngine
{
    public interface IInstanceColorPickable : IColorPickable
    {
        void DrawColorPicking(GLContext context, List<GLTransform> transforms);
    }
}
