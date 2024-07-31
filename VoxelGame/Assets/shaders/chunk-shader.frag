#version 450 core

out vec3 finalColour;

in vec4 colour;
in vec2 texcoord;

uniform sampler2D TestTexture;

void main() {
    finalColour = texture(TestTexture, texcoord).rgb;
}