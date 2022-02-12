using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace MapStudio.UI
{
    public enum TextureCoordinate
    {
        S = TextureParameterName.TextureWrapS,
        T = TextureParameterName.TextureWrapT,
        R = TextureParameterName.TextureWrapR
    }

    class Texture : IDisposable
    {
        public const SizedInternalFormat Srgb8Alpha8 = (SizedInternalFormat)All.Srgb8Alpha8;
        public const SizedInternalFormat RGB32F = (SizedInternalFormat)All.Rgb32f;

        public const GetPName MAX_TEXTURE_MAX_ANISOTROPY = (GetPName)0x84FF;

        public static readonly float MaxAniso;

        static Texture()
        {
            MaxAniso = GLH.GetFloat(MAX_TEXTURE_MAX_ANISOTROPY);
        }

        public readonly string Name;
        public readonly int GLTexture;
        public readonly int Width, Height;
        public readonly int MipmapLevels;
        public readonly SizedInternalFormat InternalFormat;

        public Texture(string name, System.Drawing.Bitmap image, bool generateMipmaps, bool srgb)
        {
            Name = name;
            Width = image.Width;
            Height = image.Height;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;

            if (generateMipmaps)
            {
                // Calculate how many levels to generate for this texture
                MipmapLevels = (int)Math.Floor(Math.Log(Math.Max(Width, Height), 2));
            }
            else
            {
                // There is only one level
                MipmapLevels = 1;
            }

            Util.CheckGLError("Clear");

            Util.CreateTexture(TextureTarget.Texture2D, Name, out GLTexture);

            GLH.BindTexture(TextureTarget.Texture2D, GLTexture);
            GLH.TexStorage2D(TextureTarget2d.Texture2D, MipmapLevels, InternalFormat, Width, Height);
            Util.CheckGLError("Storage2d");

            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, Width, Height),
                ImageLockMode.ReadOnly, global::System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            GLH.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            Util.CheckGLError("SubImage");

            image.UnlockBits(data);
            image.Dispose();

            if (generateMipmaps) GLH.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GLH.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, MipmapLevels - 1);

            SetWrap(TextureCoordinate.S, TextureWrapMode.Repeat);
            SetWrap(TextureCoordinate.T, TextureWrapMode.Repeat);

            SetMinFilter(generateMipmaps ? TextureMinFilter.Linear : TextureMinFilter.LinearMipmapLinear);
            SetMagFilter(TextureMagFilter.Linear);
        }

        public Texture(string name, int GLTex, int width, int height, int mipmaplevels, SizedInternalFormat internalFormat)
        {
            Name = name;
            GLTexture = GLTex;
            Width = width;
            Height = height;
            MipmapLevels = mipmaplevels;
            InternalFormat = internalFormat;
        }

        public Texture(string name, int width, int height, IntPtr data, bool generateMipmaps = false, bool srgb = false)
        {
            Name = name;
            Width = width;
            Height = height;
            InternalFormat = srgb ? Srgb8Alpha8 : SizedInternalFormat.Rgba8;
            MipmapLevels = generateMipmaps == false ? 1 : (int)Math.Floor(Math.Log(Math.Max(Width, Height), 2));

            Util.CreateTexture(TextureTarget.Texture2D, Name, out GLTexture);
            GLH.BindTexture(TextureTarget.Texture2D, GLTexture);
            GLH.TexStorage2D(TextureTarget2d.Texture2D, MipmapLevels, InternalFormat, Width, Height);

            GLH.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, data);

            if (generateMipmaps) GLH.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GLH.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, MipmapLevels - 1);

            SetWrap(TextureCoordinate.S, TextureWrapMode.Repeat);
            SetWrap(TextureCoordinate.T, TextureWrapMode.Repeat);
        }

        public void SetMinFilter(TextureMinFilter filter)
        {
            GLH.BindTexture(TextureTarget.Texture2D, GLTexture);
            GLH.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
            GLH.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void SetMagFilter(TextureMagFilter filter)
        {
            GLH.BindTexture(TextureTarget.Texture2D, GLTexture);
            GLH.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
            GLH.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void SetAnisotropy(float level)
        {
            const TextureParameterName TEXTURE_MAX_ANISOTROPY = (TextureParameterName)0x84FE;

            GLH.BindTexture(TextureTarget.Texture2D, GLTexture);
            GLH.TexParameter(TextureTarget.Texture2D, TEXTURE_MAX_ANISOTROPY, Util.Clamp(level, 1, MaxAniso));
            GLH.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void SetLod(int @base, int min, int max)
        {
            GLH.BindTexture(TextureTarget.Texture2D, GLTexture);
            GLH.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, @base);
            GLH.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, min);
            GLH.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, max);
            GLH.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void SetWrap(TextureCoordinate coord, TextureWrapMode mode)
        {
            GLH.BindTexture(TextureTarget.Texture2D, GLTexture);
            GLH.TexParameter(TextureTarget.Texture2D, (TextureParameterName)coord, (int)mode);
            GLH.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Dispose()
        {
            GLH.DeleteTexture(GLTexture);
        }
    }
}
