#version 450 core

#define INFINITY 3.402823466e+38

out vec4 finalColour;

in vec2 texcoord;
in vec3 pos;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_invProj;
uniform mat4 m_invView;

uniform vec3 _CamPos;

uniform float NearPlane;
uniform float FarPlane;

uniform sampler2D _MainTex;
uniform sampler2D _DepthTexture;

float sampleDepthTexture(vec2 uv);
vec3 sampleMainTexture(vec2 uv);
float linearizeDepth(float nonLinearDepth, float near, float far);

float intersectPlane(vec3 ro, vec3 rd, float h);
float computeDepthValue(vec3 position);

void main() {
    
    finalColour = vec4(sampleMainTexture(texcoord), 1.0);
    return;
    
    vec4 clipPos = vec4(pos.xy, 1.0, 1.0);
    vec4 viewPos = m_invProj * clipPos;
    viewPos /= viewPos.w; // Perspective divide

    // Now it represents a point on the far plane in view space
    vec3 rayDirInViewSpace = viewPos.xyz;

    // Transform this point to the world space
    // We apply only rotation part of the view matrix, because we have already applied the perspective part
    mat3 rotView = mat3(m_invView);
    vec3 rayDir = rotView * rayDirInViewSpace;

    // get scene depth
    float sceneDepthNonLinear = sampleDepthTexture(texcoord);
    float sceneDepth = linearizeDepth(sceneDepthNonLinear, NearPlane, FarPlane);

    // ray origin
    vec3 ro = _CamPos;

    // intersection
    float distanceToIntersection = intersectPlane(ro, rayDir, -15);
    bool didHitPlane = distanceToIntersection != INFINITY;

    if (didHitPlane) {
        vec3 intersectionPointCameraSpace = _CamPos + rayDir * distanceToIntersection;
        float intersectionPointDepth = -intersectionPointCameraSpace.z;

        if (sceneDepth > intersectionPointDepth + 0.00001) {
            finalColour = vec4(0, 0, 1, 0.0);
            return;
        }
    }

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

float intersectPlane(vec3 ro, vec3 rd, float h)
{
    float shiftedOriginY = ro.y - h;

    if (rd.y == 0.0f)
    {
        return (shiftedOriginY == 0.0f) ? 0.0f : INFINITY;
    }

    float t = -shiftedOriginY / rd.y;

    if (t < 0.0f)
    {
        return INFINITY;
    }

    return t;
}

float computeDepthValue(vec3 position)
{
    vec4 homPosition = m_proj * vec4(position, 1.0);
    return (homPosition.z / homPosition.w + 1.0)/ 2.0;
}
