#version 450 core

layout (location = 0) out vec3 gAlbedo;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec4 gSpecular;

in vec3 normal;

in vec2 uv;

uniform float Smoothness;

uniform sampler2D Albedo;
uniform sampler2D Normal;
uniform sampler2D Specular;

void main() {
    vec3 albedo = textureLod(Albedo, uv, 0).rgb;
    vec3 specular = textureLod(Specular, uv, 0).rgb;
    
    gAlbedo = albedo;
    gNormal = normal;
    gSpecular = vec4(specular.rgb, Smoothness);
}