#version 330 core

in vec3 vPosition;
in uint vMaterialIndex;
in vec2 vTexCoord;
in vec3 vNormal;

out vec2 fTexCoords;
out vec3 fNormals;
out vec4 fTangentWorld;
flat out uint texIndex;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    texIndex = vMaterialIndex;

    fTexCoords = vTexCoord;
    fNormals = vNormal;

    gl_Position = mtxCam * mtxMdl * vec4(vPosition, 1.0);
}