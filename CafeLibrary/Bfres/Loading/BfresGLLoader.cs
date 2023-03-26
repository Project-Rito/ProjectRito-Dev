using System;
using System.Collections.Generic;
using System.Linq;
using GLFrameworkEngine;
using Nintendo.Bfres;
using Nintendo.Bfres.Helpers;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core.IO;

namespace CafeLibrary.Rendering
{
    public class BfresGLLoader
    {
        public static List<VaoAttribute> LoadAttributes(Model mdl, Shape shape, Material material)
        {
            var vertexBuffer = mdl.VertexBuffers[shape.VertexBufferIndex];

            List<VaoAttribute> attributes = new List<VaoAttribute>();

            int offset = 0;
            foreach (VertexAttrib att in vertexBuffer.Attributes.Values)
            {
                if (!ElementCountLookup.ContainsKey(att.Name.Remove(2)))
                    continue;

                bool assigned = false;
                int stride = 0;

                var assign = material.ShaderAssign;
                foreach (var matAttribute in assign.AttribAssigns)
                {
                    if (matAttribute.Value == att.Name)
                    {
                        //Get the translated attribute that is passed to the fragment shader
                        //Models can assign the same attribute to multiple uniforms (ie u0 to u1, u2)
                        string translated = matAttribute.Key;

                        VaoAttribute vaoAtt = new VaoAttribute();
                        vaoAtt.vertexAttributeName = att.Name;
                        vaoAtt.name = translated;
                        vaoAtt.ElementCount = ElementCountLookup[att.Name.Remove(2)];
                        vaoAtt.Assigned = assigned;
                        vaoAtt.Offset = offset;

                        if (att.Name.Contains("_i"))
                        {
                            vaoAtt.IntType = VertexAttribIntegerType.Int;
                            vaoAtt.IsFloat = false;
                        }
                        else
                        {
                            vaoAtt.FloatType = VertexAttribPointerType.Float;
                            vaoAtt.IsFloat = true;
                        }

                        attributes.Add(vaoAtt);

                        if (!assigned)
                        {
                            stride = vaoAtt.Stride;
                            assigned = true;
                        }
                    }
                }

                offset += stride;
            }

            return attributes;
        }

        public static byte[] LoadIndexBufferData(Shape shape)
        {
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                foreach (var mesh in shape.Meshes) {
                    var lodFaces = mesh.GetIndices().ToArray();
                    for (int i = 0; i < lodFaces.Length; i++)
                    {
                        switch (mesh.IndexFormat)
                        {
                            case Nintendo.Bfres.GX2.GX2IndexFormat.UInt16:
                            case Nintendo.Bfres.GX2.GX2IndexFormat.UInt16LittleEndian:
                                writer.Write((ushort)(lodFaces[i] + mesh.FirstVertex));
                                break;
                            default:
                                writer.Write((uint)(lodFaces[i] + mesh.FirstVertex));
                                break;
                        }
                    }
                }

            }
            return mem.ToArray();
        }

        public static byte[] LoadBufferData(BfresFile resFile, Model mdl, Shape shape, List<VaoAttribute> attributes)
        {
            //Create a buffer instance which stores all the buffer data
            VertexBufferHelper helper = new VertexBufferHelper(
                mdl.VertexBuffers[shape.VertexBufferIndex], resFile.Endian);

            //Fill a byte array of data
            int vertexCount = helper.Attributes.FirstOrDefault().Data.Length;

            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                var strideTotal = attributes.Sum(x => x.Stride);
                for (int i = 0; i < vertexCount; i++)
                {
                    foreach (var attribute in attributes)
                    {
                        var att = helper.Attributes.FirstOrDefault(x => x.Name == attribute.vertexAttributeName);
                        if (att == null)
                            continue;

                        writer.SeekBegin(attribute.Offset + (i * strideTotal));
                        for (int j = 0; j < attribute.ElementCount; j++)
                        {
                            if (attribute.vertexAttributeName == "_i0" && mdl.Skeleton.MatrixToBoneList.Count > (int)att.Data[i][j])
                                writer.Write((int)mdl.Skeleton.MatrixToBoneList[(int)att.Data[i][j]]);
                            else if (attribute.vertexAttributeName == "_p0")
                                writer.Write(att.Data[i][j] * GLContext.PreviewScale);
                            else
                                writer.Write(att.Data[i][j]);
                        }
                    }
                }
            }
            return mem.ToArray();
        }

        static Dictionary<string, int> ElementCountLookup = new Dictionary<string, int>()
        {
            { "_u", 2 },
            { "_p", 3 },
            { "_n", 3 },
            { "_t", 4 },
            { "_b", 4 },
            { "_c", 4 },
            { "_w", 4 },
            { "_i", 4 },
        };

        public class VaoAttribute
        {
            public string name;
            public string vertexAttributeName;
            public VertexAttribPointerType FloatType;
            public VertexAttribIntegerType IntType;
            public bool IsFloat;
            public int ElementCount;

            public int Offset;

            public bool Assigned;

            public int Stride
            {
                get { return Assigned ? 0 : ElementCount * FormatSize(); }
            }

            public string UniformName
            {
                get
                {
                    switch (name)
                    {
                        case "_p0": return GLConstants.VPosition;
                        case "_n0": return GLConstants.VNormal;
                        case "_w0": return GLConstants.VBoneWeight;
                        case "_i0": return GLConstants.vBoneIndex;
                        case "_u0": return GLConstants.VTexCoord0;
                        case "_u1": return GLConstants.VTexCoord1;
                        case "_u2": return GLConstants.VTexCoord2;
                        case "_u3": return GLConstants.VTexCoord3;
                        case "_c0": return GLConstants.VColor;
                        case "_t0": return GLConstants.VTangent;
                        case "_b0": return GLConstants.VBitangent;
                        default:
                            return name;
                    }
                }
            }

            private int FormatSize()
            {
                if (IsFloat)
                {
                    switch (FloatType)
                    {
                        case VertexAttribPointerType.Byte:
                        case VertexAttribPointerType.UnsignedByte:
                            return 1;
                        case VertexAttribPointerType.HalfFloat:
                        case VertexAttribPointerType.Short:
                            return 2;
                        case VertexAttribPointerType.Float:
                        case VertexAttribPointerType.Int:
                        case VertexAttribPointerType.UnsignedInt:
                            return 4;
                        default:
                            throw new Exception($"Could not set format stride. Format not supported! {FloatType}");
                    }
                }
                else
                {
                    switch (IntType)
                    {
                        case VertexAttribIntegerType.Byte:
                        case VertexAttribIntegerType.UnsignedByte:
                            return 1;
                        case VertexAttribIntegerType.Short:
                            return 2;
                        case VertexAttribIntegerType.Int:
                        case VertexAttribIntegerType.UnsignedInt:
                            return 4;
                        default:
                            throw new Exception($"Could not set format stride. Format not supported! {IntType}");
                    }
                }
            }
        }
    }
}
