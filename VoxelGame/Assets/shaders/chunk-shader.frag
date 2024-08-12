#version 450 core

#define EPSILON 1e-5

out vec3 finalColour;

in vec2 texcoord;

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;

in vec3 fragPos;

uniform sampler2D TestTexture;
uniform sampler2D Normal;

const float faceShading[6] = float[6](
    0.6, 0.8,  // front back
    1.0, 0.4,  // top bottom
    0.5, 0.7   // right left
);

void main() {
    
    finalColour = normal * 0.5 + 0.5;
    return;
    
    vec3 norm = normal;
    vec3 sampledNormal = texture(Normal, texcoord).xyz;
    sampledNormal = sampledNormal * 2.0 - 1.0; // normalize to [-1, 1]
    norm = normalize(tangent * sampledNormal.x +
    bitangent * sampledNormal.y +
    normal * sampledNormal.z);

    vec3 lightPos = vec3(1, 1, 1);
    vec3 lightDir = normalize(lightPos - fragPos);

    vec3 ambient = 0.2 * texture(TestTexture, texcoord).rgb;
    
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * texture(TestTexture, texcoord).rgb;
    
    vec3 result = ambient + diffuse;
    result = clamp(result, 0.0, 1.0);
    
    finalColour = result;
}