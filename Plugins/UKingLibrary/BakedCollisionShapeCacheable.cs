using System;
using HKX2;
using HKX2Builders;

namespace UKingLibrary
{
    public class BakedCollisionShapeCacheable
    {
        public hkpShape Shape
        {
            get
            {
                return Instance.m_shape;
            }
        }
        public hkpStaticCompoundShapeInstance Instance
        {
            get
            {
                return ((hkpStaticCompoundShape)RigidBody.m_collidable.m_shape).m_instances[0];
            }
        }

        public hkpRigidBody RigidBody;
        public int SystemIndex; // As far as I can tell, the game references systems by index. Probably why there are always 17 systems.
        public sbyte BodyGroup;
        public byte BodyLayerType;
        public bool NullActorInfoPtr;
    }
}
