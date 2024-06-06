#version 450 core

#define FXAA_REDUCE_MIN   (1.0/128.0)
#define FXAA_REDUCE_MUL   (1.0/8.0)
#define FXAA_SPAN_MAX     8.0

out vec4 finalColour;

in vec2 texcoord;

uniform float NearPlane;
uniform float FarPlane;

uniform sampler2D _MainTex;
uniform sampler2D _DepthTexture;

float sampleDepthTexture(sampler2D depth, vec2 uv);
float linearizeDepth(float nonLinearDepth, float near, float far);

vec4 fxaa(vec4 colour, vec2 fragCoord, vec2 resolution,
          vec2 v_rgbNW, vec2 v_rgbNE,
          vec2 v_rgbSW, vec2 v_rgbSE,
          vec2 v_rgbM);

void main() {
}

float sampleDepthTexture(sampler2D depth, vec2 uv) {
    return texture(depth, uv).r;
}

float linearizeDepth(float nonLinearDepth, float near, float far) {
    float z = nonLinearDepth;
    return (2.0 * near * far) / (far + near - z * (far - near));
}

vec4 fxaa(vec4 colour, vec2 fragCoord, vec2 resolution,
          vec2 v_rgbNW, vec2 v_rgbNE,
          vec2 v_rgbSW, vec2 v_rgbSE,
          vec2 v_rgbM) {
    vec4 rgbNW = texture2D(_MainTex, v_rgbNW);
    vec4 rgbNE = texture2D(_MainTex, v_rgbNE);
    vec4 rgbSW = texture2D(_MainTex, v_rgbSW);
    vec4 rgbSE = texture2D(_MainTex, v_rgbSE);
    vec4 rgbM  = texture2D(_MainTex, v_rgbM );
    vec4 luma = vec4(0.299, 0.587, 0.114, 1.0);
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

    float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) *
                          (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);

    float rcpDirMin = 1.0/(min(abs(dir.x), abs(dir.y)) + dirReduce);
    dir = min(vec2(FXAA_SPAN_MAX, FXAA_SPAN_MAX),
              max(vec2(-FXAA_SPAN_MAX, -FXAA_SPAN_MAX),
                  dir * rcpDirMin)) / resolution;

    vec3 rgbA = 0.5 * (
    texture2D(_MainTex, fragCoord * dir * 0.5).xyz +
    texture2D(_MainTex, fragCoord * dir * -0.5).xyz);
    vec3 rgbB = rgbA * 0.5 + 0.25 * (
    texture2D(_MainTex, fragCoord * dir * 0.25).xyz +
    texture2D(_MainTex, fragCoord * dir * -0.25).xyz);

    float lumaB = dot(rgbB, luma);
    if((lumaB < lumaMin) || (lumaB > lumaMax))
    colour.rgb = rgbA;
    else
    colour.rgb = rgbB;

    return colour;
}