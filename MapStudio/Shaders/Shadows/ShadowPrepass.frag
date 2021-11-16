#version 330

uniform sampler2D shadowMap;
uniform sampler2D depthTexture;
uniform sampler2D normalsTexture;

uniform mat4 mtxViewProjInv;
uniform mat4 mtxLightVP;

uniform vec3 lightPos;
uniform vec3 viewPos;
uniform float shadowBias;

in vec2 TexCoords;

out vec4 fragOutput;

const float pcfKernel3x3[9] = float[](
0.102059,	0.115349,	0.102059,
0.115349,	0.130371,	0.115349,
0.102059,	0.115349,	0.102059
);

vec4 CalculatePosition(vec2 texture_coordinate, float depth)
{
    float z = depth * 2.0 - 1.0;

    vec4 clipSpacePosition = vec4(texture_coordinate * 2.0 - 1.0, z, 1.0);
    //Invert the view/proj space
    vec4 viewSpacePosition = mtxViewProjInv * clipSpacePosition;
    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    //Get the position in light space
    return vec4(viewSpacePosition.xyz, 1.0);
}

float CalculateShadow(vec3 fragPos, vec4 fragPosLightSpace)
{
    // Perspective division
   vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
   //Adjust bias
   projCoords.z -= shadowBias;
    // Transform to [0,1] range
   projCoords = projCoords * 0.5 + 0.5;
    // Get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;

    float shadow = 0.0;
	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			float texelDepth = textureOffset(shadowMap, projCoords.xy, ivec2(x, y)).r;
			shadow += (currentDepth > texelDepth) ? 0.0 : pcfKernel3x3[x + 1 + (y + 1) * 3];
 		}
	}

    if(projCoords.z > 1.0)
        shadow = 1.0;

   return shadow;
}

void main()
{            
   float depth = texture(depthTexture, TexCoords).r;
   vec4 fragPos = CalculatePosition(TexCoords, depth);
   vec4 fragPosLightSpace = mtxLightVP * fragPos;
   float shadow = CalculateShadow(fragPos.xyz, fragPosLightSpace);


   float ambientOcc = 1.0;
   float staticShadow = 1.0;

   fragOutput.r = shadow;
   fragOutput.g = staticShadow;
   fragOutput.b = ambientOcc;
   fragOutput.a = 1.0;
}  