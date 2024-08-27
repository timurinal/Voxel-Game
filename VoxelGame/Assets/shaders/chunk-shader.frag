#version 450 core

#define EPSILON 1e-5

#include <assets/shaders/includes/triplanar.glsl>
#include <assets/shaders/includes/camera.glsl>

out vec3 finalColour;

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;

flat in vec3 vertexPos;
in vec3 fragPos;

in vec2 uv;

flat in int faceId;

uniform sampler2D TestTexture;
uniform sampler2D Normal;

uniform float Near;
uniform float Far;

uniform vec2 screenSize;

const float faceShading[6] = float[6](
    0.6, 0.8,  // front back
    1.0, 0.4,  // top bottom
    0.5, 0.7   // right left
);

void main() {
    
    vec2 screenspaceUV = gl_FragCoord.xy / screenSize;
    
    vec3 norm = normal;
    vec3 sampledNormal = texture(Normal, uv).rgb;
    sampledNormal = sampledNormal * 2.0 - 1.0; // normalize to [-1, 1]
    norm = normalize(tangent * sampledNormal.x +
    bitangent * sampledNormal.y +
    normal * sampledNormal.z);

    float normalMapInfluence = 0.75;
    norm = normalize(mix(normal, norm, normalMapInfluence));
    
    vec3 lightPos = vec3(1, 1, 1);
    vec3 lightDir = normalize(vec3(1, 1, 1));

    vec3 texCol = texture(TestTexture, uv).rgb;
    vec3 ambient = 0.2 * texCol;

    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * texCol;

    vec3 result = ambient + diffuse;
    result = clamp(result, 0.0, 1.0);

    finalColour = result;
}