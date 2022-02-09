using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;
using HKX2;
using HKX2Builders;

namespace UKingLibrary
{
    public class FieldCollisionLoader
    {
        private HKXHeader Header = HKXHeader.BotwWiiu(); // TODO - actually get the right platform
        private hkRootLevelContainer Root;
        private StaticCompoundInfo StaticCompound;

        public void Load(Stream stream)
        {
            List<IHavokObject> roots = Util.ReadBotwHKX(stream, ".hksc");

            StaticCompound = (StaticCompoundInfo)roots[0];
            Root = (hkRootLevelContainer)roots[1];
        }

        public void GetShapes(uint hashId)
        {
            // TODO: In future do a binary search like https://github.com/krenyy/botw_havok/blob/dc7966c7780ef8c8a35e061cd3aacc20020fa2d7/botw_havok/cli/hkrb_extract.py#L30
            foreach (var actorInfo in StaticCompound.m_ActorInfo)
            {
                if (actorInfo.m_HashId == hashId)
                {
                    foreach (hkpRigidBody rigidBody in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies)
                    {

                        foreach (hkpStaticCompoundShapeInstance instance in ((hkpStaticCompoundShape)rigidBody.m_collidable.m_shape).m_instances)
                        {
                            if (instance.m_userData >= (ulong)actorInfo.m_ShapeInfoStart && instance.m_userData <= (ulong)actorInfo.m_ShapeInfoEnd)
                            {
                                // This is a shape we need!
                            }
                        }
                    }
                    break;
                }
            }
        }

        public void AddShape(hkpShape shape, uint hashId)
        {

        }

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
            var writer = new BinaryWriterEx(stream);
            var s = new PackFileSerializer();
            s.Serialize(Root, writer, Header);
        }
    }
}
