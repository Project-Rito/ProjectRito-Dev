using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;
using Toolbox.Core.IO;
using HKX2;
using HKX2Builders;
using HKX2Builders.Extensions;

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
    public class FieldCollisionLoader
    {
        private HKXHeader Header = HKXHeader.BotwWiiu(); // TODO - actually get the right platform
        private hkRootLevelContainer Root;
        private StaticCompoundInfo StaticCompound;

        public void Load(Stream stream)
        {
            List<IHavokObject> roots = Util.ReadBotwHKX(YAZ0.Decompress(stream.ReadAllBytes()), ".hksc");

            StaticCompound = (StaticCompoundInfo)roots[0];
            Root = (hkRootLevelContainer)roots[1];
        }

        public hkpShape[] GetShapes(uint hashId)
        {
            List<hkpShape> shapes = new List<hkpShape>(1);

            // TODO: In future do a binary search like https://github.com/krenyy/botw_havok/blob/dc7966c7780ef8c8a35e061cd3aacc20020fa2d7/botw_havok/cli/hkrb_extract.py#L30
            ActorInfo actorInfo = null;
            foreach (var actInfo in StaticCompound.m_ActorInfo)
            {
                if (actInfo.m_HashId == hashId)
                {
                    actorInfo = actInfo;
                    break;
                }
            }
            if (actorInfo == null)
                return null;

            foreach (hkpRigidBody rigidBody in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies)
            {

                foreach (hkpStaticCompoundShapeInstance instance in ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances)
                {
                    if (instance.m_userData >= (ulong)actorInfo.m_ShapeInfoStart && instance.m_userData <= (ulong)actorInfo.m_ShapeInfoEnd)
                    {
                        // This is a shape we need!
                        shapes.Add(instance.m_shape);
                    }
                }
            }

            return shapes.ToArray();
        }

        public void AddShape(hkpShape shape, Matrix4x4 transform, uint hashId)
        {
            List<ActorShapePairing> shapePairings = GenerateActorShapePairings();

            Vector3 translation = new Vector3();
            Quaternion rotation = new Quaternion();
            Vector3 scale = new Vector3();

            Matrix4x4.Decompose(transform, out scale, out rotation, out translation);

            hkpStaticCompoundShapeInstance shapeInstance = new hkpStaticCompoundShapeInstance()
            {
                m_transform = new Matrix4x4
                {
                    M11 = translation.X,
                    M12 = translation.Y,
                    M13 = translation.Z,
                    M14 = .5000001f,
                    M21 = rotation.X,
                    M22 = rotation.Y,
                    M23 = rotation.Z,
                    M24 = rotation.W,
                    M31 = scale.X,
                    M32 = scale.Y,
                    M33 = scale.Z,
                    M34 = .5f,
                    M41 = 0f,
                    M42 = 0f,
                    M43 = 0f,
                    M44 = 1f
                },
                m_shape = shape,
                m_filterInfo = 0,
                m_childFilterInfoMask = ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies[0].m_uid,
                m_userData = 0 // Set when applying actor shape pairings
            };

            // Add our data
            shapePairings.Add(new ActorShapePairing()
            {
                ActorInfo = new ActorInfo()
                {
                    m_HashId = hashId,
                    m_ShapeInfoStart = 0, // Set when applying actor shape pairings
                    m_ShapeInfoEnd = 0 // Set when applying actor shape pairings
                },
                Shapes = new List<ShapeInfoShapeInstancePairing>()
                {
                    new ShapeInfoShapeInstancePairing()
                    {
                        ShapeInfo = new ShapeInfo()
                        {
                            m_ActorInfoIndex = 0, // Set when applying actor shape pairings
                            m_InstanceId = 0, // If you put all instances per the object into an array, I think this would be the index into that.
                            m_BodyGroup = 0,
                            m_BodyLayerType = 0
                        },
                        Instance = shapeInstance,
                        RigidBodyIndex = 0 // Which rigid body should this live in?
                    }
                }
            });

            ApplyActorShapePairings(shapePairings);

            ActorInfo actorInfo = StaticCompound.m_ActorInfo.Find(x => x.m_HashId == hashId);


            var compoundBvh = ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies[0].m_collidable.m_shape).m_tree.GetBVH();


            var instanceBvh = ((hkpBvCompressedMeshShape)shape).GetMeshBvh();



            var leafBvhNode = new BVNode
            {
                IsLeaf = true,
                Primitive = (uint)((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies[0].m_collidable.m_shape).m_instances.FindIndex(x => x.m_userData == (ulong)actorInfo.m_ShapeInfoStart),
                PrimitiveCount = 1,
                Left = null,
                Right = null,
                Min = instanceBvh.Min,
                Max = instanceBvh.Max
            };

            leafBvhNode = BVNode.TransformBvh(leafBvhNode, transform);

            compoundBvh = compoundBvh.InsertLeaf(leafBvhNode);

            ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies[0].m_collidable.m_shape).m_tree.m_nodes = compoundBvh.BuildAxis6Tree();
        }

        /// <summary>
        /// Untested. Prefer using 
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

        public void Save(Stream stream)
        {
            var uncompressed = new MemoryStream();
            Util.WriteBotwHKX(new IHavokObject[] { StaticCompound, Root }, Header, ".hksc", uncompressed);

            stream.Position = 0;
            stream.Write(YAZ0.Compress(uncompressed.ReadAllBytes()));
            stream.SetLength(stream.Position + 1);
        }



        private struct ShapeInfoShapeInstancePairing
        {
            public ShapeInfo ShapeInfo;
            public hkpStaticCompoundShapeInstance Instance;
            public int RigidBodyIndex;
        }
        private struct ActorShapePairing
        {
            public ActorInfo ActorInfo;
            public List<ShapeInfoShapeInstancePairing> Shapes;
        }

        /// <summary>
        /// Looks through all rigid bodies and finds a shape instance based on userData.
        /// </summary>
        private hkpStaticCompoundShapeInstance GetShapeInstanceByUserData(ulong userData)
        {
            foreach (var rigidBody in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies)
            {
                foreach (var shapeInstance in ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances)
                {
                    if (shapeInstance.m_userData == userData)
                        return shapeInstance;
                }
            }
            
            return null;
        }
        /// <summary>
        /// Finds which rigid body contains a shape instance with the given userData.
        /// </summary>
        private int GetShapeRigidBodyIndexByUserData(ulong userData)
        {
            for (int index = 0; index < ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies.Count; index++)
            {
                foreach (var shapeInstance in ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies[index].m_collidable.m_shape).m_instances)
                {
                    if (shapeInstance.m_userData == userData)
                        return index;
                }
            }

            return 0;
        }

        private List<ActorShapePairing> GenerateActorShapePairings()
        {
            List<ActorShapePairing> shapePairings = new List<ActorShapePairing>(StaticCompound.m_ActorInfo.Count);
            foreach (var actorInfo in StaticCompound.m_ActorInfo)
            {
                List<ShapeInfoShapeInstancePairing> shapeInfoShapeInstancePairing = new List<ShapeInfoShapeInstancePairing>();
                for (int i = 0; i < StaticCompound.m_ShapeInfo.Count; i++)
                {
                    if (i >= actorInfo.m_ShapeInfoStart && i <= actorInfo.m_ShapeInfoEnd)
                    {
                        shapeInfoShapeInstancePairing.Add(new ShapeInfoShapeInstancePairing()
                        {
                            ShapeInfo = StaticCompound.m_ShapeInfo[i],
                            Instance = GetShapeInstanceByUserData((ulong)i),
                            RigidBodyIndex = GetShapeRigidBodyIndexByUserData((ulong)i)
                        });
                    }
                }

                shapePairings.Add(new ActorShapePairing
                {
                    ActorInfo = actorInfo,
                    Shapes = shapeInfoShapeInstancePairing
                });
            }

            return shapePairings;
        }

        private void ApplyActorShapePairings(List<ActorShapePairing> shapePairings)
        {
            StaticCompound.m_ActorInfo.Clear();
            StaticCompound.m_ShapeInfo.Clear();
            foreach (var rigidBody in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies)
                ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances.Clear();

            foreach (ActorShapePairing pairing in shapePairings)
                StaticCompound.m_ActorInfo.Add(pairing.ActorInfo);
            StaticCompound.m_ActorInfo.Sort(delegate (ActorInfo x, ActorInfo y)
            {
                if (x.m_HashId == y.m_HashId)
                    return 0;
                return (x.m_HashId > y.m_HashId) ? 1 : -1;
            });

            foreach (ActorShapePairing pairing in shapePairings)
            {
                int actorInfoIndex = StaticCompound.m_ActorInfo.FindIndex(x => x.m_HashId == pairing.ActorInfo.m_HashId);
                foreach (ShapeInfoShapeInstancePairing shapeData in pairing.Shapes)
                {
                    shapeData.ShapeInfo.m_ActorInfoIndex = actorInfoIndex;
                    StaticCompound.m_ShapeInfo.Add(shapeData.ShapeInfo);
                }
            }
            StaticCompound.m_ShapeInfo.Sort(delegate (ShapeInfo x, ShapeInfo y)
            {
                if (x.m_ActorInfoIndex != y.m_ActorInfoIndex)
                    return (x.m_ActorInfoIndex > y.m_ActorInfoIndex) ? 1 : -1;
                if (x.m_InstanceId != y.m_InstanceId)
                    return (x.m_InstanceId > y.m_InstanceId) ? 1 : -1;

                return 0;
            });

            for (int i = 0; i < StaticCompound.m_ActorInfo.Count; i++)
            {
                StaticCompound.m_ActorInfo[i].m_ShapeInfoStart = StaticCompound.m_ShapeInfo.FindIndex(x => x.m_ActorInfoIndex == i);
                StaticCompound.m_ActorInfo[i].m_ShapeInfoEnd = StaticCompound.m_ShapeInfo.FindLastIndex(x => x.m_ActorInfoIndex == i);
            }

            foreach (ActorShapePairing pairing in shapePairings)
            {
                foreach (ShapeInfoShapeInstancePairing shapeData in pairing.Shapes)
                {
                    shapeData.Instance.m_userData = (ulong)StaticCompound.m_ShapeInfo.FindIndex(x => x.m_ActorInfoIndex == shapeData.ShapeInfo.m_ActorInfoIndex && x.m_InstanceId == shapeData.ShapeInfo.m_InstanceId);
                    ((hkpStaticCompoundShape)((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies[shapeData.RigidBodyIndex].m_collidable.m_shape).m_instances.Add(shapeData.Instance);
                }
            }
            
            foreach (var rigidBody in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies)
            {
                ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances.Sort(delegate (hkpStaticCompoundShapeInstance x, hkpStaticCompoundShapeInstance y)
                {
                    if (x.m_userData == y.m_userData)
                        return 0;
                    return (x.m_userData > y.m_userData) ? 1 : -1;
                });
            }
        }
    }
}
