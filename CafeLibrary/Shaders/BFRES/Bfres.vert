﻿#version 330 core

#define PI 3.1415926538

precision mediump float;

struct EnvLightParam {
    vec4 BacksideColor;
    vec4 DiffuseColor;
    vec4 Direction;
};
layout(std140) uniform ub_MaterialParams {
    mat3x4 u_TexCoordSRT0;
    vec4 u_TexCoordBake0ScaleBias;
    vec4 u_TexCoordBake1ScaleBias;
    mat3x4 u_TexCoordSRT1;
    mat3x4 u_TexCoordSRT2;
    mat3x4 u_TexCoordSRT3;
    vec4 u_AlbedoColorAndTransparency;
    vec4 u_EmissionColorAndNormalMapWeight;
    vec4 u_SpecularColorAndIntensity;
    vec4 u_BakeLightScaleAndRoughness;
    vec4 u_MultiTexReg[3];
    vec4 u_Misc[1];
    EnvLightParam u_EnvLightParams[2];

    float u_uking_wind_vtx_transform_intensity;
    float u_uking_wind_vtx_transform_lie_intensity;
    float u_uking_wind_vtx_transform_lie_height;
};

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoord0;
layout (location = 3) in vec2 vTexCoord1;
layout (location = 4) in vec2 vTexCoord2;
layout (location = 5) in vec2 vTexCoord3;
layout (location = 6) in vec4 vColor;
layout (location = 7) in ivec4 vBoneIndex;
layout (location = 8) in vec4 vBoneWeight;
layout (location = 9) in vec4 vTangent;
layout (location = 10) in vec4 vBitangent;

// GL
uniform mat4[64] mtxMdl;
uniform mat4 mtxCam;
uniform float previewScale;
uniform float millisecond;

// Skinning uniforms
uniform mat4 bones[170];
uniform int SkinCount;
uniform int UseSkinning;
uniform int BoneIndex;
uniform mat4 RigidBindTransform;

out vec3 v_PositionWorld;
out vec2 v_TexCoord0;
out vec2 v_TexCoord1;
out vec2 v_TexCoord2;
out vec2 v_TexCoord3;
out vec4 v_VtxColor;
out vec3 v_NormalWorld;
out vec4 v_TangentWorld;

// Defined in Utility.frag
vec3 SRT_Scale(mat3x4 matrix);
vec3 SRT_Rot(mat3x4 matrix);
vec3 SRT_Trans(mat3x4 matrix);

vec2 CalcScaleBias(in vec2 t_Pos, in vec4 t_SB) {
    return t_Pos.xy * t_SB.xy + t_SB.zw;
}

vec4 skin(vec3 pos, ivec4 index)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);
    if (SkinCount == 1) //Rigid
    {
        newPosition = bones[index.x] * vec4(pos, 1.0);
    }
    else //Smooth
    {
        newPosition = bones[index.x] * vec4(pos, 1.0) * vBoneWeight.x;
        newPosition += bones[index.y] * vec4(pos, 1.0) * vBoneWeight.y;
        newPosition += bones[index.z] * vec4(pos, 1.0) * vBoneWeight.z;
        if (vBoneWeight.w < 1) //Necessary. Bones may scale weirdly without
		    newPosition += bones[index.w] * vec4(pos, 1.0) * vBoneWeight.w;
    }
    return newPosition;
}

vec3 skinNRM(vec3 nr, ivec4 index)
{
    vec3 newNormal = vec3(0);
    if (SkinCount == 1) //Rigid
    {
	    newNormal =  mat3(bones[index.x]) * nr;
    }
    else //Smooth
    {
	    newNormal =  mat3(bones[index.x]) * nr * vBoneWeight.x;
	    newNormal += mat3(bones[index.y]) * nr * vBoneWeight.y;
	    newNormal += mat3(bones[index.z]) * nr * vBoneWeight.z;
	    newNormal += mat3(bones[index.w]) * nr * vBoneWeight.w;
    }
    return newNormal;
}

vec3 uking_wind_vtx_transform(vec3 pos, vec3 origin, vec3 normal) {
    vec3 offset;

    float t = float(millisecond) / 1000.f; // Scale down the time to (floating-point) seconds.

    offset.x = sin(t * 1);
    offset.y = sin(t * 2);
    offset.z = sin(t * 3);

    float distFromCenter = length(pos - origin) / previewScale;

    float windCatch = 1.f - normalize(normal).y;
    float scale = u_uking_wind_vtx_transform_intensity * vColor.b * distFromCenter * 0.05f * windCatch; // The 0.1 is just for fun.

    return pos + (offset * scale);
}

void main(){
    vec4 worldPosition = vec4(uking_wind_vtx_transform(vPosition.xyz, vec3(0.f), vNormal.xyz), 1);
    vec3 normal = normalize(mat3(mtxMdl[gl_InstanceID]) * vNormal.xyz);

    //Vertex Rigging
    if (UseSkinning == 1) //Animated object using the skeleton
    {
        ivec4 index = vBoneIndex;

        //Apply skinning to vertex position and normal
	    if (SkinCount > 0) {
		    worldPosition = skin(worldPosition.xyz, index);
		    normal = normalize(mat3(mtxMdl[gl_InstanceID]) * (skinNRM(vNormal.xyz, index)).xyz);
        }
        //Single bind models that have no skinning to the bone they are mapped to
        else if (SkinCount == 0)
        {
            worldPosition = RigidBindTransform * worldPosition;
            normal = mat3(RigidBindTransform) * normal;
        }
    }

    vec3 fragPosition = (mtxMdl[gl_InstanceID] * worldPosition).xyz;
    gl_Position = mtxCam * vec4(fragPosition, 1);

    v_PositionWorld = fragPosition.xyz;

    v_TexCoord0 = mat4x2(u_TexCoordSRT0) * vec4(vTexCoord0, 1.0, 1.0);
    v_TexCoord1 = mat4x2(u_TexCoordSRT1) * vec4(vTexCoord1, 1.0, 1.0); // I'm not sure what's supposed to go on here..
    v_TexCoord2 = mat4x2(u_TexCoordSRT2) * vec4(vTexCoord2, 1.0, 1.0); 
    v_TexCoord3 = mat4x2(u_TexCoordSRT3) * vec4(vTexCoord3, 1.0, 1.0);
    v_VtxColor = vColor;
    v_NormalWorld.xyz = normalize(normal.xyz);
    v_TangentWorld.xyzw = vTangent.xyzw;
}