﻿#version 330 core

in vec3 vPosition;
in vec3 vMaterialMap;
in vec4 vTexCoord;
in vec3 vNormalWorld;
in vec3 vTangentWorld;
in vec3 vDebugHighlight;


out vec4 v_TexCoords;
out vec3 v_NormalWorld;
out vec3 v_TangentWorld;
out float materialWeight;
out vec3 v_PositionWorld;
flat out vec2 texIndex;
out vec3 v_DebugHighlight;

uniform mat4[64] mtxMdl;
uniform mat4 mtxCam;

void main()
{
    texIndex = vMaterialMap.rg;
    materialWeight   = vMaterialMap.b / 255.0;

    v_TexCoords = vTexCoord;
    v_NormalWorld = vNormalWorld;
    v_TangentWorld = vTangentWorld;
    v_PositionWorld = (mtxMdl[gl_InstanceID] * vec4(vPosition, 1.0)).xyz;

    v_DebugHighlight = vDebugHighlight;

    gl_Position = mtxCam * mtxMdl[gl_InstanceID] * vec4(vPosition, 1.0);
}