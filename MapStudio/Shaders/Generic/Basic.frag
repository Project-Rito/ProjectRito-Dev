#version 330 core

in vec2 f_texcoord0;
in vec3 f_normal;
in vec4 f_color;

uniform sampler2D textureMap;

uniform int displayVertexColors;
uniform int hasVertexColors;
uniform int hasTextures;
uniform int halfLambert;
uniform vec4 color;
uniform vec4 highlight_color;
uniform vec3 difLightDirection;

out vec4 fragColor;

void main()
{
	fragColor = color;

	 if (hasTextures == 1)
		fragColor = texture(textureMap, f_texcoord0).rgba;
	if (halfLambert == 1)
	{
		float halfLambert = dot(f_normal, difLightDirection) * 0.5 + 0.5;
		fragColor.rgb *= vec3(halfLambert); 
	}
	if (highlight_color.w > 0.0)
	{
		//Highlight intensity for object selection
		float hc_a   = highlight_color.w;
		fragColor = vec4(fragColor.rgb * (1-hc_a) + highlight_color.rgb * hc_a, fragColor.a);
	}

	if (hasVertexColors == 1)
		fragColor.rgb *= f_color.rgb;

	if (displayVertexColors == 1)
		fragColor.rgba = f_color.rgba;
}