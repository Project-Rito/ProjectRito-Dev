using Nintendo.Bfres;
using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.ViewModels;

namespace CafeLibrary.Rendering
{
    public class FSKL : STSkeleton
    {
        public FSKL(Skeleton skeleton) {
            Reload(skeleton);
        }

        public NodeBase[] CreateBoneHierarchy()
        {
            List<NodeBase> nodes = new List<NodeBase>();
            foreach (var bone in Bones)
                nodes.Add(new NodeBase(bone.Name) { Tag = bone, Icon = "/Images/Bone.png" });

            List<NodeBase> roots = new List<NodeBase>();
            foreach (var bone in Bones)
            {
                int index = Bones.IndexOf(bone);
                if (bone.ParentIndex != -1)
                    nodes[bone.ParentIndex].AddChild(nodes[index]);
                else
                    roots.Add(nodes[index]);
            }

            return roots.ToArray();
        }

        public void Reload(Skeleton Skeleton)
        {
            //Set the remap table
            RemapTable.Clear();
            if (Skeleton.MatrixToBoneList != null)
            {
                for (int i = 0; i < Skeleton.MatrixToBoneList.Count; i++)
                    RemapTable.Add(Skeleton.MatrixToBoneList[i]);
            }

            foreach (var bone in Skeleton.Bones.Values)
            {
                var genericBone = new STBone(this) {
                    Name = bone.Name,
                    ParentIndex = bone.ParentIndex,
                    Position = new OpenTK.Vector3(
                        bone.Position.X,
                        bone.Position.Y,
                        bone.Position.Z) * GLFrameworkEngine.GLContext.PreviewScale,
                    Scale = new OpenTK.Vector3(
                        bone.Scale.X,
                        bone.Scale.Y,
                        bone.Scale.Z),
                };

                if (Skeleton.FlagsScaling == SkeletonFlagsScaling.Maya)
                    genericBone.UseSegmentScaleCompensate = true;

                if (bone.FlagsRotation == BoneFlagsRotation.EulerXYZ)
                {
                    genericBone.EulerRotation = new OpenTK.Vector3(
                        bone.Rotation.X, bone.Rotation.Y, bone.Rotation.Z);
                }
                else
                    genericBone.Rotation = new OpenTK.Quaternion(
                         bone.Rotation.X, bone.Rotation.Y,
                         bone.Rotation.Z, bone.Rotation.W);

                Bones.Add(genericBone);
            }

            Reset();
        }
    }

}