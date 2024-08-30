#version 450 core

#define MIN_SHADOW_INTENSITY 0.5

out vec3 finalColour;

in vec2 uv;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gAlbedo;
uniform sampler2D gSpecular;
uniform sampler2D gDepth;

uniform int ShadowsEnabled;
uniform int SoftShadows;

uniform mat4 m_lightProj;
uniform mat4 m_lightView;

uniform sampler2D shadowMap;

uniform vec3 LightDir;

float calcShadow(vec4 fragPosLightSpace, vec3 normal);

void main() {
    vec3 albedo = texture(gAlbedo, uv).rgb;
    float depth = texture(gDepth, uv).r;
    
    if (depth >= 1.0) {
        finalColour = albedo;
        return;
    }

    vec3 position = texture(gPosition, uv).xyz;
    vec3 normal = normalize(texture(gNormal, uv).xyz);
    vec4 specular = texture(gSpecular, uv);

    vec3 ambient = 0.2 * albedo;
    
    float diff = max(dot(normal, LightDir), 0.0);
    vec3 diffuse = diff * albedo;
    
    if (ShadowsEnabled == 1) {
        vec4 fragPosLightSpace = m_lightProj * m_lightView * vec4(position, 1.0);
        
        float shadow = calcShadow(fragPosLightSpace, normal);

        vec3 result = ambient + shadow * (diffuse);
        result = clamp(result, 0.0, 1.0);
        
        finalColour = result * shadow;
        return;
    }

    vec3 result = ambient + diffuse;
    result = clamp(result, 0.0, 1.0);
    
    finalColour = result;
}

float pcfSoftShadow(vec2 uv, float currentDepth, float radius, float samples) {
    float shadow = 0.0;
    float diskRadius = radius / float(samples);

    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);

    int halfSamples = int(samples / 2.0);
    for (int i = -halfSamples; i < halfSamples; i++) {
        for (int j = -halfSamples; j < halfSamples; j++) {
            vec2 offset = diskRadius * vec2(float(i), float(j));
            float sampleDepth = texture(shadowMap, uv + offset * texelSize).r;
            shadow += (currentDepth < sampleDepth ? 1.0 : 0.0);
        }
    }
    shadow /= samples * samples;
    return shadow;
}

float calcShadow(vec4 fragPosLightSpace, vec3 normal) {
    // Perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    // Transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;

    // Sample the shadow map
    float currentDepth = projCoords.z;
    
    float bias = max(0.0001 * (1.0 - dot(normal, LightDir)), 0.00001);

    float radius = 5.0;
    float samples = 25.0;
    float shadow = pcfSoftShadow(projCoords.xy, currentDepth - bias, radius, samples);

    return clamp(shadow, MIN_SHADOW_INTENSITY, 1.0);
}