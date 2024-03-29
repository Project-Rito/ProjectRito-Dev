﻿#version 330 core

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec2 vTexCoord;
layout(location = 3) in vec4 vColor;

uniform mat4[64] mtxMdl;
uniform mat4 mtxCam;
uniform mat4 mtxView;
uniform mat4 mtxProj;

out vec2 f_texcoord0;
out vec3 f_normal;
out vec4 f_color;

void main()
{
    f_texcoord0 = vTexCoord;
    f_normal = normalize(mat3(mtxMdl[gl_InstanceID]) * vNormal.xyz);
    f_color = vColor;
    gl_Position = mtxCam*mtxMdl[gl_InstanceID]*vec4(vPosition.xyz, 1.0);
}