#version 330 core

layout (location = 0) in vec3 vPositon;

uniform mat4[32] mtxMdl;
uniform mat4 mtxCam;

void main(){
    vec4 worldPosition = vec4(vPositon.xyz, 1);
    gl_Position = mtxMdl[gl_InstanceID] * mtxCam * worldPosition;
}