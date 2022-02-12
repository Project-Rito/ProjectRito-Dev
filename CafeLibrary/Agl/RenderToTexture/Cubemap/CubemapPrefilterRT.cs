using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class CubemapPrefilterRT
    {
        public static void CreateCubemap(GLContext control, GLTextureCube cubemapInput,
            GLTexture cubemapOutput, int layer)
        {
            int size = cubemapOutput.Width;

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, 0.1f, 10.0f);
            Matrix4[] captureViews = {
                 Matrix4.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
                 Matrix4.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)),
                 Matrix4.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)),
                 Matrix4.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)),
                 Matrix4.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)),
                 Matrix4.LookAt(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f)),
            };

            GLL.BindTexture(TextureTarget.TextureCubeMap, 0);

            //Bind the cubemap's texture into a filtered quad. 
            //Bind the drawn filter to a cubemap array layer
            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, size, size, PixelInternalFormat.Rgba32f);
            frameBuffer.Bind();

            GLL.Disable(EnableCap.Blend);

            var cubemapFilter = GlobalShaders.GetShader("CUBEMAP_PREFILTER");
            cubemapFilter.Enable();

            //Allocate mipmaps
            cubemapOutput.Bind();
            cubemapOutput.GenerateMipmaps();
            cubemapOutput.Unbind();

            GLL.ActiveTexture(TextureUnit.Texture0 + 1);
            cubemapInput.Bind();
            cubemapFilter.SetInt("environmentMap", 1);
            cubemapFilter.SetMatrix4x4("projection", ref projection);

            //Quick hack, draw once before rendering (first buffer not updating for some reason??)
            RenderTools.DrawCube();

            GLL.Disable(EnableCap.CullFace);
            for (int mip = 0; mip < cubemapOutput.MipCount; mip++)
            {
                int mipWidth = (int)(size * Math.Pow(0.5, mip));
                int mipHeight = (int)(size * Math.Pow(0.5, mip));

                frameBuffer.Resize(mipWidth, mipHeight);
                GLL.Viewport(0, 0, mipWidth, mipHeight);

                float roughness = (float)mip / (float)(cubemapOutput.MipCount - 1);
                cubemapFilter.SetFloat("roughness", roughness);

                for (int i = 0; i < 6; i++)
                {
                    //attach face to fbo as color attachment 0
                    if (cubemapOutput is GLTextureCubeArray)
                    {
                        GLL.FramebufferTextureLayer(FramebufferTarget.Framebuffer,
                             FramebufferAttachment.ColorAttachment0, cubemapOutput.ID, mip, (layer * 6) + i);
                    }
                    else
                    {
                        GLL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                                 FramebufferAttachment.ColorAttachment0,
                                  TextureTarget.TextureCubeMapPositiveX + i, cubemapOutput.ID, mip);
                    }

                    cubemapFilter.SetMatrix4x4("view", ref captureViews[i]);

                    GLL.ClearColor(0,0,0,1);
                    GLL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    RenderTools.DrawCube();
                }
            }

            var errorcheck = GLL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());

            GLL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GLL.Enable(EnableCap.CullFace);
            GLL.Enable(EnableCap.Blend);

            frameBuffer.Dispoe();
            frameBuffer.DisposeRenderBuffer();

            GLL.UseProgram(0);
        }
    }
}
