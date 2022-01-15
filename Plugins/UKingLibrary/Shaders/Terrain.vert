#version 330 core

in vec3 vPosition;
in vec3 vMaterialMap;
in vec4 vTexCoord;
in vec3 vNormalWorld;
in vec3 vTangentWorld;

out vec4 v_TexCoords;
out vec3 v_NormalWorld;
out vec3 v_TangentWorld;
out float materialWeight;
flat out vec2 texIndex;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    texIndex = vMaterialMap.rg;
    materialWeight   = vMaterialMap.b / 255.0;

    v_TexCoords = vTexCoord;
    v_NormalWorld = vNormalWorld;
    v_TangentWorld = vTangentWorld;

    gl_Position = mtxCam * mtxMdl * vec4(vPosition, 1.0);
}