#version 450 core

#define EPSILON 1e-5

out vec3 finalColour;

in vec3 normal;
in vec2 texcoord;

uniform sampler2D TestTexture;

const float faceShading[6] = float[6](
    0.6, 0.8,  // front back
    1.0, 0.4,  // top bottom
    0.5, 0.7   // right left
);

void main() {
    int faceId = 0;
    if (length(normal - vec3(0, 0, -1)) < EPSILON) faceId = 0;
    else if (length(normal - vec3(0, 0, 1)) < EPSILON) faceId = 1;
    else if (length(normal - vec3(0, 1, 0)) < EPSILON) faceId = 2;
    else if (length(normal - vec3(0, -1, 0)) < EPSILON) faceId = 3;
    else if (length(normal - vec3(1, 0, 0)) < EPSILON) faceId = 4;
    else if (length(normal - vec3(-1, 0, 0)) < EPSILON) faceId = 5;
    
    finalColour = vec3(texture(TestTexture, texcoord).rgb) * faceShading[faceId];
//    finalColour = vec3(texcoord, 0.0);
}