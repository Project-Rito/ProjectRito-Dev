#version 330 core

layout (location = 0) in vec3 vPosition;

uniform mat4[32] mtxMdl;
uniform mat4 mtxCam;

void main(){
    vec4 worldPosition = vec4(vPosition.xyz, 1);
    gl_Position = mtxCam * mtxMdl[gl_InstanceID] * worldPosition;
}