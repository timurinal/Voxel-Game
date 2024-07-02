#version 450 core

out vec4 finalColour;

in vec2 texcoord;

uniform float Sample;

uniform sampler2D _MainTex;

vec3 sampleMainTexture(vec2 uv);

vec3 blend(vec3 src, vec3 dest, float alpha);
vec4 blend(vec4 src, vec4 dest, float alpha);

void main() {
    finalColour = vec4(sampleMainTexture(texcoord), 1.0 / Sample);
}

vec3 sampleMainTexture(vec2 uv) {
    return texture(_MainTex, uv).rgb;
}

vec3 blend(vec3 src, vec3 dest, float alpha) {
    return (src * alpha) + (dest * (1.0 - alpha));
}

vec4 blend(vec4 src, vec4 dest, float alpha) {
    return (src * alpha) + (dest * (1.0 - alpha));
}