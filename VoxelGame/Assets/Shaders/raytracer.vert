﻿#version 450 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec2 vUv;

out vec2 texcoord;
out vec3 pos;

void main() {
    gl_Position = vec4(vPosition, 1.0);
    texcoord = vUv;
    pos = vPosition;
}