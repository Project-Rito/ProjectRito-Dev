#version 330 core

in vec3 vPosition;
in vec3 vMaterialMap;
in vec4 vTexCoord;
in vec3 vNormal;

out vec4 fTexCoords;
out vec3 fNormals;
out vec4 fTangentWorld;
out float materialWeight;
flat out vec2 texIndex;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    texIndex = vMaterialMap.rg;
    materialWeight   = vMaterialMap.b / 255.0;

    fTexCoords = vTexCoord;
    fNormals = vNormal;

    gl_Position = mtxCam * mtxMdl * vec4(vPosition, 1.0);
}