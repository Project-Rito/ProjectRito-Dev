#version 330 core

uniform mat4 mtxCam;
uniform mat4[64] mtxMdl;

uniform vec2 pos;
uniform float scale;

in vec2 vPosition;
in vec4 vColor;

out vec4 fColor;
out vec2 fragUV;
flat out float barEndPos;

void main() {
   
   fragUV = vPosition.xy;
   fColor = vColor,
    gl_Position = mtxCam*mtxMdl[gl_InstanceID]*vec4(pos + vPosition.xy * scale, 0.0, 1.0);
}