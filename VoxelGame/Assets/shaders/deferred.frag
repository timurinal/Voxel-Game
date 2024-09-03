﻿#version 450 core

// #include "assets/shaders/chunk-lit.glsl"

#define MIN_SHADOW_INTENSITY 0.0

out vec3 finalColour;

in vec2 uv;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gAlbedo;
uniform sampler2D gMaterial;
uniform sampler2D gSpecular;
uniform sampler2D gDepth;

uniform sampler2D gAo;

uniform int ShadowsEnabled;
uniform int SoftShadows;

uniform int PlayerDynamicLight;

uniform vec3 viewPos;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_invView;

uniform mat4 m_lightProj;
uniform mat4 m_lightView;

uniform sampler2D shadowMap;

uniform vec3 LightDir;

float calcShadow(vec4 fragPosLightSpace, vec3 normal);

void main() {
    
//    finalColour = vec3(uv, 0.0);
//    return;
    
    vec3 albedo = texture(gAlbedo, uv).rgb;
    float depth = texture(gDepth, uv).r;
    
    if (depth >= 1.0) {
        finalColour = albedo;
        return;
    }

    vec3 position = texture(gPosition, uv).xyz;
    vec3 normal = normalize(texture(gNormal, uv).xyz);
    vec4 specularTex = texture(gSpecular, uv);
    
    vec3 viewDir = normalize(viewPos - position);
    vec3 reflectDir = reflect(-LightDir, normal);
    
    vec4 fragPosLightSpace = m_lightProj * m_lightView * vec4(position, 1.0);
    float shadow = calcShadow(fragPosLightSpace, normal);
    
    float ambientStrength = 0.2;
    vec3 ambient = ambientStrength * albedo;
    
    float diff = max(dot(normal, LightDir), 0.0);
    vec3 diffuse = diff * albedo;
    
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
    vec3 specular = specularTex.r * spec * vec3(1.0);

    vec3 result = ambient + shadow * (diffuse + specular);
    result = clamp(result, 0.0, 1.0);
    
    finalColour = result;
}

float pcfSoftShadow(vec2 uv, float currentDepth, float radius, float samples, float bias) {
    float shadow = 0.0;
    float diskRadius = radius / float(samples);

    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);

    int halfSamples = int(samples / 2.0);
    for (int i = -halfSamples; i < halfSamples; i++) {
        for (int j = -halfSamples; j < halfSamples; j++) {
            vec2 offset = diskRadius * vec2(float(i), float(j));
            float sampleDepth = texture(shadowMap, uv + offset * texelSize).r;
            shadow += (currentDepth < sampleDepth - bias ? 1.0 - MIN_SHADOW_INTENSITY : 0.0);
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

    float bias = max(0.0005 * tan(acos(dot(normal, LightDir))), 0.00005);

    float radius = 2.5;
    float samples = 15.0;
    float shadow = pcfSoftShadow(projCoords.xy, currentDepth - bias, radius, samples, bias);

    return shadow;
}