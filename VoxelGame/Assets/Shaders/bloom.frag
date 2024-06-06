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
    float sceneDepthNonLinear = sampleDepthTexture(texcoord);
    float sceneDepth = linearizeDepth(sceneDepthNonLinear, NearPlane, FarPlane);
    
    finalColour = vec4(sampleMainTexture(texcoord), 0.0);
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