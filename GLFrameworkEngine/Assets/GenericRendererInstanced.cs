using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using Toolbox.Core.ViewModels;

namespace GLFrameworkEngine
{
    public class GenericRendererInstanced : GenericRenderer
    {

        public GenericRendererInstanced() : base(null)
        {
        }

        public GenericRendererInstanced(NodeBase parent) : base(parent)
        {
        }

        public virtual void DrawColorIDPass(GLContext control, List<GLTransform> transforms = null)
        {

        }

        public virtual void DrawShadowModel(GLContext control, List<GLTransform> transforms = null)
        {

        }

        public virtual void DrawGBuffer(GLContext control, List<GLTransform> transforms = null)
        {

        }

        public virtual void DrawCaustics(GLContext control, GLTexture gbuffer, GLTexture linearDepth, List<GLTransform> transforms = null)
        {

        }

        public virtual void DrawCubeMapScene(GLContext control, List<GLTransform> transforms = null)
        {

        }

        public virtual void DrawSelection(GLContext control, List<GLTransform> transforms = null)
        {

        }
    }
}
