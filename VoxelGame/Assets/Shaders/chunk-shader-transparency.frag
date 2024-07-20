#version 450 core

out vec4 finalColour;

uniform sampler2D Albedo;

in vec2 texcoord;
in vec3 normal;
in vec3 fragPos;

void main() {
    finalColour = texture(Albedo, texcoord);
}