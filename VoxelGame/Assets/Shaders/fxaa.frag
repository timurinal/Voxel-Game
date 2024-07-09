#version 450 core

out vec4 finalColour;

in vec2 texcoord;

uniform float NearPlane;
uniform float FarPlane;

uniform sampler2D _MainTex;
uniform sampler2D _DepthTexture;

float sampleDepthTexture(sampler2D depth, vec2 uv);
float linearizeDepth(float nonLinearDepth, float near, float far);

// FXAA settings
uniform vec2 screenSize;

#define FXAA_REDUCE_MIN   (1.0/128.0)
#define FXAA_REDUCE_MUL   (1.0/8.0)
#define FXAA_SPAN_MAX     8.0

vec4 applyFXAA(sampler2D tex, vec2 fragCoord);

void main() {
    vec2 fragCoord = texcoord * screenSize;
    finalColour = applyFXAA(_MainTex, fragCoord);
}

float sampleDepthTexture(sampler2D depth, vec2 uv) {
    return texture(depth, uv).r;
}

float linearizeDepth(float nonLinearDepth, float near, float far) {
    float z = nonLinearDepth;
    return (2.0 * near * far) / (far + near - z * (far - near));
}

vec4 applyFXAA(sampler2D tex, vec2 fragCoord) {
    vec2 inverseScreenSize = 1.0 / screenSize;

    vec3 rgbNW = texture(tex, (fragCoord + vec2(-1.0, -1.0)) * inverseScreenSize).xyz;
    vec3 rgbNE = texture(tex, (fragCoord + vec2(1.0, -1.0)) * inverseScreenSize).xyz;
    vec3 rgbSW = texture(tex, (fragCoord + vec2(-1.0, 1.0)) * inverseScreenSize).xyz;
    vec3 rgbSE = texture(tex, (fragCoord + vec2(1.0, 1.0)) * inverseScreenSize).xyz;
    vec3 rgbM  = texture(tex, fragCoord * inverseScreenSize).xyz;

    vec3 luma = vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);

    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

    vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

    float dirReduce = max(
        (lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL),
        FXAA_REDUCE_MIN);

    float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = min(vec2(FXAA_SPAN_MAX, FXAA_SPAN_MAX),
              max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),
                  dir * rcpDirMin)) * inverseScreenSize;

    vec3 rgbA = 0.5 * (
    texture(tex, fragCoord * inverseScreenSize + dir * (1.0/3.0 - 0.5)).xyz +
    texture(tex, fragCoord * inverseScreenSize + dir * (2.0/3.0 - 0.5)).xyz);
    vec3 rgbB = rgbA * 0.5 + 0.25 * (
    texture(tex, fragCoord * inverseScreenSize + dir * -0.5).xyz +
    texture(tex, fragCoord * inverseScreenSize + dir * 0.5).xyz);

    float lumaB = dot(rgbB, luma);
    if((lumaB < lumaMin) || (lumaB > lumaMax)) {
        return vec4(rgbA, 1.0);
    }
    return vec4(rgbB, 1.0);
}
