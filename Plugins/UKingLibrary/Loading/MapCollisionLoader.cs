using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using HKX2;
using HKX2Builders;
using HKX2Builders.Extensions;
using GLFrameworkEngine;
using ImGuiNET;

/*
 THE STORY OF THE BORED PROGRAMMER

    Once upon a time there was some guy. He found himself occupying his time by writing code that did stuff. Rendering code was by far his favorite to write - it felt very rewarding to see his efforts appear on-screen.
But as a maintainer of any project should, he busied himself with writing all sorts of stuff for the entire codebase. Oftentimes he would go to school thinking about implementation and come home to apply his thoughts
from throughout the day. It was on one of these days that he decided to try something different - he would write a story. He thought long and hard for a duration of about fifteen seconds, then settled on writing about himself.
Any respectable programmer knows to keep their ego in good health. It isn't often that they get the opportunity to feel valued by society.
    He knew that collision work would be tedious and time consuming, even with the advice offered by colleagues. But so many people were anticipating his work that he felt that he could not quit. Instead, he put on some Juice WRLD
and got to work.
 */

namespace UKingLibrary
{
    public class MapCollisionLoader
    {
        public NodeBase RootNode;

        private string Prefix;

        private HKXHeader Header = HKXHeader.BotwWiiu(); // TODO - actually get the right platform
        private hkRootLevelContainer Root;
        private StaticCompoundInfo StaticCompound;

        private List<ActorShapePairing> ShapePairings;

        private STFileLoader.Settings FileSettings;

        public void Load(Stream stream, string fileName, GLScene scene = null)
        {
            FileSettings = STFileLoader.TryDecompressFile(stream, fileName);

            List<IHavokObject> roots = Util.ReadBotwHKX(FileSettings.Stream.ReadAllBytes(), ".hksc");

            StaticCompound = (StaticCompoundInfo)roots[0];
            Root = (hkRootLevelContainer)roots[1];

            RootNode = new NodeFolder(fileName)
            {
                Tag = this,
                HasCheckBox = true,
                IsChecked = true,
                OnChecked = (object value, EventArgs args) =>
                {
                    IsVisible = (bool)value;
                }
            };

            Prefix = Path.GetFileNameWithoutExtension(fileName);

            ShapePairings = GenerateActorShapePairings();

            UpdateRenders(scene); // Rendering functionality
        }

        #region Interfacing for collision manipulation
        public BakedCollisionShapeCacheable[] GetCacheables(uint hashId)
        {
            // TODO: In future do a binary search like https://github.com/krenyy/botw_havok/blob/dc7966c7780ef8c8a35e061cd3aacc20020fa2d7/botw_havok/cli/hkrb_extract.py#L30
            foreach (ActorShapePairing pairing in ShapePairings)
            {
                if (pairing.ActorInfo?.m_HashId == hashId)
                {
                    BakedCollisionShapeCacheable[] cacheables = new BakedCollisionShapeCacheable[pairing.Shapes.Count];

                    for (int i = 0; i < pairing.Shapes.Count; i++)
                    {
                        ShapeInfoShapeInstancePairing shape = pairing.Shapes[i];
                        hkpRigidBody rigidBody = ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[shape.SystemIndex].m_rigidBodies[shape.RigidBodyIndex];

                        BakedCollisionShapeCacheable cacheable = new BakedCollisionShapeCacheable
                        {
                            RigidBody = new hkpRigidBody
                            {
                                m_autoRemoveLevel = rigidBody.m_autoRemoveLevel,
                                m_collidable = new hkpLinkedCollidable
                                {
                                    m_allowedPenetrationDepth = rigidBody.m_collidable.m_allowedPenetrationDepth,
                                    m_broadPhaseHandle = rigidBody.m_collidable.m_broadPhaseHandle,
                                    m_forceCollideOntoPpu = rigidBody.m_collidable.m_forceCollideOntoPpu,
                                    m_shape = new hkpStaticCompoundShape
                                    {
                                        m_bitsPerKey = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_bitsPerKey,
                                        m_bvTreeType = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_bvTreeType,
                                        m_disabledLargeShapeKeyTable = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_disabledLargeShapeKeyTable,
                                        m_dispatchType = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_dispatchType,
                                        m_instanceExtraInfos = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instanceExtraInfos,
                                        m_instances = new List<hkpStaticCompoundShapeInstance>
                                        {
                                            new hkpStaticCompoundShapeInstance()
                                            {
                                                m_childFilterInfoMask = shape.Instance.m_childFilterInfoMask,
                                                m_filterInfo = shape.Instance.m_filterInfo,
                                                m_instanceFlags = shape.Instance.m_instanceFlags,
                                                m_position = Vector3.Zero,
                                                m_rotation = Quaternion.Identity,
                                                m_scale = Vector3.One,
                                                m_shape = shape.Instance.m_shape,
                                                m_userData = shape.Instance.m_userData,
                                                m_ukn = shape.Instance.m_ukn
                                            }
                                        },
                                        m_numBitsForChildShapeKey = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_numBitsForChildShapeKey,
                                        m_shapeInfoCodecType = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_shapeInfoCodecType,
                                        m_tree = new hkcdStaticTreeDefaultTreeStorage6()
                                        {
                                            m_domain = new hkAabb
                                            {
                                                m_min = Vector4.Zero,
                                                m_max = Vector4.Zero,
                                            },
                                            m_nodes = new List<hkcdStaticTreeCodec3Axis6>()
                                        },
                                        m_userData = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_userData,
                                    },
                                    m_shapeKey = rigidBody.m_collidable.m_shapeKey
                                },
                                m_contactPointCallbackDelay = rigidBody.m_contactPointCallbackDelay,
                                m_damageMultiplier = rigidBody.m_damageMultiplier,
                                m_localFrame = rigidBody.m_localFrame,
                                m_material = rigidBody.m_material,
                                m_motion = rigidBody.m_motion,
                                m_multiThreadCheck = rigidBody.m_multiThreadCheck,
                                m_name = rigidBody.m_name,
                                m_npData = rigidBody.m_npData,
                                m_numShapeKeysInContactPointProperties = rigidBody.m_numShapeKeysInContactPointProperties,
                                m_properties = rigidBody.m_properties,
                                m_responseModifierFlags = rigidBody.m_responseModifierFlags,
                                m_spuCollisionCallback = rigidBody.m_spuCollisionCallback,
                                m_storageIndex = rigidBody.m_storageIndex,
                                m_uid = rigidBody.m_uid
                            },
                            SystemIndex = shape.SystemIndex,
                            BodyGroup = shape.ShapeInfo.m_BodyGroup,
                            BodyLayerType = shape.ShapeInfo.m_BodyLayerType,
                            NullActorInfoPtr = shape.NullActorInfoPtr
                        };

                        cacheables[i] = cacheable;
                    }

                    return cacheables;
                }
            }
            return null;
        }

        /// <summary>
        /// Add shape to collision for actor. Returns true on success.
        /// </summary>
        public bool AddShape(BakedCollisionShapeCacheable cacheable, uint hashId, System.Numerics.Vector3 translation, System.Numerics.Quaternion rotation, System.Numerics.Vector3 scale)
        {
            // Some shapes we can't find the aabb for yet.
            if (cacheable.Shape is not hkpBvCompressedMeshShape && cacheable.Shape is not hkpConvexVerticesShape)
                return false;

            #region Build Instance
            InstanceFlags instanceFlags = cacheable.Instance.m_instanceFlags;
            if (scale != Vector3.One)
                instanceFlags |= InstanceFlags.SCALED;
            else
                instanceFlags &= ~InstanceFlags.SCALED;

            hkpStaticCompoundShapeInstance shapeInstance = new hkpStaticCompoundShapeInstance()
            {
                m_position = translation,
                m_rotation = rotation,
                m_scale = scale,
                m_instanceFlags = instanceFlags,
                m_ukn = cacheable.Instance.m_ukn,
                m_shape = cacheable.Shape,
                m_filterInfo = cacheable.Instance.m_filterInfo,
                m_childFilterInfoMask = cacheable.Instance.m_childFilterInfoMask,
                m_userData = 0 // Set when applying actor shape pairings
            };
            #endregion

            #region New shape pairing
            // Get BVH
            BVNode shapeBvh = GenerateShapeBvh(cacheable.Shape);
            BVNode leafBvhNode = leafBvhNode = TransformLeaf(shapeBvh, ComposeMatrix(translation, rotation, scale));

            // Generate rigidbody if needed
            cacheable.RigidBody.m_name = Prefix + cacheable.RigidBody.m_name.Substring(cacheable.RigidBody.m_name.IndexOf('_')); // Update name to match compound

            int rigidBodyIdx = ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[cacheable.SystemIndex].m_rigidBodies.FindIndex(x => x.m_name == cacheable.RigidBody.m_name);
            if (rigidBodyIdx == -1)
            {
                ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[cacheable.SystemIndex].m_rigidBodies.Add(cacheable.RigidBody);
                rigidBodyIdx = ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[cacheable.SystemIndex].m_rigidBodies.Count - 1;
            }
            else // When merging into an existing rigidbody we might need to update some stuff.
            {
                hkpRigidBody rigidBody = ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[cacheable.SystemIndex].m_rigidBodies[rigidBodyIdx];

                sbyte oldNumBitsForChildShapeKey = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_numBitsForChildShapeKey;
                sbyte newNumBitsForChildShapeKey = ((hkpStaticCompoundShape)cacheable.RigidBody.m_collidable.m_shape).m_numBitsForChildShapeKey;
                if (newNumBitsForChildShapeKey > oldNumBitsForChildShapeKey)
                    ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[cacheable.SystemIndex].m_rigidBodies[rigidBodyIdx].m_collidable.m_shape).m_numBitsForChildShapeKey = newNumBitsForChildShapeKey;

                byte oldNumShapeKeysInContactPointProperties = rigidBody.m_numShapeKeysInContactPointProperties;
                byte newNumShapeKeysInContactPointProperties = cacheable.RigidBody.m_numShapeKeysInContactPointProperties;
                if (newNumShapeKeysInContactPointProperties > oldNumShapeKeysInContactPointProperties)
                    ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[cacheable.SystemIndex].m_rigidBodies[rigidBodyIdx].m_numShapeKeysInContactPointProperties = newNumShapeKeysInContactPointProperties;
            }

            // Add our data
            if (ShapePairings.Any(x => x.ActorInfo?.m_HashId == hashId))
            {
                ShapePairings.Find(x => x.ActorInfo?.m_HashId == hashId).Shapes.Add(new ShapeInfoShapeInstancePairing()
                {
                    ShapeInfo = new ShapeInfo() {
                        m_ActorInfoIndex = 0, // Set when applying actor shape pairings
                        m_InstanceId = 0, // If you put all instances per the object into an array, I think this would be the index into that.
                        m_BodyGroup = cacheable.BodyGroup,
                        m_BodyLayerType = cacheable.BodyLayerType
                    },
                    Instance = shapeInstance,
                    RigidBodyIndex = rigidBodyIdx, // Which rigid body should this live in?
                    SystemIndex = cacheable.SystemIndex, // Which system should this live in?
                    LeafNode = leafBvhNode,
                    NullActorInfoPtr = cacheable.NullActorInfoPtr
                });
            }
            else
            {
                ShapePairings.Add(new ActorShapePairing()
                {
                    ActorInfo = new ActorInfo()
                    {
                        m_HashId = hashId,
                        m_ShapeInfoStart = 0, // Set when applying actor shape pairings
                        m_ShapeInfoEnd = 0 // Set when applying actor shape pairings,
                    },
                    Shapes = new List<ShapeInfoShapeInstancePairing>()
                    {
                        new ShapeInfoShapeInstancePairing()
                        {
                            ShapeInfo = new ShapeInfo()
                            {
                                m_ActorInfoIndex = 0, // Set when applying actor shape pairings
                                m_InstanceId = 0, // If you put all instances per the object into an array, I think this would be the index into that.
                                m_BodyGroup = cacheable.BodyGroup,
                                m_BodyLayerType = cacheable.BodyLayerType
                            },
                            Instance = shapeInstance,
                            RigidBodyIndex = rigidBodyIdx, // Which rigid body should this live in?
                            SystemIndex = cacheable.SystemIndex, // Which system should this live in?
                            LeafNode = leafBvhNode,
                            NullActorInfoPtr = cacheable.NullActorInfoPtr
                        }
                    }
                });
            }
            #endregion

            return true;
        }
        
        public void RemoveShape(uint hashId)
        {
            int pairingIdx = ShapePairings.FindIndex(x => x.ActorInfo?.m_HashId == hashId);
            if (pairingIdx == -1)
                return;

            ShapePairings.RemoveAt(pairingIdx);
        }

        public bool ShapeExists(uint hashId)
        {
            int pairingIdx = ShapePairings.FindIndex(x => x.ActorInfo?.m_HashId == hashId);
            if (pairingIdx == -1)
                return false;

            return true;
        }

        /// <summary>
        /// Updates a shape's transform given HashId.
        /// </summary>
        /// <returns>True if shape is present. False if shape is not present.</returns>
        public bool UpdateShapeTransform(uint hashId, Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            int pairingIdx = ShapePairings.FindIndex(x => x.ActorInfo?.m_HashId == hashId);
            if (pairingIdx == -1)
                return false;

            for (int shapeIdx = 0; shapeIdx < ShapePairings[pairingIdx].Shapes.Count; shapeIdx++)
            {
                // Get
                ShapeInfoShapeInstancePairing shapePairing = ShapePairings[pairingIdx].Shapes[shapeIdx];

                // Some shapes we can't find the aabb for yet.
                if (shapePairing.Instance.m_shape is not hkpBvCompressedMeshShape && shapePairing.Instance.m_shape is not hkpConvexVerticesShape)
                    continue;

                // Set transform
                Matrix4x4 originalTransform = ComposeMatrix(shapePairing.Instance.m_position, shapePairing.Instance.m_rotation, shapePairing.Instance.m_scale);
                shapePairing.Instance.m_position = translation;
                shapePairing.Instance.m_rotation = rotation;
                shapePairing.Instance.m_scale = scale;
                if (scale != Vector3.One)
                    shapePairing.Instance.m_instanceFlags |= InstanceFlags.SCALED;
                else
                    shapePairing.Instance.m_instanceFlags &= ~InstanceFlags.SCALED;

                // Set BVH (If we can't find the shape BVH it's not supported yet... leave vanilla leaf intact.)
                BVNode shapeBvh = GenerateShapeBvh(shapePairing.Instance.m_shape);
                if (shapeBvh != null)
                    shapePairing.LeafNode = TransformLeaf(shapeBvh, ComposeMatrix(translation, rotation, scale));

                // Apply
                ShapePairings[pairingIdx].Shapes[shapeIdx] = shapePairing;
            }

            return true;
        }
        #endregion

        #region Cache object building
        public void CreateForCaching()
        {
            StaticCompound = new StaticCompoundInfo
            {
                m_Offset = 0,
                m_ActorInfo = new List<ActorInfo>(),
                m_ShapeInfo = new List<ShapeInfo>()
            };

            List<hkpPhysicsSystem> systems = new List<hkpPhysicsSystem>(17);
            for (int i = 0; i < 17; i++)
            {
                systems.Add(new hkpPhysicsSystem
                {
                    m_rigidBodies = new List<hkpRigidBody>(),
                    m_constraints = new List<hkpConstraintInstance>(),
                    m_actions = new List<hkpAction>(),
                    m_phantoms = new List<hkpPhantom>(),
                    m_name = $@"Compound_{i}",
                    m_userData = 0,
                    m_active = true,
                });
            }

            Root = new hkRootLevelContainer
            {
                m_namedVariants = new List<hkRootLevelContainerNamedVariant>()
                {
                    new hkRootLevelContainerNamedVariant
                    {
                        m_name = "hkpPhysicsData",
                        m_className = "hkpPhysicsData",
                        m_variant = new hkpPhysicsData
                        {
                            m_worldCinfo = null,
                            m_systems = systems
                        }
                    }
                }
            };

            ShapePairings = GenerateActorShapePairings();
            FileSettings = new STFileLoader.Settings()
            {
                CompressionFormat = new Toolbox.Core.Uncompressed(),
            };
        }
        #endregion

        #region Collision manipulation extras
        /// <summary>
        /// Untested.
        /// </summary>
        public void AddMesh(List<Vector3> vertices, List<uint> indices, IEnumerable<Tuple<uint, uint>> primitiveInfos)
        {
            // This is from Kreny's HKX2 blender addon! Go check it out at https://gitlab.com/HKX2/BlenderAddon/-/blob/main/lib/BlenderAddon/BlenderAddon/Generator.cs
            // I typed it all out because I felt like it
            Root.m_namedVariants.Add(new hkRootLevelContainerNamedVariant()
            {
                m_name = "Physics Data",
                m_className = "hkpPhysicsData",
                m_variant = new hkpPhysicsData
                {
                    m_worldCinfo = null,
                    m_systems = new List<hkpPhysicsSystem>
                    {
                        new()
                        {
                            m_rigidBodies = new List<hkpRigidBody>
                            {
                                new()
                                {
                                    m_userData = 0,
                                    m_collidable = new hkpLinkedCollidable
                                    {
                                        m_shape = hkpBvCompressedMeshShapeBuilder.Build(vertices, indices, primitiveInfos),
                                        m_shapeKey = 0xFFFFFFFF,
                                        m_forceCollideOntoPpu = 8,
                                        m_broadPhaseHandle = new hkpTypedBroadPhaseHandle
                                        {
                                            m_type = BroadPhaseType.BROAD_PHASE_ENTITY,
                                            m_objectQualityType = 0,
                                            m_collisionFilterInfo = 0x90000000
                                        },
                                        m_allowedPenetrationDepth = float.MaxValue
                                    },
                                    m_multiThreadCheck = new hkMultiThreadCheck(),
                                    m_name = "Collision_IDK", // Lolll perfect
                                    m_properties = new List<hkSimpleProperty>(),
                                    m_material = new hkpMaterial
                                    {
                                        m_responseType = ResponseType.RESPONSE_SIMPLE_CONTACT,
                                        m_rollingFrictionMultiplier = 0,
                                        m_friction = .5f,
                                        m_restitution = .4f
                                    },
                                    m_damageMultiplier = 1f,
                                    m_storageIndex = 0xFFFF,
                                    m_contactPointCallbackDelay = 0xFFFF,
                                    m_autoRemoveLevel = 0,
                                    m_numShapeKeysInContactPointProperties = 1,
                                    m_responseModifierFlags = 0,
                                    m_uid = 0xFFFFFFFF,
                                    m_spuCollisionCallback = new hkpEntitySpuCollisionCallback
                                    {
                                        m_eventFilter = SpuCollisionCallbackEventFilter.SPU_SEND_CONTACT_POINT_ADDED_OR_PROCESS,
                                        m_userFilter = 1
                                    },
                                    m_motion = new hkpMaxSizeMotion
                                    {
                                        m_type = MotionType.MOTION_FIXED,
                                        m_deactivationIntegrateCounter = 15,
                                        m_deactivationNumInactiveFrames_0 = 0xC000,
                                        m_deactivationNumInactiveFrames_1 = 0xC000,
                                        m_motionState = new hkMotionState
                                        {
                                            m_transform = Matrix4x4.Identity,
                                            m_sweptTransform_0 = new Vector4(.0f),
                                            m_sweptTransform_1 = new Vector4(.0f),
                                            m_sweptTransform_2 = new Vector4(.0f, .0f, .0f, .99999994f),
                                            m_sweptTransform_3 = new Vector4(.0f, .0f, .0f, .99999994f),
                                            m_sweptTransform_4 = new Vector4(.0f),
                                            m_deltaAngle = new Vector4(.0f),
                                            m_objectRadius = 2.25f,
                                            m_linearDamping = 0,
                                            m_angularDamping = 0x3D4D,
                                            m_timeFactor = 0x3F80,
                                            m_maxLinearVelocity = new hkUFloat8 {m_value = 127},
                                            m_maxAngularVelocity = new hkUFloat8 {m_value = 127},
                                            m_deactivationClass = 1
                                        },
                                        m_inertiaAndMassInv = new Vector4(.0f),
                                        m_linearVelocity = new Vector4(.0f),
                                        m_angularVelocity = new Vector4(.0f),
                                        m_deactivationRefOrientation_0 = 0,
                                        m_deactivationRefOrientation_1 = 0,
                                        m_savedMotion = null,
                                        m_savedQualityTypeIndex = 0,
                                        m_gravityFactor = 0x3F80
                                    },
                                    m_localFrame = null,
                                    m_npData = 0xFFFFFFFF
                                }
                            },
                            m_constraints = new List<hkpConstraintInstance>(),
                            m_actions = new List<hkpAction>(),
                            m_phantoms = new List<hkpPhantom>(),
                            m_name = "Default Physics System",
                            m_userData = 0,
                            m_active = true
                        }
                    }
                }
            });
        }
        #endregion

        #region Utils
        private BVNode GenerateShapeBvh(hkpShape shape)
        {
            if (shape is hkpBvCompressedMeshShape)
            {
                return ((hkpBvCompressedMeshShape)shape).GetMeshBvh();
            }
            else if (shape is hkpConvexVerticesShape)
            {
                Vector4 center = ((hkpConvexVerticesShape)shape).m_aabbCenter;
                Vector4 halfExtents = ((hkpConvexVerticesShape)shape).m_aabbHalfExtents;

                return new BVNode()
                {
                    Min = new Vector3(center.X, center.Y, center.Z) - new Vector3(halfExtents.X, halfExtents.Y, halfExtents.Z),
                    Max = new Vector3(center.X, center.Y, center.Z) + new Vector3(halfExtents.X, halfExtents.Y, halfExtents.Z)
                };
            }

            return null;
        }

        private BVNode TransformLeaf(BVNode shapeBvh, Matrix4x4 instanceTransform)
        {
            if (shapeBvh == null)
                return null;

            BVNode instanceBvhLeaf = new BVNode()
            {
                IsLeaf = true,
                IsSectionHead = false,
                Primitive = 0,
                PrimitiveCount = 1,
                Left = null,
                Right = null,
                Min = shapeBvh.Min,
                Max = shapeBvh.Max
            };

            // A little bit annoying that we have to convert stuff between System.Numerics and OpenTK, but it's worth it to be able to reuse aabb code.
            BoundingBox leafBvhBoundingNode = new BoundingBox(new OpenTK.Vector3(instanceBvhLeaf.Min.X, instanceBvhLeaf.Min.Y, instanceBvhLeaf.Min.Z), new OpenTK.Vector3(instanceBvhLeaf.Max.X, instanceBvhLeaf.Max.Y, instanceBvhLeaf.Max.Z));
            leafBvhBoundingNode.UpdateTransform(new OpenTK.Matrix4()
            {
                M11 = instanceTransform.M11,
                M12 = instanceTransform.M12,
                M13 = instanceTransform.M13,
                M14 = instanceTransform.M14,
                M21 = instanceTransform.M21,
                M22 = instanceTransform.M22,
                M23 = instanceTransform.M23,
                M24 = instanceTransform.M24,
                M31 = instanceTransform.M31,
                M32 = instanceTransform.M32,
                M33 = instanceTransform.M33,
                M34 = instanceTransform.M34,
                M41 = instanceTransform.M41,
                M42 = instanceTransform.M42,
                M43 = instanceTransform.M43,
                M44 = instanceTransform.M44
            });
            instanceBvhLeaf.Min.X = leafBvhBoundingNode.Min.X;
            instanceBvhLeaf.Min.Y = leafBvhBoundingNode.Min.Y;
            instanceBvhLeaf.Min.Z = leafBvhBoundingNode.Min.Z;
            instanceBvhLeaf.Max.X = leafBvhBoundingNode.Max.X;
            instanceBvhLeaf.Max.Y = leafBvhBoundingNode.Max.Y;
            instanceBvhLeaf.Max.Z = leafBvhBoundingNode.Max.Z;

            return instanceBvhLeaf;
        }

        private Matrix4x4 ComposeMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(translation);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(rotation);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale);
            return scaleMatrix * rotationMatrix * translationMatrix;
        }
        #endregion

        public void Save(Stream stream)
        {
            UpdateRenders(); // Might as well apply render updates
            ApplyActorShapePairings(ShapePairings);

            var uncompressed = new MemoryStream();
            Util.WriteBotwHKX(new IHavokObject[] { StaticCompound, Root }, Header, ".hksc", uncompressed);

            uncompressed.Position = 0;
            stream.Position = 0;
            FileSettings.CompressionFormat.Compress(uncompressed).CopyTo(stream);
            stream.SetLength(stream.Position);
        }

        #region Pairing structs
        private struct ShapeInfoShapeInstancePairing
        {
            public ShapeInfo ShapeInfo;
            public hkpStaticCompoundShapeInstance Instance;
            public int RigidBodyIndex;
            public int SystemIndex;
            public BVNode LeafNode;
            public bool NullActorInfoPtr;
        }
        private struct ActorShapePairing
        {
            public ActorInfo ActorInfo;
            public List<ShapeInfoShapeInstancePairing> Shapes;
        }
        #endregion

        #region Pairing creation and application
        /// <summary>
        /// Looks through all rigid bodies and finds a shape instance based on userData.
        /// </summary>
        private hkpStaticCompoundShapeInstance GetShapeInstanceByUserData(ulong userData)
        {
            foreach (var system in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems)
            {
                foreach (var rigidBody in system.m_rigidBodies)
                {
                    foreach (var shapeInstance in ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances)
                    {
                        if (shapeInstance.m_userData == userData)
                            return shapeInstance;
                    }
                }
            }
            
            return null;
        }
        /// <summary>
        /// Looks through all rigid bodies and finds a shape instance based on userData. Returns the index.
        /// </summary>
        private int GetShapeInstanceIndexByUserData(ulong userData)
        {
            foreach (var system in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems)
            {
                foreach (var rigidBody in system.m_rigidBodies)
                {
                    for (int i = 0; i < ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances.Count; i++)
                        if (((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances[i].m_userData == userData)
                            return i;
                }
            }

            return -1;
        }
        /// <summary>
        /// Finds which rigid body contains a shape instance with the given userData.
        /// </summary>
        private int GetShapeRigidBodyIndexByUserData(ulong userData)
        {
            foreach (var system in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems)
            {
                for (int index = 0; index < system.m_rigidBodies.Count; index++)
                {
                    foreach (var shapeInstance in ((hkpStaticCompoundShape)system.m_rigidBodies[index].m_collidable.m_shape).m_instances)
                    {
                        if (shapeInstance.m_userData == userData)
                            return index;
                    }
                }
            }

            return -1;
        }
        /// <summary>
        /// Finds which system contains the rigid body that contains a shape instance with the given userData.
        /// </summary>
        private int GetShapeSystemIndexByUserData(ulong userData)
        {
            for (int index = 0; index < ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems.Count; index++)
            {
                foreach (var rigidBody in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[index].m_rigidBodies)
                {
                    foreach (var shapeInstance in ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances)
                    {
                        if (shapeInstance.m_userData == userData)
                            return index;
                    }
                }
            }

            return -1;
        }
        /// <summary>
        /// Find ActorInfo based on a shapeInfo index it points to.
        /// Try to avoid calling this if possible as it might find an ActorInfo referencing a shape instance in another rigidbody.
        /// </summary>
        private ActorInfo GetActorInfoByShapeIndex(int index)
        {
            foreach (ActorInfo actorInfo in StaticCompound.m_ActorInfo)
            {
                if (actorInfo.m_ShapeInfoStart <= index && actorInfo.m_ShapeInfoEnd >= index)
                {
                    return actorInfo;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a BVH leaf node containing the given primative.
        /// </summary>
        private BVNode GetShapeLeafNodeByPrimitive(int primitive, int rigidBodyIndex, int systemIndex)
        {
            if (primitive == -1)
                return null;
            BVNode rigidBodyBVH = ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[systemIndex].m_rigidBodies[rigidBodyIndex].m_collidable.m_shape).m_tree.GetBVH();
            foreach (BVNode leafNode in BVNode.GetLeafNodes(rigidBodyBVH))
                if (leafNode.Primitive == primitive)
                    return leafNode;

            return null;
        }

        private List<ActorShapePairing> GenerateActorShapePairings()
        {
            List<ActorShapePairing> shapePairings = new List<ActorShapePairing>(StaticCompound.m_ActorInfo.Count);
            for (int i = 0; i < StaticCompound.m_ShapeInfo.Count; i++)
            {
                ShapeInfo shapeInfo = StaticCompound.m_ShapeInfo[i];

                ShapeInfoShapeInstancePairing shapeInstancePairing = new ShapeInfoShapeInstancePairing() // Todo: Store system index and utilize when rebuilding or caching
                {
                    ShapeInfo = shapeInfo,
                    Instance = GetShapeInstanceByUserData((ulong)i),
                    RigidBodyIndex = GetShapeRigidBodyIndexByUserData((ulong)i),
                    SystemIndex = GetShapeSystemIndexByUserData((ulong)i),
                    LeafNode = GetShapeLeafNodeByPrimitive(GetShapeInstanceIndexByUserData((ulong)i), GetShapeRigidBodyIndexByUserData((ulong)i), GetShapeSystemIndexByUserData((ulong)i)),
                    NullActorInfoPtr = shapeInfo.m_ActorInfoIndex == -1
                };

                // Find existing shape pairing or create a new one
                int actorShapePairingIdx = shapePairings.FindIndex(x => x.ActorInfo?.m_ShapeInfoStart <= i && x.ActorInfo?.m_ShapeInfoEnd >= i);
                if (actorShapePairingIdx != -1)
                    shapePairings[actorShapePairingIdx].Shapes.Add(shapeInstancePairing);
                else
                    shapePairings.Add(new ActorShapePairing()
                    {
                        ActorInfo = shapeInfo.m_ActorInfoIndex != -1 ? StaticCompound.m_ActorInfo[shapeInfo.m_ActorInfoIndex] : GetActorInfoByShapeIndex(i),
                        Shapes = new List<ShapeInfoShapeInstancePairing>()
                        {
                            shapeInstancePairing
                        }
                    });
            }

            return shapePairings;
        }

        private void ApplyActorShapePairings(List<ActorShapePairing> shapePairings)
        {
            StaticCompound.m_ActorInfo.Clear();
            StaticCompound.m_ShapeInfo.Clear();
            foreach (hkpPhysicsSystem system in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems)
                foreach (hkpRigidBody rigidBody in system.m_rigidBodies)
                    ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances.Clear();

            foreach (ActorShapePairing pairing in shapePairings)
                if (pairing.ActorInfo != null)
                    StaticCompound.m_ActorInfo.Add(pairing.ActorInfo);
            StaticCompound.m_ActorInfo.Sort(delegate (ActorInfo x, ActorInfo y)
            {
                if (x.m_HashId == y.m_HashId)
                    return 0;
                return (x.m_HashId > y.m_HashId) ? 1 : -1;
            });

            foreach (ActorShapePairing pairing in shapePairings)
            {
                int actorInfoIndex = StaticCompound.m_ActorInfo.FindIndex(x => x.m_HashId == pairing.ActorInfo?.m_HashId);
                foreach (ShapeInfoShapeInstancePairing shapeData in pairing.Shapes)
                {
                    shapeData.ShapeInfo.m_ActorInfoIndex = actorInfoIndex;
                    StaticCompound.m_ShapeInfo.Add(shapeData.ShapeInfo);
                }
            }
            StaticCompound.m_ShapeInfo.Sort(delegate (ShapeInfo x, ShapeInfo y)
            {
                if (x.m_ActorInfoIndex != y.m_ActorInfoIndex)
                {
                    if (x.m_ActorInfoIndex == -1 || y.m_ActorInfoIndex == -1)
                        return (x.m_ActorInfoIndex < y.m_ActorInfoIndex) ? 1 : -1;
                    else
                        return (x.m_ActorInfoIndex > y.m_ActorInfoIndex) ? 1 : -1;
                }
                //if (x.m_InstanceId != y.m_InstanceId)
                //    return (x.m_InstanceId > y.m_InstanceId) ? 1 : -1;

                return 0;
            });
            for (int i = 0; i < StaticCompound.m_ActorInfo.Count; i++)
            {
                StaticCompound.m_ActorInfo[i].m_ShapeInfoStart = StaticCompound.m_ShapeInfo.FindIndex(x => x.m_ActorInfoIndex == i);
                StaticCompound.m_ActorInfo[i].m_ShapeInfoEnd = StaticCompound.m_ShapeInfo.FindLastIndex(x => x.m_ActorInfoIndex == i);
            }
            // Apply null actorinfo pointers... why do these exist? Idk.
            foreach (ActorShapePairing pairing in shapePairings)
            {
                foreach (ShapeInfoShapeInstancePairing shapeData in pairing.Shapes)
                {
                    int shapeInfoIdx = StaticCompound.m_ShapeInfo.FindIndex(x => x.m_InstanceId == shapeData.ShapeInfo.m_InstanceId && x.m_ActorInfoIndex == shapeData.ShapeInfo.m_ActorInfoIndex);
                    StaticCompound.m_ShapeInfo[shapeInfoIdx].m_ActorInfoIndex = shapeData.NullActorInfoPtr ? -1 : StaticCompound.m_ShapeInfo[shapeInfoIdx].m_ActorInfoIndex;
                }
            }
            

            foreach (ActorShapePairing pairing in shapePairings)
            {
                foreach (ShapeInfoShapeInstancePairing shapeData in pairing.Shapes)
                {
                    shapeData.Instance.m_userData = (ulong)StaticCompound.m_ShapeInfo.FindIndex(x => x == shapeData.ShapeInfo);
                    ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[shapeData.SystemIndex].m_rigidBodies[shapeData.RigidBodyIndex].m_collidable.m_shape).m_instances.Add(shapeData.Instance);
                }
            }

            foreach (var system in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems)
            {
                foreach (var rigidBody in system.m_rigidBodies)
                {
                    ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances.Sort(delegate (hkpStaticCompoundShapeInstance x, hkpStaticCompoundShapeInstance y)
                    {
                        if (x.m_userData == y.m_userData)
                            return 0;
                        return (x.m_userData > y.m_userData) ? 1 : -1;
                    });
                }
            }

            foreach (var system in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems)
            {
                foreach (var rigidBody in system.m_rigidBodies)
                {
                    for (int i = 0; i < ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances.Count; i++)
                    {
                        hkpStaticCompoundShapeInstance instance = ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances[i];
                        StaticCompound.m_ShapeInfo[(int)instance.m_userData].m_InstanceId = i;
                    }
                }
            }


            List<List<List<BVNode>>> leafNodes = new List<List<List<BVNode>>>();
            foreach (hkpPhysicsSystem system in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems)
            {
                leafNodes.Add(new List<List<BVNode>>());
                foreach (hkpRigidBody rigidBody in system.m_rigidBodies)
                    leafNodes.Last().Add(new List<BVNode>());
            }

            foreach (ActorShapePairing pairing in shapePairings)
            {
                foreach (ShapeInfoShapeInstancePairing shapeData in pairing.Shapes)
                {
                    if (shapeData.LeafNode == null)
                        continue;
                    shapeData.LeafNode.Primitive = (uint)GetShapeInstanceIndexByUserData(shapeData.Instance.m_userData);
                    leafNodes[shapeData.SystemIndex][shapeData.RigidBodyIndex].Add(shapeData.LeafNode);
                }
            }

            for (int systemIndex = 0; systemIndex < ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems.Count; systemIndex++)
            {
                for (int rigidBodyIndex = 0; rigidBodyIndex < ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[systemIndex].m_rigidBodies.Count; rigidBodyIndex++)
                {
                    if (leafNodes[systemIndex][rigidBodyIndex].Count == 0)
                        continue;
                    BVNode rigidBodyBVH = new BVNode() { IsLeaf = false };
                    rigidBodyBVH = BVNode.InsertLeafs(rigidBodyBVH, leafNodes[systemIndex][rigidBodyIndex]);
                    ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[systemIndex].m_rigidBodies[rigidBodyIndex].m_collidable.m_shape).m_tree.m_nodes = rigidBodyBVH.BuildAxis6Tree();
                    ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[systemIndex].m_rigidBodies[rigidBodyIndex].m_collidable.m_shape).m_tree.m_domain.m_min = new Vector4(rigidBodyBVH.Min, 0);
                    ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[systemIndex].m_rigidBodies[rigidBodyIndex].m_collidable.m_shape).m_tree.m_domain.m_max = new Vector4(rigidBodyBVH.Max, 0);
                }
            }
        }
        #endregion

        #region Rendering
        private bool _isVisible = true;
        public bool IsVisible 
        {
            get
            {
                // Kinda meh location for this logic
                // but it cleans up the user experience
                // in such a small way that I'm not too worried about it.
                if (!MapData.ShowCollisionShapes)
                    RootNode.HasCheckBox = false;
                else
                    RootNode.HasCheckBox = true;

                return MapData.ShowCollisionShapes && _isVisible;
            }
            set
            {
                _isVisible = value;
            }
        }

        private List<HavokMeshShapeRender> ShapeRenders = new List<HavokMeshShapeRender>();

        private void UpdateRenders(GLScene scene = null)
        {
            foreach (HavokMeshShapeRender render in ShapeRenders)
                scene?.RemoveRenderObject(render);
            ShapeRenders.Clear();
            foreach (ActorShapePairing actorPairing in ShapePairings)
            {
                foreach (ShapeInfoShapeInstancePairing shapePairing in actorPairing.Shapes)
                {
                    if (shapePairing.Instance.m_shape is not hkpBvCompressedMeshShape) // Only thing supported rn.
                        continue;

                    HavokMeshShapeRender render = new HavokMeshShapeRender(RootNode);
                    render.LoadShape((hkpBvCompressedMeshShape)shapePairing.Instance.m_shape);

                    render.Transform.Position = new OpenTK.Vector3(shapePairing.Instance.m_position.X, shapePairing.Instance.m_position.Y, shapePairing.Instance.m_position.Z) * GLContext.PreviewScale;
                    render.Transform.Rotation = new OpenTK.Quaternion(shapePairing.Instance.m_rotation.X, shapePairing.Instance.m_rotation.Y, shapePairing.Instance.m_rotation.Z, shapePairing.Instance.m_rotation.W);
                    render.Transform.Scale = new OpenTK.Vector3(shapePairing.Instance.m_scale.X, shapePairing.Instance.m_scale.Y, shapePairing.Instance.m_scale.Z);
                    render.Transform.UpdateMatrix(true);

                    if (shapePairing.LeafNode != null)
                        render.SetBounding(new BoundingNode(shapePairing.LeafNode.Min * GLContext.PreviewScale, shapePairing.LeafNode.Max * GLContext.PreviewScale));

                    ((EditableObjectNode)render.UINode).UIProperyDrawer += delegate
                    {
                        ImGui.Separator();
                        ImGui.Text("Debug Shape Info:");
                        ImGui.Separator();
                        ImGui.Text("System Index:");
                        ImGui.Text(shapePairing.SystemIndex.ToString());
                        ImGui.Separator();
                        ImGui.Text("RigidBody Index:");
                        ImGui.Text(shapePairing.RigidBodyIndex.ToString());
                        ImGui.Separator();
                        ImGui.Text("LeafNode:");
                        if (shapePairing.LeafNode == null)
                        {
                            ImGui.Text("Null");
                        }
                        else
                        {
                            ImGui.Text("Min:");
                            ImGui.Text(shapePairing.LeafNode.Min.ToString());
                            ImGui.Text("Max:");
                            ImGui.Text(shapePairing.LeafNode.Max.ToString());
                        }
                        ImGui.Separator();
                        ImGui.Text("Null ActorInfo Ptr:");
                        ImGui.Text(shapePairing.NullActorInfoPtr.ToString());
                        ImGui.Separator();
                        if (!shapePairing.NullActorInfoPtr)
                        {
                            ImGui.Text("HashId:");
                            ImGui.Text(actorPairing.ActorInfo?.m_HashId.ToString());
                            ImGui.Separator();
                            ImGui.Text("SRTHash:");
                            ImGui.Text(actorPairing.ActorInfo.m_SRTHash.ToString());
                            ImGui.Separator();
                        }
                    };

                    ShapeRenders.Add(render);

                    scene?.AddRenderObject(render);
                }
            }
        }
        #endregion

        public void Dispose()
        {
            // Haha who knows who cares
        }
    }
}
