using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Toolbox.Core
{
    public class Uncompressed : ICompressionFormat
    {
        public string[] Description { get; set; } = new string[] { "Uncompressed" };
        public string[] Extension { get; set; } = new string[] { "*.*" };

        public override string ToString() { return "Uncompressed"; }

        public bool Identify(Stream stream, string fileName)
        {
            return false; // Never identify a file as this. Let it be assigned manually when nothing else matches.
        }

        public bool CanCompress { get; } = true;

        public Stream Decompress(Stream stream)
        {
            long streamStartPos = stream.Position;

            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = streamStartPos;

            return ms; // Why are we loading this into memory? To ensure that modifications are not applied to the source stream.
        }

        public Stream Compress(Stream stream)
        {
            return stream;
        }
    }
}
