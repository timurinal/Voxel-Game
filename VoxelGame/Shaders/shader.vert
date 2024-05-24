#version 330 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec4 vColour;

out vec4 fragColour;

void main() {
    gl_Position = vec4(vPosition, 1.0);
    
    fragColour = vColour;
}