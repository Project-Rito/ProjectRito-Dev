using System;
using System.Collections.Generic;
using System.Linq;
using Nintendo.Bfres.Helpers;
using Nintendo.Bfres;
using Nintendo.Bfres.GX2;
using System.IO;
using Syroot.Maths;
using Toolbox.Core.IO;
using Toolbox.Core;
using Nintendo.Bfres.TextConvert;

namespace CafeLibrary.ModelConversion
{
    public class BfresModelImporter
    {
        public static Model ImportModel(CommandListHandler cmdList, BfresFile resFile, string dir)
        {
            STGenericScene scene = new STGenericScene();
            if (File.Exists($"{dir}.dae"))
                scene = Toolbox.Core.Collada.DAE.Read($"{dir}.dae", new Toolbox.Core.Collada.DAE.ImportSettings()
                {
                    RemoveDuplicateVerts = true,
                });

            var fmdl = ConvertScene(cmdList, resFile, dir, scene);
            fmdl.Name = new DirectoryInfo(dir).Name;
            return fmdl;
        }

        static Model ConvertScene(CommandListHandler cmdList, BfresFile resFile, string dir, STGenericScene scene)
        {
            var model = scene.Models.FirstOrDefault();
            model.Skeleton.Reset();

            Dictionary<string, MaterialConvert.MeshMetaInfo[]> meshMatInfo = new Dictionary<string, MaterialConvert.MeshMetaInfo[]>();

            Model fmdl = new Model();
            fmdl.Name = model.Name;

            foreach (var file in Directory.GetFiles($"{dir}/Materials"))
            {
                Material fmat = MaterialConvert.FromJson(File.ReadAllText("Default.fmat.json"));
                var meshMetaList = MaterialConvert.FromJson(fmat, File.ReadAllText(file));

                if (meshMetaList.Count > 0)
                    meshMatInfo.Add(fmat.Name, meshMetaList.ToArray());

                fmat.RenderState = new RenderState();
                fmdl.Materials.Add(fmat.Name, fmat);
            }

            fmdl.Skeleton = new Skeleton();
            fmdl.Skeleton.FlagsRotation = SkeletonFlagsRotation.EulerXYZ;

            foreach (var bone in model.Skeleton.Bones)
            {
                Vector4F rotation = new Vector4F(
                    bone.EulerRotation.X,
                    bone.EulerRotation.Y,
                    bone.EulerRotation.Z,
                    1.0f);

                //Convert to quaternion if setting is used
                if (fmdl.Skeleton.FlagsRotation == SkeletonFlagsRotation.Quaternion)
                {
                    var quat = STMath.FromEulerAngles(bone.EulerRotation);
                    rotation = new Vector4F(quat.X, quat.Y, quat.Z, quat.W);
                }

                var bfresBone = new Nintendo.Bfres.Bone()
                {
                    FlagsRotation = BoneFlagsRotation.EulerXYZ,
                    FlagsTransform = SetBoneFlags(bone),
                    Name = bone.Name,
                    RigidMatrixIndex = -1,  //Gets calculated after
                    SmoothMatrixIndex = -1, //Gets calculated after
                    ParentIndex = (short)bone.ParentIndex,
                    Position = new Vector3F(
                         bone.Position.X,
                         bone.Position.Y,
                         bone.Position.Z),
                    Scale = new Vector3F(
                         bone.Scale.X,
                         bone.Scale.Y,
                         bone.Scale.Z),
                    Rotation = rotation,
                    Visible = true,
                };
                fmdl.Skeleton.Bones.Add(bone.Name, bfresBone);
            }

            List<int> smoothSkinningIndices = new List<int>();
            List<int> rigidSkinningIndices = new List<int>();

            //Determine the rigid and smooth bone skinning
            foreach (var mesh in model.Meshes)
            {
                //Set the skin count
                mesh.VertexSkinCount = CalculateSkinCount(mesh.Vertices);

                int materialIndex = mesh.PolygonGroups[0].MaterialIndex;
                if (materialIndex == -1)
                    materialIndex = 0;

                //Get the original material and map by string key
                string material = model.Materials[materialIndex].Name;
                if (meshMatInfo.ContainsKey(material))
                {
                    var meshMeta = meshMatInfo[material][0];

                    //Map out the original mesh attribute data attached to the material.
                    if (!meshMatInfo[material].Any(x => x.SkinCount == mesh.VertexSkinCount))
                        mesh.VertexSkinCount = (uint)meshMeta.SkinCount;
                }

                foreach (var vertex in mesh.Vertices)
                {
                    foreach (var bone in vertex.BoneNames)
                    {
                        var bn = fmdl.Skeleton.Bones.Values.Where(x => x.Name == bone).FirstOrDefault();
                        if (bn != null)
                        {
                            int index = fmdl.Skeleton.Bones.IndexOf(bn);

                            //Rigid skinning
                            if (mesh.VertexSkinCount == 1)
                            {
                                if (!rigidSkinningIndices.Contains(index))
                                    rigidSkinningIndices.Add(index);
                            }
                            else
                            {
                                if (!smoothSkinningIndices.Contains(index))
                                    smoothSkinningIndices.Add(index);
                            }
                        }
                    }
                }
            }

            //Sort indices
            smoothSkinningIndices.Sort();
            rigidSkinningIndices.Sort();

            //Create a global skinning list. Smooth indices first, rigid indices last
            List<int> skinningIndices = new List<int>();
            skinningIndices.AddRange(smoothSkinningIndices);
            skinningIndices.AddRange(rigidSkinningIndices);

            //Next update the bone's skinning index value
            foreach (var index in smoothSkinningIndices)
            {
                var bone = fmdl.Skeleton.Bones[index];
                bone.SmoothMatrixIndex = (short)smoothSkinningIndices.IndexOf(index);
            }   
            //Rigid indices go after smooth indices
            //Here we do not index the global iist as the global list can include the same index in both smooth/rigid
            foreach (var index in rigidSkinningIndices)
            {
                var bone = fmdl.Skeleton.Bones[index];
                bone.RigidMatrixIndex = (short)(smoothSkinningIndices.Count + rigidSkinningIndices.IndexOf(index));
            }

            //Turn them into ushorts for the final list in the binary
            fmdl.Skeleton.MatrixToBoneList = new List<ushort>();
            for (int i = 0; i < skinningIndices.Count; i++)
                fmdl.Skeleton.MatrixToBoneList.Add((ushort)skinningIndices[i]);

            //Generate inverse matrices
            fmdl.Skeleton.InverseModelMatrices = new List<Matrix3x4>();
            foreach (var bone in fmdl.Skeleton.Bones.Values)
            {
                //Set identity types for none smooth bones
                bone.InverseMatrix = new Matrix3x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0);

                //Inverse matrices are used for smooth bone skinning
                if (bone.SmoothMatrixIndex == -1)
                    continue;

                var transform = MatrixExenstion.GetMatrixInverted(fmdl.Skeleton, bone);
                //Assign the inverse matrix directly for older versions that store it directly
                bone.InverseMatrix = transform;
                //Add it to the global inverse list
                fmdl.Skeleton.InverseModelMatrices.Add(transform);
            }

            foreach (var mesh in model.Meshes)
            {
                if (mesh.Vertices.Count == 0)
                    continue;

                var settings = new MeshSettings()
                {
                    UseBoneIndices = true,
                    UseBoneWeights = true,
                    UseNormal = true,
                    UseTangents = false,
                    UseTexCoord = new bool[5] { true, true, true, true, true, },
                    UseColor = new bool[5] { true, true, true, true, true, },
                };

                var names = fmdl.Shapes.Values.Select(x => x.Name).ToList();

                Shape fshp = new Shape();
                fshp.Name = Utils.RenameDuplicateString(mesh.Name, names, 0, 2);
                fshp.MaterialIndex = 0;
                fshp.BoneIndex = 0;

                fshp.SkinBoneIndices = new List<ushort>();
                foreach (var vertex in mesh.Vertices)
                {
                    foreach (var bone in vertex.BoneNames)
                    {
                        var bn = fmdl.Skeleton.Bones.Values.Where(x => x.Name == bone).FirstOrDefault();
                        if (bn != null)
                        {
                            ushort index = (ushort)fmdl.Skeleton.Bones.IndexOf(bn);
                            if (!fshp.SkinBoneIndices.Contains(index))
                                fshp.SkinBoneIndices.Add(index);
                        }
                    }
                }

                //Get the original material and map by string key
                int matIndex = mesh.PolygonGroups[0].MaterialIndex;
                if (matIndex == -1)
                    matIndex = 0;

                string material = model.Materials[matIndex].Name;
                if (mesh.PolygonGroups[0].Material != null)
                    material = mesh.PolygonGroups[0].Material.Name;

                int materialIndex = fmdl.Materials.IndexOf(material);
                if (materialIndex != -1)
                    fshp.MaterialIndex = (ushort)materialIndex;
                else
                    Console.WriteLine($"Failed to find material {material}");

                //Meshes and materials often share necessary shader information.
                //If attribute data, skin counts, etc is off, then shaders can become invalid.
                if (meshMatInfo.ContainsKey(material) && cmdList.use_mesh_meta_info)
                {
                    var meshMeta = meshMatInfo[material][0];

                    //Map out the original mesh attribute data attached to the material.
                    settings.UseNormal = meshMeta.Attributes.ContainsKey("_n0");
                    settings.UseTangents = meshMeta.Attributes.ContainsKey("_t0");
                    settings.UseBitangents = meshMeta.Attributes.ContainsKey("_b0");
                    settings.UseBoneWeights = meshMeta.Attributes.ContainsKey("_w0");
                    settings.UseBoneIndices = meshMeta.Attributes.ContainsKey("_i0");

                    for (int i = 0; i < 5; i++)
                        settings.UseTexCoord[i] = meshMeta.Attributes.ContainsKey($"_u{i}");
                    for (int i = 0; i < 5; i++)
                        settings.UseColor[i] = meshMeta.Attributes.ContainsKey($"_c{i}");

                    foreach (var att in meshMeta.Attributes)
                    {
                        var format = (GX2AttribFormat)Enum.Parse(typeof(GX2AttribFormat), att.Value.Format);
                        switch (att.Key)
                        {
                            case "_p0": settings.PositionFormat = format; break;
                            case "_n0": settings.NormalFormat = format; break;
                            case "_t0": settings.TangentFormat = format; break;
                            case "_b0": settings.BitangentFormat = format; break;
                            case "_u0": settings.TexCoordFormat = format; break;
                            case "_c0": settings.ColorFormat = format; break;
                            case "_w0": settings.BoneWeightsFormat = format; break;
                            case "_i0": settings.BoneIndicesFormat = format; break;
                        }
                    }
                }

                //Only add a root node for bounding node tree
                fshp.SubMeshBoundingIndices.Add(0);
                fshp.SubMeshBoundingNodes.Add(new BoundingNode()
                {
                    LeftChildIndex = 0,
                    RightChildIndex = 0,
                    NextSibling = 0,
                    SubMeshIndex = 0,
                    Unknown = 0,
                    SubMeshCount = 1,
                });

                try
                {
                    //Generate a vertex buffer
                    VertexBuffer buffer = GenerateVertexBuffer(resFile, mesh,
                        settings, model.Skeleton, fmdl.Skeleton, rigidSkinningIndices, smoothSkinningIndices);

                    fshp.VertexBufferIndex = (ushort)fmdl.VertexBuffers.Count;
                    fshp.VertexSkinCount = (byte)buffer.VertexSkinCount;
                    fmdl.VertexBuffers.Add(buffer);

                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to generate vertex buffer! \n " + ex.ToString());
                }

                //Generate boundings for the mesh
                //If a mesh's vertex data is split into parts, we can create sub meshes with their own boundings
                //Sub meshes are used for large meshes where they need to cull parts of a mesh from it's bounding box
                //Sub meshes do not allow multiple materials, that is from the shape itself
                //These are added in the order of the index list, with an offset/count for the indices being used as a sub mesh
                var boundingBox = CalculateBoundingBox(mesh.Vertices, model.Skeleton, mesh.VertexSkinCount > 0);
                fshp.SubMeshBoundings.Add(boundingBox); //Create bounding for total mesh
                fshp.SubMeshBoundings.Add(boundingBox); //Create bounding for single sub meshes

                Vector3F min = boundingBox.Center - boundingBox.Extent;
                Vector3F max = boundingBox.Center + boundingBox.Extent;

                var sphereRadius = GetBoundingSphereFromRegion(new Vector4F(
                    min.X, min.Y, min.Z, 1), new Vector4F(max.X, max.Y, max.Z, 1));

                fshp.RadiusArray.Add(sphereRadius); //Total radius (per LOD)

                Console.WriteLine($"BOUDNING {sphereRadius}");

                //A mesh represents a single level of detail. Here we can create additional level of detail meshes if supported.
                Mesh bMesh = new Mesh();
                bMesh.PrimitiveType = GX2PrimitiveType.Triangles;

                GX2IndexFormat Format = GX2IndexFormat.UInt16;
                if (resFile.IsPlatformSwitch)
                {
                    Format = GX2IndexFormat.UInt16LittleEndian;
                    if (mesh.Faces.Any(x => x > ushort.MaxValue))
                        Format = GX2IndexFormat.UInt32LittleEndian;
                }
                else
                {
                    if (mesh.Faces.Any(x => x > ushort.MaxValue))
                        Format = GX2IndexFormat.UInt32;
                }

                bMesh.SetIndices(mesh.Faces, Format);
                bMesh.SubMeshes.Add(new SubMesh()
                {
                    Offset = 0,
                    Count = (uint)mesh.Faces.Count,
                });
                //Add the lod to the shape
                fshp.Meshes.Add(bMesh);

                //Finally add the shape to the model
                fmdl.Shapes.Add(fshp.Name, fshp);
            }

            return fmdl;
        }

        private static float CalculateRadius(float horizontalLeg, float verticalLeg)
        {
            return (float)Math.Sqrt((horizontalLeg * horizontalLeg) + (verticalLeg * verticalLeg));
        }

        private static float GetBoundingSphereFromRegion(Vector4F min, Vector4F max)
        {
            // The radius should be the hypotenuse of the triangle.
            // This ensures the sphere contains all points.
            Vector4F lengths = max - min;
            return CalculateRadius(lengths.X / 2.0f, lengths.Y / 2.0f);
        }

        private static BoneFlagsTransform SetBoneFlags(STBone bn)
        {
            BoneFlagsTransform flags = BoneFlagsTransform.None;
            if (bn.Position == OpenTK.Vector3.Zero)
                flags |= BoneFlagsTransform.TranslateZero;
            if (bn.EulerRotation == OpenTK.Vector3.Zero)
                flags |= BoneFlagsTransform.RotateZero;
            if (bn.Scale == OpenTK.Vector3.One)
                flags |= BoneFlagsTransform.ScaleOne;
            return flags;
        }

        private static byte CalculateSkinCount(List<STVertex> vertices)
        {
            uint numSkinning = 0;
            for (int v = 0; v < vertices.Count; v++)
                numSkinning = Math.Max(numSkinning, (uint)vertices[v].BoneNames.Count);
            return (byte)numSkinning;
        }

        private static Dictionary<string, AABB> CalculateBoneAABB(List<STVertex> vertices, STSkeleton skeleton)
        {
            Dictionary<string, AABB> skinnedBoundings = new Dictionary<string, AABB>();
            for (int i = 0; i < vertices.Count; i++) {
                var vertex = vertices[i];
                
                foreach (var boneID in vertex.BoneNames)
                {
                    if (!skinnedBoundings.ContainsKey(boneID))
                        skinnedBoundings.Add(boneID, new AABB());

                    var transform = skeleton.Bones.FirstOrDefault(x => x.Name == boneID).Transform;
                    var inverted = transform.Inverted();

                    //Get the position in local coordinates
                    var position = vertices[i].Position;
                    position = OpenTK.Vector3.TransformPosition(position, inverted);

                    var bounding = skinnedBoundings[boneID];
                    bounding.minX = Math.Min(bounding.minX, position.X);
                    bounding.minY = Math.Min(bounding.minY, position.Y);
                    bounding.minZ = Math.Min(bounding.minZ, position.Z);
                    bounding.maxX = Math.Max(bounding.maxX, position.X);
                    bounding.maxY = Math.Max(bounding.maxY, position.Y);
                    bounding.maxZ = Math.Max(bounding.maxZ, position.Z);
                }
            }
            return skinnedBoundings;
        }

        class AABB
        {
            public float minX = float.MaxValue;
            public float minY = float.MaxValue;
            public float minZ = float.MaxValue;
            public float maxX = float.MinValue;
            public float maxY = float.MinValue;
            public float maxZ = float.MinValue;

            public OpenTK.Vector3 Max => new OpenTK.Vector3(maxX, maxY, maxZ);
            public OpenTK.Vector3 Min => new OpenTK.Vector3(minX, minY, minZ);

        }

        private static Bounding CalculateBoundingBox(List<STVertex> vertices, STSkeleton skeleton, bool isSmoothSkinning)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            if (isSmoothSkinning)
            {
                var boundings = CalculateBoneAABB(vertices, skeleton);
                //Find largest bounding box
                foreach (var bounding in boundings.Values)
                {
                    minX = Math.Min(minX, bounding.minX);
                    minY = Math.Min(minY, bounding.minY);
                    minZ = Math.Min(minZ, bounding.minZ);
                    maxX = Math.Max(maxX, bounding.maxX);
                    maxY = Math.Max(maxY, bounding.maxY);
                    maxZ = Math.Max(maxZ, bounding.maxZ);
                }
                return CalculateBoundingBox(
                    new Vector3F(minX, minY, minZ),
                    new Vector3F(maxX, maxY, maxZ));
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                minX = Math.Min(minX, vertices[i].Position.X);
                minY = Math.Min(minY, vertices[i].Position.Y);
                minZ = Math.Min(minZ, vertices[i].Position.Z);
                maxX = Math.Max(maxX, vertices[i].Position.X);
                maxY = Math.Max(maxY, vertices[i].Position.Y);
                maxZ = Math.Max(maxZ, vertices[i].Position.Z);
            }

            return CalculateBoundingBox(
                new Vector3F(minX, minY, minZ),
                new Vector3F(maxX, maxY, maxZ));
        }

        private static Bounding CalculateBoundingBox(Vector3F min, Vector3F max)
        {
            Vector3F center = max + min;

            Console.WriteLine($"min {min}");
            Console.WriteLine($"max {max}");

            float xxMax = GetExtent(max.X, min.X);
            float yyMax = GetExtent(max.Y, min.Y);
            float zzMax = GetExtent(max.Z, min.Z);

            Vector3F extend = new Vector3F(xxMax, yyMax, zzMax);

            return new Bounding()
            {
                Center = new Vector3F(center.X, center.Y, center.Z),
                Extent = new Vector3F(extend.X, extend.Y, extend.Z),
            };
        }



        private static float GetExtent(float max, float min)
        {
            return (float)Math.Max(Math.Sqrt(max * max), Math.Sqrt(min * min));
        }

        private static VertexBuffer GenerateVertexBuffer(BfresFile resFile, STGenericMesh mesh, MeshSettings settings,
           STSkeleton skeleton, Skeleton fskl, List<int> rigidIndices, List<int> smoothIndices)
        {
            if (settings.UseTangents || settings.UseBitangents)
                mesh.CalculateTangentBitangent(0);

            VertexBufferHelper vertexBufferHelper = new VertexBufferHelper(
                new VertexBuffer(), resFile.Endian);

            List<Vector4F> Positions = new List<Vector4F>();
            List<Vector4F> Normals = new List<Vector4F>();
            List<Vector4F> BoneWeights = new List<Vector4F>();
            List<Vector4F> BoneIndices = new List<Vector4F>();
            List<Vector4F> Tangents = new List<Vector4F>();
            List<Vector4F> Bitangents = new List<Vector4F>();

            int numTexCoords = mesh.Vertices.FirstOrDefault().TexCoords.Length;
            int numColors = mesh.Vertices.FirstOrDefault().Colors.Length;

            Vector4F[][] TexCoords = new Vector4F[numTexCoords][];
            Vector4F[][] Colors = new Vector4F[numColors][];

            for (int c = 0; c < numColors; c++)
                Colors[c] = new Vector4F[mesh.Vertices.Count];
            for (int c = 0; c < numTexCoords; c++)
                TexCoords[c] = new Vector4F[mesh.Vertices.Count];

            for (int v = 0; v < mesh.Vertices.Count; v++)
            {
                var vertex = mesh.Vertices[v];

                var position = vertex.Position;
                var normal = vertex.Normal;

                //Reset rigid skinning types to local space
                if (mesh.VertexSkinCount == 0)
                {
                    var transform = skeleton.Bones[mesh.BoneIndex].Transform;
                    var inverted = transform.Inverted();
                    position = OpenTK.Vector3.TransformPosition(position, inverted);
                    normal = OpenTK.Vector3.TransformNormal(normal, inverted);
                }
                //Reset rigid skinning types to local space
                if (mesh.VertexSkinCount == 1)
                {
                    var bone = skeleton.Bones.FirstOrDefault(x => x.Name == vertex.BoneNames[0]);
                    var transform = bone.Transform;
                    var inverted = transform.Inverted();
                    position = OpenTK.Vector3.TransformPosition(position, inverted);
                    normal = OpenTK.Vector3.TransformNormal(normal, inverted);
                }

                Positions.Add(new Vector4F(
                    position.X,
                    position.Y,
                    position.Z, 0));

                Normals.Add(new Vector4F(
                    normal.X,
                    normal.Y,
                    normal.Z, 0));

                if (settings.UseTangents)
                {
                    Tangents.Add(new Vector4F(
                        vertex.Tangent.X,
                        vertex.Tangent.Y,
                        vertex.Tangent.Z, 0));
                }

                if (settings.UseBitangents)
                {
                    Bitangents.Add(new Vector4F(
                        vertex.Bitangent.X,
                        vertex.Bitangent.Y,
                        vertex.Bitangent.Z, 0));
                }

                for (int i = 0; i < vertex.TexCoords?.Length; i++)
                {
                    TexCoords[i][v] = new Vector4F(
                        vertex.TexCoords[i].X,
                        vertex.TexCoords[i].Y,
                        0, 0);
                }

                for (int i = 0; i < vertex.Colors?.Length; i++)
                {
                    Colors[i][v] = new Vector4F(
                        vertex.Colors[i].X,
                        vertex.Colors[i].Y,
                        vertex.Colors[i].Z,
                        vertex.Colors[i].W);
                }

                int[] indices = new int[4];
                float[] weights = new float[4];
                for (int j = 0; j < vertex.BoneWeights?.Count; j++)
                {
                    int index = Array.FindIndex(fskl.Bones.Values.ToArray(), x => x.Name == vertex.BoneNames[j]);
                    if (index == -1)
                        continue;

                    //Check for the index in the proper skinning index lists
                    if (mesh.VertexSkinCount > 1)
                    {
                        indices[j] = smoothIndices.IndexOf(index);
                        weights[j] = vertex.BoneWeights[j];
                    }
                    else
                    {
                        //Rigid indices start after smooth indices in the global index list
                        //Smooth indices can have the same bone index as a rigid one, so it's best to index the specific list
                        indices[j] = smoothIndices.Count + rigidIndices.IndexOf(index);
                        weights[j] = vertex.BoneWeights[j];
                    }
                }

                if (vertex.BoneWeights?.Count > 0 && settings.UseBoneIndices && mesh.VertexSkinCount > 0)
                {
                    BoneWeights.Add(new Vector4F(weights[0], weights[1], weights[2], weights[3]));
                    BoneIndices.Add(new Vector4F(indices[0], indices[1], indices[2], indices[3]));
                }
            }

            List<VertexBufferHelperAttrib> attributes = new List<VertexBufferHelperAttrib>();
            attributes.Add(new VertexBufferHelperAttrib()
            {
                Name = "_p0",
                Data = Positions.ToArray(),
                Format = settings.PositionFormat,
            });

            if (Normals.Count > 0)
            {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_n0",
                    Data = Normals.ToArray(),
                    Format = settings.NormalFormat,
                });
            }

            if (Tangents.Count > 0)
            {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_t0",
                    Data = Tangents.ToArray(),
                    Format = settings.TangentFormat,
                });
            }

            if (Bitangents.Count > 0)
            {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_b0",
                    Data = Bitangents.ToArray(),
                    Format = settings.BitangentFormat,
                });
            }

            for (int i = 0; i < TexCoords.Length; i++)
            {
                if (settings.UseTexCoord[i])
                {
                    attributes.Add(new VertexBufferHelperAttrib()
                    {
                        Name = $"_u{i}",
                        Data = TexCoords[i],
                        Format = settings.TexCoordFormat,
                    });
                }
            }

            for (int i = 0; i < Colors.Length; i++)
            {
                if (settings.UseColor[i])
                {
                    attributes.Add(new VertexBufferHelperAttrib()
                    {
                        Name = $"_c{i}",
                        Data = Colors[i],
                        Format = settings.ColorFormat,
                    });
                }
            }

            if (BoneIndices.Count > 0)
            {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_i0",
                    Data = BoneIndices.ToArray(),
                    Format = settings.BoneIndicesFormat,
                });
            }

            if (BoneWeights.Count > 0)
            {
                attributes.Add(new VertexBufferHelperAttrib()
                {
                    Name = "_w0",
                    Data = BoneWeights.ToArray(),
                    Format = settings.BoneWeightsFormat,
                });
            }

            vertexBufferHelper.Attributes = attributes;
            var buffer = vertexBufferHelper.ToVertexBuffer();
            buffer.VertexSkinCount = (byte)mesh.VertexSkinCount;
            return buffer;
        }

        public class MeshSettings
        {
            public bool UseNormal { get; set; }
            public bool[] UseTexCoord { get; set; }
            public bool[] UseColor { get; set; }
            public bool UseBoneWeights { get; set; }
            public bool UseBoneIndices { get; set; }
            public bool UseTangents { get; set; }
            public bool UseBitangents { get; set; }

            public GX2AttribFormat PositionFormat = GX2AttribFormat.Format_32_32_32_Single;
            public GX2AttribFormat NormalFormat = GX2AttribFormat.Format_10_10_10_2_SNorm;
            public GX2AttribFormat TexCoordFormat = GX2AttribFormat.Format_16_16_Single;
            public GX2AttribFormat ColorFormat = GX2AttribFormat.Format_16_16_16_16_Single;
            public GX2AttribFormat TangentFormat = GX2AttribFormat.Format_8_8_8_8_SNorm;
            public GX2AttribFormat BitangentFormat = GX2AttribFormat.Format_8_8_8_8_SNorm;

            public GX2AttribFormat BoneIndicesFormat = GX2AttribFormat.Format_8_8_8_8_UInt;
            public GX2AttribFormat BoneWeightsFormat = GX2AttribFormat.Format_8_8_8_8_UNorm;
        }
    }
}
