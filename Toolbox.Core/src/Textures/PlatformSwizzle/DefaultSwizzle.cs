using System;
using System.Collections.Generic;
using System.Text;

namespace Toolbox.Core.Imaging
{
    public class DefaultSwizzle : IPlatformSwizzle
    {
        public TexFormat OutputFormat { get; set; } = TexFormat.RGBA8_UNORM;

        public override string ToString() {
            return OutputFormat.ToString();
        }

        public bool IsOuputRGBA8 => false;

        public DefaultSwizzle() { }

        public DefaultSwizzle(TexFormat format) {
            OutputFormat = format;
        }

        public byte[] DecodeImage(STGenericTexture texture, byte[] data, uint width, uint height, int array, int mip) {
            var formatSize = texture.GetBytesPerPixel();
            uint offset = 0;
            for (byte d = 0; d < texture.Depth; ++d)
            {
                for (byte i = 0; i < texture.ArrayCount; ++i)
                {
                    for (int j = 0; j < texture.MipCount; ++j)
                    {
                        uint MipWidth = (uint)Math.Max(1, texture.Width >> j);
                        uint MipHeight = (uint)Math.Max(1, texture.Height >> j);

                        uint size = (MipWidth * MipHeight); //Total pixels
                        if (texture.IsBCNCompressed())
                        {
                            size = ((MipWidth + 3) >> 2) * ((MipHeight + 3) >> 2) * formatSize;
                            if (size < formatSize)
                                size = formatSize;
                        }
                        else
                        {
                            size = (uint)(size * formatSize); //Bytes per pixel
                        }

                        if (mip == j && array == i)
                            return ByteUtils.SubArray(data, (int)offset, (int)size);
                        offset += size;
                    }
                }
            }

            return data;
        }

        public byte[] EncodeImage(STGenericTexture texture, byte[] data, uint width, uint height, int array, int mip) {
            return null;
        }
    }
}
