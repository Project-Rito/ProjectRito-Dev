﻿#version 330 core

uniform sampler2D u_TextureAlbedo0;
uniform int hasAlpha;
uniform int u_TextureAlbedo0_Enabled;

in vec2 f_texcoord0;

out vec4 fragOutput;

void main()
{
	if (hasAlpha == 1 && u_TextureAlbedo0_Enabled == 1)
	{
		float alpha = texture(u_TextureAlbedo0, f_texcoord0).a;
		if (alpha < 0.5)
			discard;
	}
}  