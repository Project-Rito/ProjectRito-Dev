using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Toolbox.Core.IO;
using OpenTK;

namespace UKingLibrary
{
    public class ProdInfo
    {
        Header HeaderData;

        public Actor[] Actors;

        public ProdInfo(Stream stream)
        {
            using (var reader = new FileReader(stream)) {
                Read(reader);
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new FileWriter(stream)) {
                Write(writer);
            }
        }

        public void Read(FileReader reader)
        {
            reader.SetByteOrder(true);

            HeaderData = reader.ReadStruct<Header>();

            Actors = new Actor[HeaderData.NumEntries];
            for (int i = 0; i < HeaderData.NumEntries; i++)
            {
                Actors[i] = new Actor();

                uint size = reader.ReadUInt32();
                uint numInstances = reader.ReadUInt32();
                uint nameOffset = reader.ReadUInt32();
                reader.ReadUInt32(); //padding

                Actors[i].Instances = new Instance[numInstances];
                for (int j = 0; j < numInstances; j++)
                {
                    Instance instance = new Instance();
                    instance.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    instance.Rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    instance.Scale = reader.ReadSingle();
                    reader.ReadUInt32(); //padding
                    Actors[i].Instances[j] = instance;
                }

                using (reader.TemporarySeek(HeaderData.StringTableOffset + nameOffset, SeekOrigin.Begin)) {
                    Actors[i].Name = reader.ReadZeroTerminatedString();
                }
            }
        }

        public void Write(FileWriter writer)
        {
            HeaderData.NumEntries = (uint)Actors.Length;

            writer.SetByteOrder(true);
            writer.WriteStruct(HeaderData);

            Dictionary<uint, string> stringTable = new Dictionary<uint, string>();

            long actorsPos = writer.Position;
            for (int i = 0; i < Actors.Length; i++)
            {
                writer.Write(Actors[i].Instances.Length * 32); //Total instances size
                writer.Write(Actors[i].Instances.Length);
                stringTable.Add((uint)writer.Position, Actors[i].Name);
                writer.Write(0); //reserved name offset
                writer.Write(0); //padding
                for (int j = 0; j < Actors[i].Instances.Length; j++)
                {
                    writer.Write(Actors[i].Instances[j].Position);
                    writer.Write(Actors[i].Instances[j].Rotation);
                    writer.Write(Actors[i].Instances[j].Scale);
                    writer.Write(0); //padding
                }
            }

            //string table offset
            writer.BaseStream.Position = 0x0C;
            writer.Write((int)writer.BaseStream.Length - 8);

            //Save string table
            writer.BaseStream.Position = 0x18;
            writer.Write((int)writer.BaseStream.Length);

            //string table
            long strigTablePos = writer.Position;

            writer.Write(Actors.Length);
            writer.Write(0); //reserve for string table size
            foreach (var str in stringTable)
            {
                writer.WriteUint32Offset(str.Key, strigTablePos);
                writer.WriteString(str.Value);
                writer.Align(4);
            }
            //String table size 
            using (writer.TemporarySeek(strigTablePos + 4, SeekOrigin.Begin)) {
                writer.Write((uint)(writer.BaseStream.Length - strigTablePos));
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Header
        {
            public Magic Magic = "PrOD";
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public uint FileSize;
            public uint NumEntries;
            public uint StringTableOffset;
            public uint Padding;
        }

        public class Actor
        {
            public string Name { get; set; }

            public Instance[] Instances { get; set; }
        }

        public class Instance
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public float Scale;
        }
    }
}
