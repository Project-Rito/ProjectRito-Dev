using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using Toolbox.Core;
using BfresLibrary;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using MapStudio.UI;

namespace CafeLibrary.Rendering
{
    public class BfresMaterialAnim : STAnimation
    {
        private string ModelName = null;

        public List<string> TextureList = new List<string>();

        public BfresMaterialAnim() { }

        public BfresMaterialAnim(MaterialAnim anim, string name) {
            ModelName = name;
            CanPlay = false; //Default all animations to not play unless toggled in list
            Reload(anim);
        }

        public BfresMaterialAnim Clone() {
            BfresMaterialAnim anim = new BfresMaterialAnim();
            anim.Name = this.Name;
            anim.ModelName = this.ModelName;
            anim.FrameCount = this.FrameCount;
            anim.Frame = this.Frame;
            anim.Loop = this.Loop;
            anim.TextureList = this.TextureList;
            anim.AnimGroups = this.AnimGroups;
            return anim;
        }

        public BfresMaterialRender[] GetMaterials()
        {
            List<BfresMaterialRender> materials = new List<BfresMaterialRender>();

            if (!DataCache.ModelCache.ContainsKey(ModelName))
                return null;

            var models = ((BfresRender)DataCache.ModelCache[ModelName]).Models;
            if (models.Count == 0) return null;

            if (!((BfresRender)DataCache.ModelCache[ModelName]).InFrustum)
                return null;

            foreach (var model in models)
            {
                if (model.IsVisible)
                {
                    foreach (BfresMeshRender mesh in model.MeshList)
                        materials.Add((BfresMaterialRender)mesh.MaterialAsset);
                }
            }
            return materials.ToArray();
        }

        public override void NextFrame()
        {
            var materials = GetMaterials();
            if (materials == null)
                return;

            int numMats = 0;
            foreach (var mat in materials)
            {
                foreach (MaterialAnimGroup group in AnimGroups)
                {
                    if (group.Name != mat.Name)
                        continue;

                    ParseAnimationTrack(group, mat);
                    numMats++;
                }
            }
            if (numMats > 0)
                MapStudio.UI.AnimationStats.MaterialAnims += 1;
        }

        private void ParseAnimationTrack(STAnimGroup group, BfresMaterialRender mat)
        {
            foreach (var track in group.GetTracks())
            {
                if (track is SamplerTrack)
                    ParseSamplerTrack(mat, (SamplerTrack)track);
                if (track is ParamTrack)
                    ParseParamTrack(mat, group, (ParamTrack)track);
            }

            foreach (var subGroup in group.SubAnimGroups)
                ParseAnimationTrack(subGroup, mat);
        }

        public override NodeBase CreateNodeHierachy()
        {
            var animNode = new NodeBase(this.Name);
            animNode.Tag = this;
            return animNode;
        }

        private NodeBase CreateGroupHierachy(BfresMaterialRender material, STAnimGroup group)
        {
            if (group is ParamAnimGroup)
                return CreateParamNodeHierachy(material, (ParamAnimGroup)group);

            var groupNode = new NodeBase(group.Name);
            groupNode.Tag = group;

            //Params have their own sub group for each parameter
            //The X, Y, Z values being individual tracks
            foreach (STAnimGroup subGroup in group.SubAnimGroups)
                groupNode.AddChild(CreateGroupHierachy(material, subGroup));

            var tracks = group.GetTracks();
            return groupNode;
        }

        private NodeBase CreateParamNodeHierachy(BfresMaterialRender material, ParamAnimGroup group)
        {
            var groupNode = new NodeBase(group.Name);
            groupNode.Tag = group;
            var tracks = group.GetTracks();

            if (material != null)
            {
                List<ParamTrack> paramTracks = new List<ParamTrack>();

                var param = material.ShaderParams[group.Name];
                switch (param.Type)
                {
                    case ShaderParamType.TexSrt:
                    case ShaderParamType.TexSrtEx:
                        var texSrt = ((TexSrt)param.DataValue);
                        paramTracks.Add(new ParamTrack(0, (float)texSrt.Mode, "Mode"));
                        paramTracks.Add(new ParamTrack(4, (float)texSrt.Scaling.X, "Scale.X"));
                        paramTracks.Add(new ParamTrack(8, (float)texSrt.Scaling.Y, "Scale.Y"));
                        paramTracks.Add(new ParamTrack(12, (float)texSrt.Rotation, "Rotate"));
                        paramTracks.Add(new ParamTrack(16, (float)texSrt.Translation.X, "Position.X"));
                        paramTracks.Add(new ParamTrack(20, (float)texSrt.Translation.Y, "Position.X"));
                        break;
                    case ShaderParamType.Float:
                        paramTracks.Add(new ParamTrack(0, (float)param.DataValue, "Value"));
                        break;
                    case ShaderParamType.Float2:
                    case ShaderParamType.Float3:
                    case ShaderParamType.Float4:
                        var values = ((float[])param.DataValue);
                        string[] channel = new string[4] { "X", "Y", "Z", "W" };
                        for (int i = 0; i < values.Length; i++)
                            paramTracks.Add(new ParamTrack((uint)i * 4, values[i], channel[i]));
                        break;
                }

                for (int i = 0; i < paramTracks.Count; i++)
                {
                    var targetTrack = group.Tracks.FirstOrDefault(x => ((ParamTrack)x).ValueOffset == paramTracks[i].ValueOffset);
                    if (targetTrack == null)
                        group.Tracks.Add(paramTracks[i]);
                    else
                        targetTrack.Name = paramTracks[i].Name;
                }
            }

            foreach (ParamTrack track in group.Tracks.OrderBy(x => ((ParamTrack)x).ValueOffset))
            {
                track.ChannelIndex = ((int)track.ValueOffset / 4);

                var trackNode = new NodeBase(track.Name);
                trackNode.Tag = track;
                groupNode.AddChild(trackNode);
            }

            return groupNode;
        }

        private void ParseSamplerTrack(BfresMaterialRender material, SamplerTrack track)
        {
            if (TextureList.Count == 0)
                return;

            if (material.AnimatedSamplers.ContainsKey(track.Sampler))
                material.AnimatedSamplers.Remove(track.Sampler);

            var value = (int)track.GetFrameValue(this.Frame);
            var texture = TextureList[value];
            if (texture != null)
                material.AnimatedSamplers.Add(track.Sampler, texture);
        }

        private void ParseParamTrack(BfresMaterialRender material, STAnimGroup group, ParamTrack track)
        {
            if (!material.ShaderParams.ContainsKey(group.Name))
                return;

            var value = track.GetFrameValue(this.Frame);

            //4 bytes per float or int value
            uint index = track.ValueOffset / 4;

            var targetParam = material.ShaderParams[group.Name];

            var param = new ShaderParam();

            if (!material.AnimatedParams.ContainsKey(group.Name)) {
                if (targetParam.DataValue is float[]) {
                    float[] values = (float[])targetParam.DataValue;
                    float[] dest = new float[values.Length];
                    Array.Copy(values, dest, values.Length);
                    param.DataValue = dest;
                }
                else
                    param.DataValue = targetParam.DataValue;

                param.Type = targetParam.Type;
                param.Name = group.Name;

                material.AnimatedParams.Add(group.Name, param);
            }

            if (!material.AnimatedParams.ContainsKey(group.Name))
                return;

            param = material.AnimatedParams[group.Name];

            switch (targetParam.Type)
            {
                case ShaderParamType.Float: param.DataValue = (float)value; break;
                case ShaderParamType.Float2:
                case ShaderParamType.Float3:
                case ShaderParamType.Float4:
                    ((float[])param.DataValue)[index] = value;
                    break;
                case ShaderParamType.Int: param.DataValue = value; break;
                case ShaderParamType.Int2:
                case ShaderParamType.Int3:
                case ShaderParamType.Int4:
                    ((int[])param.DataValue)[index] = (int)value;
                    break;
                case ShaderParamType.TexSrt:
                case ShaderParamType.TexSrtEx:
                    {
                        TexSrtMode mode = ((TexSrt)param.DataValue).Mode;
                        var translateX = ((TexSrt)param.DataValue).Translation.X;
                        var translateY = ((TexSrt)param.DataValue).Translation.Y;
                        var rotate = ((TexSrt)param.DataValue).Rotation;
                        var scaleX = ((TexSrt)param.DataValue).Scaling.X;
                        var scaleY = ((TexSrt)param.DataValue).Scaling.Y;

                       // if (track.ValueOffset == 0) mode = (TexSrtMode)Convert.ToUInt32(value);
                        if (track.ValueOffset == 4) scaleX = value;
                        if (track.ValueOffset == 8) scaleY = value;
                        if (track.ValueOffset == 12) rotate = value;
                        if (track.ValueOffset == 16) translateX = value;
                        if (track.ValueOffset == 20) translateY = value;

                        param.DataValue = new TexSrt()
                        {
                            Mode = mode,
                            Scaling = new Syroot.Maths.Vector2F(scaleX, scaleY),
                            Translation = new Syroot.Maths.Vector2F(translateX, translateY),
                            Rotation = rotate,
                        };
                    }
                    break;
                case ShaderParamType.Srt2D:
                    {
                        var translateX = ((Srt2D)param.DataValue).Translation.X;
                        var translateY = ((Srt2D)param.DataValue).Translation.Y;
                        var rotate = ((Srt2D)param.DataValue).Rotation;
                        var scaleX = ((Srt2D)param.DataValue).Scaling.X;
                        var scaleY = ((Srt2D)param.DataValue).Scaling.Y;

                        if (track.ValueOffset == 0) scaleX = value;
                        if (track.ValueOffset == 4) scaleY = value;
                        if (track.ValueOffset == 8) rotate = value;
                        if (track.ValueOffset == 12) translateX = value;
                        if (track.ValueOffset == 16) translateY = value;

                        param.DataValue = new Srt2D()
                        {
                            Scaling = new Syroot.Maths.Vector2F(scaleX, scaleY),
                            Translation = new Syroot.Maths.Vector2F(translateX, translateY),
                            Rotation = rotate,
                        };
                    }
                    break;
            }
        }

        public void Reload(MaterialAnim anim)
        {
            Name = anim.Name;
            FrameCount = anim.FrameCount;
            FrameRate = 60.0f;
            Loop = anim.Loop;
            if (anim.TextureNames != null)
                TextureList = anim.TextureNames.Keys.ToList();

            if (anim.MaterialAnimDataList == null)
                return;

            AnimGroups.Clear();
            foreach (var matAnim in anim.MaterialAnimDataList) {
                var group = new MaterialAnimGroup();
                AnimGroups.Add(group);
                group.Name = matAnim.Name;

                //Get the material animation's texture pattern animation lists
                //Each sampler has their own info
                for (int i = 0; i < matAnim.PatternAnimInfos.Count; i++)
                {
                    var patternInfo = matAnim.PatternAnimInfos[i];

                    //Get the curve index for animated indices
                    int curveIndex = patternInfo.CurveIndex;
                    //Get the base index for starting values
                    int textureBaseIndex = matAnim.BaseDataList.Length > i ? matAnim.BaseDataList[i] : 0;

                    //Make a new sampler track using step interpolation
                    var samplerTrack = new SamplerTrack();
                    samplerTrack.InterpolationType = STInterpoaltionType.Step;
                    samplerTrack.Sampler = patternInfo.Name;
                    group.Tracks.Add(samplerTrack);

                    if (curveIndex != -1)
                        BfresAnimations.GenerateKeys(samplerTrack, matAnim.Curves[curveIndex], true);
                    else //Use the base data and make a constant key
                        samplerTrack.KeyFrames.Add(new STKeyFrame(0, textureBaseIndex));
                }
                //Get the list of animated parameters
                for (int i = 0; i < matAnim.ParamAnimInfos.Count; i++)
                {
                    ParamAnimGroup paramGroup = new ParamAnimGroup();
                    paramGroup.Name = matAnim.ParamAnimInfos[i].Name;
                    group.SubAnimGroups.Add(paramGroup);

                    var paramInfo = matAnim.ParamAnimInfos[i];
                    //Params have int and float curves
                    int curveIndex = paramInfo.BeginCurve;
                    int constantIndex = paramInfo.BeginConstant;
                    int numFloats = paramInfo.FloatCurveCount;
                    int numInts = paramInfo.IntCurveCount;
                    int numConstants = paramInfo.ConstantCount;

                    //Each constant and curve get's their own value using a value offset
                    for (int j = 0; j < numConstants; j++) {
                        var constant = matAnim.Constants[constantIndex + j];
                        float value = constant.Value;
                        //A bit hacky, convert int32 types by value range SRT modes use
                        if (constant.Value.Int32 > 0 && constant.Value.Int32 < 6)
                            value = constant.Value.Int32;

                        paramGroup.Tracks.Add(new ParamTrack()
                        {
                            Name = constant.AnimDataOffset.ToString("X"),
                            ValueOffset = constant.AnimDataOffset,
                            //Not the best way, but 4 is typically the stride size for each value
                            ChannelIndex = (int)(constant.AnimDataOffset / 4),
                            KeyFrames = new List<STKeyFrame>() { new STKeyFrame(0, value) },
                            InterpolationType = STInterpoaltionType.Constant,
                        });
                    }
                    //Loop through all int and float curve values
                    for (int j = 0; j < numInts + numFloats; j++)
                    {
                        var curve = matAnim.Curves[curveIndex + j];
                        var paramTrack = new ParamTrack() { Name = curve.AnimDataOffset.ToString("X") };
                        paramTrack.ValueOffset = curve.AnimDataOffset;
                        //Not the best way, but 4 is typically the stride size for each value
                        paramTrack.ChannelIndex = (int)(curve.AnimDataOffset / 4);
                        paramGroup.Tracks.Add(paramTrack);

                        BfresAnimations.GenerateKeys(paramTrack, curve);
                    }
                }
            }
        }

        public class MaterialAnimGroup : STAnimGroup
        {
            public List<STAnimationTrack> Tracks = new List<STAnimationTrack>();

            public override List<STAnimationTrack> GetTracks() { return Tracks; }
        }

        public class ParamAnimGroup : STAnimGroup
        {
            public List<STAnimationTrack> Tracks = new List<STAnimationTrack>();

            public override List<STAnimationTrack> GetTracks() { return Tracks; }

            public void RemoveKey(float frame)
            {
                foreach (var track in Tracks) {
                    track.RemoveKey(frame);
                }
            }

            public void InsertKey(float frame, int offset, float value, float slopeIn, float slopeOut)
            {
                var interpolation = STInterpoaltionType.Hermite;
                //Deternine what other tracks might be using and use that instead
                if (Tracks.Count > 0)
                    interpolation = Tracks.FirstOrDefault().InterpolationType;

                if (!Tracks.Any(x => ((BfresAnimationTrack)x).Name == offset.ToString("X")))
                    Tracks.Add(new ParamTrack() { Name = offset.ToString("X"), InterpolationType = interpolation, });

                var editedTrack = Tracks.FirstOrDefault(x => x.Name == offset.ToString("X"));
                if (editedTrack.InterpolationType == STInterpoaltionType.Hermite)
                {
                    editedTrack.Insert(new STHermiteKeyFrame()
                    {
                        Frame = frame,
                        Value = value,
                        TangentIn = slopeIn,
                        TangentOut = slopeOut,
                    });
                }
                else if (editedTrack.InterpolationType == STInterpoaltionType.Linear)
                {
                    editedTrack.Insert(new STKeyFrame()
                    {
                        Frame = frame,
                        Value = value,
                    });
                }
                else if (editedTrack.InterpolationType == STInterpoaltionType.Step)
                {
                    editedTrack.Insert(new STKeyFrame()
                    {
                        Frame = frame,
                        Value = value,
                    });
                }
            }
        }

        public class SamplerTrack : BfresAnimationTrack
        {
            /// <summary>
            /// The sampler to map to for texture mapping.
            /// </summary>
            public string Sampler { get; set; }
        }

        public class ParamTrack : BfresAnimationTrack
        {
            /// <summary>
            /// The offset value of the value offset in byte length.
            /// </summary>
            public uint ValueOffset { get; set; } 

            public ParamTrack() { }

            public ParamTrack(uint offset, float value, string name)
            {
                ValueOffset = offset;
                this.KeyFrames.Add(new STKeyFrame(0, value));
                Name = name;
            }
        }
    }
}
