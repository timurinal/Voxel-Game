#version 450 core

out vec3 finalColour;

in vec2 uv;

uniform sampler2D gAlbedo;
uniform sampler2D gNormal;
uniform sampler2D gSpecular;
uniform sampler2D gDepth;

void main() {
    vec3 albedo = texture(gAlbedo, uv).rgb;
    
    float depth = texture(gDepth, uv).r;
    
    if (depth >= 1.0) {
        finalColour = albedo;
        return;
    }

    vec3 normal = normalize(texture(gNormal, uv).xyz);
    vec4 specular = texture(gSpecular, uv);

    vec3 ambient = 0.2 * albedo;

    vec3 lightDir = normalize(vec3(1, 1, 1));
    
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * albedo;
    
    vec3 result = ambient + diffuse;
    result = clamp(result, 0.0, 1.0);
    
    finalColour = result;
}