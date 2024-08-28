#version 450 core

#define EPSILON 1e-5

out vec3 finalColour;

in vec3 normal;
in vec3 tangent;
in vec3 bitangent;

in vec3 fragPos;

in vec2 uv;

uniform sampler2D TestTexture;
uniform sampler2D Normal;

uniform float Near;
uniform float Far;

uniform vec2 screenSize;

uniform vec3 viewPos;

void main() {
    
//    finalColour = vec3(uv, 0.0);
//    return;
    
    vec3 viewDir = normalize(fragPos - viewPos);
    
    vec2 screenspaceUV = gl_FragCoord.xy / screenSize;
    
    vec3 norm = normal;
    vec3 sampledNormal = texture(Normal, uv).rgb;
    sampledNormal = sampledNormal * 2.0 - 1.0; // normalize to [-1, 1]
    norm = normalize(tangent * sampledNormal.x +
    bitangent * sampledNormal.y +
    normal * sampledNormal.z);

    float normalMapInfluence = 0.0;
    norm = normalize(mix(normal, norm, normalMapInfluence));
    
    vec3 lightPos = vec3(1, 1, 1);
    vec3 lightDir = normalize(vec3(1, 1, 1));

    vec3 texCol = textureLod(TestTexture, uv, 0).rgb;
    vec3 ambient = 0.2 * texCol;

    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * texCol;

    vec3 result = ambient + diffuse;
    result = clamp(result, 0.0, 1.0);

    finalColour = result;
}