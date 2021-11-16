#version 330

uniform sampler2DArray texTerrain;

in vec4 fTexCoords;
in vec3 fNormals;

in float materialWeight;

flat in vec2 texIndex;

out vec4 fragColor;

void main(void)
{
    vec4 color0 = texture(texTerrain, vec3(fTexCoords.xy, texIndex[0]));
    vec4 color1 = texture(texTerrain, vec3(fTexCoords.zw, texIndex[1]));

    fragColor = mix(color0, color1, materialWeight);
}