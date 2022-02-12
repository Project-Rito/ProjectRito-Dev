using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class Renderbuffer : GLObject, IFramebufferAttachment
    {
        public int Width { get; }

        public int Height { get; }

        public RenderbufferStorage InternalFormat { get; private set; }

        public Renderbuffer(int width, int height, RenderbufferStorage internalFormat)
            : base(GLH.GenRenderbuffer())
        {
            Width = width;
            Height = height;
            InternalFormat = internalFormat;

            // Allocate storage for the renderbuffer.
            Bind();
            GLH.RenderbufferStorage(RenderbufferTarget.Renderbuffer, internalFormat, width, height);
        }

        public Renderbuffer(int width, int height, int samples, RenderbufferStorage internalFormat)
          : base(GLH.GenRenderbuffer())
        {
            Width = width;
            Height = height;
            InternalFormat = internalFormat;

            // Allocate storage for the renderbuffer.
            Bind();
            GLH.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4,
                internalFormat, width, height);
        }

        public void Bind() {
            GLH.BindRenderbuffer(RenderbufferTarget.Renderbuffer, ID);
        }

        public void Unbind() {
            GLH.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        public void Attach(FramebufferAttachment attachment, Framebuffer target) {
            target.Bind();
            GLH.FramebufferRenderbuffer(target.Target, attachment, RenderbufferTarget.Renderbuffer, ID);
        }

        public void Dispose() {
            GLH.DeleteRenderbuffer(ID);
        }
    }
}
