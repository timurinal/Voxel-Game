#version 450 core

struct Ray {
    vec3 origin;
    vec3 dir;
};

out vec4 finalColour;

in vec2 texcoord;
in vec3 pos;

uniform float NearPlane;
uniform float FarPlane;

uniform sampler2D _MainTex;
uniform sampler2D _DepthTexture;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_invProj;
uniform mat4 m_invView;

uniform vec3 _CamPos;

uniform vec3 SkyColourHorizon;
uniform vec3 SkyColourZenith;
uniform vec3 GroundColour;
uniform vec3 SunColour;

uniform float SunFocus;
uniform float SunIntensity;

uniform vec3 SunLightDirection;

float sampleDepthTexture(vec2 uv);
vec3 sampleMainTexture(vec2 uv);
float linearizeDepth(float nonLinearDepth, float near, float far);

vec3 getEnvironmentLight(Ray ray);

void main()
{
    vec4 clipPos = vec4(pos.xy, 1.0, 1.0);
    vec4 viewPos = m_invProj * clipPos;
    viewPos /= viewPos.w; // Perspective divide
    viewPos = m_invView * viewPos;
    vec3 rayDir = normalize(viewPos.xyz - _CamPos); // Correct ray direction based on camera position
    
    Ray ray = Ray(_CamPos, rayDir);
    
    float sceneDepthNonLinear = sampleDepthTexture(texcoord);
    float sceneDepth = linearizeDepth(sceneDepthNonLinear, NearPlane, FarPlane);
    
    if (sceneDepthNonLinear >= 1)
            finalColour = vec4(getEnvironmentLight(ray), 1.0);
    else
        finalColour = vec4(sampleMainTexture(texcoord), 1.0);
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

vec3 getEnvironmentLight(Ray ray) {
    float skyGradientT = pow(smoothstep(0.0, 0.4, ray.dir.y), 0.35);
    vec3 skyGradient = mix(SkyColourHorizon, SkyColourZenith, skyGradientT);
    float sun = pow(max(0.0, dot(ray.dir, SunLightDirection)), SunFocus) * SunIntensity;

    float groundToSkyT = smoothstep(-0.01, 0.0, ray.dir.y);
    float sunMask = groundToSkyT >= 1.0 ? 1.0 : 0.0;
    return (mix(GroundColour, skyGradient, groundToSkyT) + (sun * vec3(SunColour.r, SunColour.g, SunColour.b)) * sunMask);
}