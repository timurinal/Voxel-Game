#version 450 core

out vec3 finalColour;

in vec4 colour;
in vec2 texcoord;

void main() {
    finalColour = vec3(texcoord, 0.0);
}