#version 330 core
in vec3 color;

out vec4 fragOutput;

void main(){
	fragOutput = vec4(color, 1.0);
}