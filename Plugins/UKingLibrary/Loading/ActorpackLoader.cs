using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CafeLibrary;
using HKX2;

namespace UKingLibrary
{
    // Unused and unfinished
    // No reason yet to load this in the map editor & would impact loading performance
    // Would be useful if/when actor editor is implemented
    public class ActorpackLoader
    {
        public SARC Pack;

        private ActorCollisionLoader _rigidBodyCollision;
        public ActorCollisionLoader RigidBodyCollision
        {
            get
            {
                if (_rigidBodyCollision != null)
                    return _rigidBodyCollision;

                byte[] data = Pack.SarcData.Files["Physics/RigidBody/"];

                _rigidBodyCollision = new ActorCollisionLoader();
                //_rigidBodyCollision.Load()
                return null;
            }
            set
            {
                _rigidBodyCollision = value;
            }
        }


        public void Load(Stream stream, string name)
        {
            Pack = new SARC();
            Pack.Load(stream, name);
        }
    }
}