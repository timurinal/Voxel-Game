#version 450 core

out vec4 finalColour;

in vec2 texcoord;

uniform float NearPlane;
uniform float FarPlane;

uniform sampler2D _MainTex;
uniform sampler2D _DepthTexture;

float sampleDepthTexture(vec2 uv);
vec3 sampleMainTexture(vec2 uv);
float linearizeDepth(float nonLinearDepth, float near, float far);

void main() {
    
    const float gamma = 0.8;
    const float exposure = 2.2;
    vec3 hdrColour = sampleMainTexture(texcoord);

    // exposure tonemapping
    vec3 mapped = vec3(1.0) - exp(-hdrColour * exposure);
    // gamma correction
    mapped = pow(mapped, vec3(1.0 / gamma));

    finalColour = vec4(mapped, 1.0);
}

float sampleDepthTexture(vec2 uv) {
    return texture(_DepthTexture, uv).r;
}

vec3 sampleMainTexture(vec2 uv) {
    return texture(_MainTex, uv).rgb;
}

float linearizeDepth(float nonLinearDepth, float near, float far) {
    float z = nonLinearDepth;
    return (2.0 * near * far) / (far + near - z * (far - near));
}