﻿#version 330 core
uniform vec4 color;
layout (location = 0) out vec4 fragOutput;

void main(){
    fragOutput = color;
}