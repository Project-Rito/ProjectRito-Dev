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
    public class ActorCollisionLoader
    {
        public NodeBase RootNode;

        private HKXHeader Header = HKXHeader.BotwWiiu(); // TODO - actually get the right platform
        private hkRootLevelContainer Root;

        private STFileLoader.Settings FileSettings;

        public void Load(Stream stream, string fileName)
        {
            FileSettings = STFileLoader.TryDecompressFile(stream, fileName);

            List<IHavokObject> roots = Util.ReadBotwHKX(FileSettings.Stream.ReadAllBytes(), ".hkrb");

            Root = (hkRootLevelContainer)roots[0];

            RootNode = new NodeBase(fileName);
            RootNode.Tag = this;
        }

        public hkpShape[] GetShapes()
        {
            List<hkpShape> shapes = new List<hkpShape>();

            foreach (hkpRigidBody rigidBody in ((hkpPhysicsData)Root.m_namedVariants[0].m_variant).m_systems[0].m_rigidBodies)
                shapes.Add(rigidBody.m_collidable.m_shape);

            return shapes.ToArray();
        }

        public void AddShape(hkpShape shape)
        {

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
            Util.WriteBotwHKX(new IHavokObject[] { Root }, Header, ".hkrb", uncompressed);

            uncompressed.Position = 0;
            stream.Position = 0;
            FileSettings.CompressionFormat.Compress(uncompressed).CopyTo(stream);
            stream.SetLength(stream.Position + 1);
        }
    }
}
