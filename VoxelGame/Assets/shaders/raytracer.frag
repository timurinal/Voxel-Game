#version 450 core

#define INFINITY 3.402823466e+38
#define EPSILON 1e-4

#define AtlasWidth 256
#define VoxelTextureSize 16

#define RT_MODE 0
#define DRAW_CUBE_INSIDES false

struct Ray {
    vec3 origin;
    vec3 dir;
};
Ray createRay(vec3 origin, vec3 dir) {
    Ray ray;
    ray.origin = origin;
    ray.dir = normalize(dir);
    return ray;
}

struct RTMaterial {
    vec3 colour;
    float emissionStrength;

    vec3 emissionColour;
    float _pad0;
};

struct HitInfo {
    bool didHit;
    float dst;
    vec3 point;
    vec3 normal;
    vec2 uv;
    int id;
    RTMaterial material;

    int aabbTests;
    int debugValue;
};
HitInfo createHitInfo(bool didHit, float dst, vec3 point, vec3 normal, vec2 uv) {
    HitInfo hitInfo;
    hitInfo.didHit = didHit;
    hitInfo.dst = dst;
    hitInfo.point = point;
    hitInfo.normal = normal;
    hitInfo.uv = uv;
    return hitInfo;
}

struct Cube {
    vec3 min;
    vec3 max;

    int id;

    RTMaterial material;
};
Cube createCube(vec3 min, vec3 max, RTMaterial material) {
    Cube cube;
    cube.min = min;
    cube.max = max;
    cube.material = material;
    return cube;
}
Cube createCubeOffset(vec3 offset, RTMaterial material) {
    Cube cube = createCube(vec3(-0.5, -0.5, -0.5) + offset, vec3(0.5, 0.5, 0.5) + offset, material);
    cube.material = material;
    return cube;
}

struct Chunk {
    vec3 boundsMin; int voxelCount;
    vec3 boundsMax; int voxelStartIndex;
};

struct VoxelData {
    uint id;
    int texFaces[6];
};

out vec4 finalColour;

in vec2 texcoord;
in vec3 pos;

layout (std430, binding = 0) buffer ChunkBuffer {
    Chunk chunks[];
};
layout (std430, binding = 1) buffer CubeBuffer {
    Cube cubes[];
};

uniform int NumChunks;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_invProj;
uniform mat4 m_invView;

uniform int MaxLightBounces;
uniform int RaysPerPixel;

uniform float SkyboxIntensity;

uniform float Time;

uniform vec3 _CamPos;

uniform float NearPlane;
uniform float FarPlane;

uniform vec3 SkyColourHorizon;
uniform vec3 SkyColourZenith;
uniform vec3 GroundColour;

uniform float SunFocus;
uniform float SunIntensity;

uniform vec3 SunLightDirection;

uniform sampler2D _MainTex;

uniform sampler2D TestTexture;

vec3 sampleMainTexture(vec2 uv);
float linearizeDepth(float nonLinearDepth, float near, float far);

vec3 getEnvironmentLight(Ray ray);

float hash(inout uint state);
float normalDistHash(inout uint state);
vec3 randDir(inout uint state);
vec3 randHemisphereDir(vec3 normal, inout uint state);
uint hashVec2(vec2 v);

vec3 trace(Ray ray, inout uint rngState);

HitInfo calcRayCollision(Ray ray);

bool rayAabb(Ray ray, vec3 boxMin, vec3 boxMax);
HitInfo raySphere(Ray ray, vec3 sphereCentre, float sphereRadius);
HitInfo rayCube(Ray ray, vec3 cubeMin, vec3 cubeMax);

vec2 getVoxelUv(int voxelID, float u, float v);

float remap(float value, float outputMin, float outputMax);

vec3 transformPoint(vec3 point, mat4 matrix);

const float faceShading[6] = float[6](
    0.6, 0.8,  // front back
    1.0, 0.4,  // top bottom
    0.5, 0.7   // right left
);

void main() {
    vec4 clipPos = vec4(pos.xy, 1.0, 1.0);
    vec4 viewPos = m_invProj * clipPos;
    viewPos /= viewPos.w; // Perspective divide
    viewPos = m_invView * viewPos;
    vec3 rayDir = normalize(viewPos.xyz - _CamPos); // Correct ray direction based on camera position

    uint rngState = hashVec2(texcoord + vec2(Time, -Time));

    Ray ray = createRay(_CamPos, rayDir);

    if (RT_MODE == 0) {
        HitInfo worldHit = calcRayCollision(ray);
        
        if (worldHit.didHit) {
            finalColour = vec4(texture(TestTexture, worldHit.uv).rgb, 1.0);
        } else {
            finalColour = vec4(getEnvironmentLight(ray), 1.0);
        }
    } else if (RT_MODE == 1) {
        HitInfo worldHit = calcRayCollision(ray);

        if (worldHit.didHit) {
            float val = worldHit.debugValue / (NumChunks - 1);
            finalColour = vec4(texture(TestTexture, worldHit.uv).rgb * val, 1.0);
        } else {
            finalColour = vec4(getEnvironmentLight(ray), 1.0);
        }
    } else if (RT_MODE == 2) {
        vec3 col = vec3(0);

        for (int rayIndex = 0; rayIndex < RaysPerPixel; rayIndex++) {
            col += trace(ray, rngState);
        }

        col /= RaysPerPixel;
        finalColour = vec4(col, 1.0);
    }
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
    float sun = pow(max(0.0, dot(ray.dir, -SunLightDirection)), SunFocus) * SunIntensity;
    float moon = pow(max(0.0, dot(ray.dir, SunLightDirection)), SunFocus) * SunIntensity;

    float groundToSkyT = smoothstep(-0.01, 0.0, ray.dir.y);
    float sunMask = groundToSkyT >= 1.0 ? 1.0 : 0.0;
    return mix(GroundColour, skyGradient, groundToSkyT);
}

float hash(inout uint state) {
    state = state * 747796405u + 2891336453u;
    uint result = ((state >> (state >> 28u + 4u)) ^ state) * 277803737u;
    return float(result) / 4294967265.0f;
}

float normalDistHash(inout uint state) {
    // thanks to https://stackoverflow.com/a/6178290
    float theta = 2 * 3.14159265 * hash(state);
    float rho = sqrt(-2 * log(hash(state)));
    return rho * cos(theta);
}

vec3 randDir(inout uint state) {
    float z = 2.0 * hash(state) - 1.0; // z in range [-1, 1]
    float r = sqrt(1.0 - z * z);
    float theta = 2.0 * 3.14159265358979323846 * hash(state);
    return vec3(r * cos(theta), r * sin(theta), z);
}

vec3 randHemisphereDir(vec3 normal, inout uint state) {
    vec3 dir = randDir(state);
    return dot(dir, normal) > 0.0 ? dir : -dir;
}

uint hashVec2(vec2 v) {
    // Constants for hashing
    const uint prime1 = 0x85ebca6b;
    const uint prime2 = 0xc2b2ae35;
    const uint prime3 = 0x27d4eb2f;
    const uint prime4 = 0x165667b1;

    // Mix the components of the vec2
    uint h1 = floatBitsToUint(v.x) * prime1;
    uint h2 = floatBitsToUint(v.y) * prime2;

    // Bitwise operations for better mixing
    h1 ^= (h1 >> 15);
    h1 *= prime3;
    h1 ^= (h1 >> 13);

    h2 ^= (h2 >> 15);
    h2 *= prime4;
    h2 ^= (h2 >> 13);

    // Combine the hashed values
    uint result = h1 ^ h2;
    result ^= (result >> 16);

    return result;
}

bool nearlyEqual(float a, float b, float epsilon) {
    return abs(a - b) < epsilon;
}

vec3 trace(Ray ray, inout uint rngState) {
    vec3 incomingLight = vec3(0);
    vec3 rayColour = vec3(1);
    
    bool hasHit = false;
    
    int tests = 0;
    
    for (int i = 0; i < MaxLightBounces; i++) {
       HitInfo hitInfo = calcRayCollision(ray);
        if (hitInfo.didHit) {
            hasHit = true;
            
            ray.origin = hitInfo.point;
            ray.dir = randHemisphereDir(hitInfo.normal, rngState);
            
            tests += hitInfo.aabbTests;
            
            RTMaterial material = hitInfo.material;
            material.colour = texture(TestTexture, hitInfo.uv).rgb;
            
            vec3 emittedLight = material.emissionColour * material.emissionStrength;
            incomingLight += emittedLight * rayColour;
            rayColour *= material.colour;
        } else {
            // We missed every object and this ray 'hit' the sky
            // but first check if that ray hit an object first to
            // allow darkening of the natural light given off by
            // the skybox
            if (hasHit)
            {
                incomingLight += (getEnvironmentLight(ray) * rayColour) * SkyboxIntensity;
            } else
            {
                incomingLight += (getEnvironmentLight(ray) * rayColour);
            }
            break;
        }
    }
    
    return incomingLight;
}

HitInfo calcRayCollision(Ray ray) {
    HitInfo closestHit = HitInfo(false, 0, vec3(0), vec3(0), vec2(0), 0, RTMaterial(vec3(0), 0, vec3(0), 0), 0, 0);

    // As we've not hit anything (yet), the 'closest' hit is infinitely far away
    closestHit.dst = INFINITY;

    for (int i = 0; i < NumChunks; i++) {
        Chunk chunk = chunks[i];
        
        // Raycast against the chunk's AABB
        if (rayAabb(ray, chunk.boundsMin, chunk.boundsMax)) {
            closestHit.aabbTests += 1;
            // If the ray hits the chunk AABB, check each cube
            // TODO: BVH
            
            for (int j = 0; j < chunk.voxelCount; j++) {
                Cube cube = cubes[j + chunk.voxelStartIndex];
                
                if (rayAabb(ray, cube.min, cube.max)) {
                    closestHit.aabbTests += 1;
                    closestHit.debugValue = i;
                    HitInfo hitInfo = rayCube(ray, cube.min, cube.max);
                    // HitInfo hitInfo = raySphere(ray, (cube.min + cube.max) * 0.5, 0.5);
                    
                    if (hitInfo.didHit && hitInfo.dst < closestHit.dst) {
                       closestHit = hitInfo;
                       closestHit.material = cube.material;
                       closestHit.id = cube.id;
                    }
                }
            }
        }
    }
    
    // Trace sun sphere
//    HitInfo sphereHit = raySphere(ray, vec3(-50, 100, -50), 25);
//    if (sphereHit.didHit && sphereHit.dst < closestHit.dst) {
//        closestHit = sphereHit;
//        closestHit.material = RTMaterial(vec3(0), 15, vec3(1.0), 0);
//    }  

    return closestHit;
}

bool rayAabb(Ray ray, vec3 boxMin, vec3 boxMax) {
    vec3 invDir = 1.0 / ray.dir;
    vec3 tMin = (boxMin - ray.origin) * invDir;
    vec3 tMax = (boxMax - ray.origin) * invDir;

    vec3 t1 = min(tMin, tMax);
    vec3 t2 = max(tMin, tMax);

    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    return tNear <= tFar && tFar >= 0.0;
}

HitInfo raySphere(Ray ray, vec3 sphereCentre, float sphereRadius) {
    HitInfo hitInfo = HitInfo(false, 0, vec3(0), vec3(0), vec2(0), 0, RTMaterial(vec3(0), 0, vec3(0), 0), 0, 0);
    vec3 offsetRayOrigin = ray.origin - sphereCentre;

    float a = dot(ray.dir, ray.dir);
    float b = 2 * dot(offsetRayOrigin, ray.dir);
    float c = dot(offsetRayOrigin, offsetRayOrigin) - sphereRadius * sphereRadius;
    float discriminant = b * b - 4 * a * c;

    if (discriminant >= 0) {
        float dst = (-b - sqrt(discriminant)) / (2 * a);

        if (dst >= 0) {
            hitInfo.didHit = true;
            hitInfo.dst = dst;
            hitInfo.point = ray.origin + ray.dir * dst;
            hitInfo.normal = normalize(hitInfo.point - sphereCentre);
        }
    }

    return hitInfo;
}

HitInfo rayCube(Ray ray, vec3 cubeMin, vec3 cubeMax) {
    HitInfo hitInfo = HitInfo(false, 0, vec3(0), vec3(0), vec2(0), 0, RTMaterial(vec3(0), 0, vec3(0), 0), 0, 0);
    float tMin = (cubeMin.x - ray.origin.x) / ray.dir.x;
    float tMax = (cubeMax.x - ray.origin.x) / ray.dir.x;

    if (tMin > tMax) {
        float temp = tMin;
        tMin = tMax;
        tMax = temp;
    }

    float tyMin = (cubeMin.y - ray.origin.y) / ray.dir.y;
    float tyMax = (cubeMax.y - ray.origin.y) / ray.dir.y;

    if (tyMin > tyMax) {
        float temp = tyMin;
        tyMin = tyMax;
        tyMax = temp;
    }

    if ((tMin > tyMax) || (tyMin > tMax))
    return hitInfo;

    if (tyMin > tMin)
    tMin = tyMin;
    if (tyMax < tMax)
    tMax = tyMax;

    float tzMin = (cubeMin.z - ray.origin.z) / ray.dir.z;
    float tzMax = (cubeMax.z - ray.origin.z) / ray.dir.z;

    if (tzMin > tzMax) {
        float temp = tzMin;
        tzMin = tzMax;
        tzMax = temp;
    }

    if ((tMin > tzMax) || (tzMin > tMax))
    return hitInfo;

    if (tzMin > tMin)
    tMin = tzMin;
    if (tzMax < tMax)
    tMax = tzMax;

    float t = tMin;

    if (t < 0) {
        t = tMax;
        if (t < 0)
        return hitInfo;
    }

    hitInfo.didHit = true;
    hitInfo.dst = t;
    hitInfo.point = ray.origin + t * ray.dir;

    vec3 hitNormal = vec3(0);
    vec3 dirNormalized = normalize(ray.dir);

    if (nearlyEqual(hitInfo.point.x, cubeMin.x, EPSILON)) {
        hitNormal = vec3(-1, 0, 0);
        if (dot(dirNormalized, hitNormal) > 0 && !DRAW_CUBE_INSIDES) {
            hitInfo.didHit = false;
            return hitInfo;
        }
    }
    else if (nearlyEqual(hitInfo.point.x, cubeMax.x, EPSILON)) {
        hitNormal = vec3(1, 0, 0);
        if (dot(dirNormalized, hitNormal) > 0 && !DRAW_CUBE_INSIDES) {
            hitInfo.didHit = false;
            return hitInfo;
        }
    }
    else if (nearlyEqual(hitInfo.point.y, cubeMin.y, EPSILON)) {
        hitNormal = vec3(0, -1, 0);
        if (dot(dirNormalized, hitNormal) > 0 && !DRAW_CUBE_INSIDES) {
            hitInfo.didHit = false;
            return hitInfo;
        }
    }
    else if (nearlyEqual(hitInfo.point.y, cubeMax.y, EPSILON)) {
        hitNormal = vec3(0, 1, 0);
        if (dot(dirNormalized, hitNormal) > 0 && !DRAW_CUBE_INSIDES) {
            hitInfo.didHit = false;
            return hitInfo;
        }
    }
    else if (nearlyEqual(hitInfo.point.z, cubeMin.z, EPSILON)) {
        hitNormal = vec3(0, 0, -1);
        if (dot(dirNormalized, hitNormal) > 0 && !DRAW_CUBE_INSIDES) {
            hitInfo.didHit = false;
            return hitInfo;
        }
    }
    else if (nearlyEqual(hitInfo.point.z, cubeMax.z, EPSILON)) {
        hitNormal = vec3(0, 0, 1);
        if (dot(dirNormalized, hitNormal) > 0 && !DRAW_CUBE_INSIDES) {
            hitInfo.didHit = false;
            return hitInfo;
        }
    }

    hitInfo.normal = hitNormal;

    // Compute UV coordinates based on the hit face
    vec3 localPoint = hitInfo.point - cubeMin; // Convert to local space
    vec3 cubeSize = cubeMax - cubeMin;

    if (hitNormal == vec3(-1, 0, 0)) {
        hitInfo.uv = vec2(localPoint.z / cubeSize.z, localPoint.y / cubeSize.y);
    } else if (hitNormal == vec3(1, 0, 0)) {
        hitInfo.uv = vec2(1.0 - localPoint.z / cubeSize.z, localPoint.y / cubeSize.y);
    } else if (hitNormal == vec3(0, -1, 0)) {
        hitInfo.uv = vec2(localPoint.x / cubeSize.x, localPoint.z / cubeSize.z);
    } else if (hitNormal == vec3(0, 1, 0)) {
        hitInfo.uv = vec2(1.0 - localPoint.x / cubeSize.x, localPoint.z / cubeSize.z);
    } else if (hitNormal == vec3(0, 0, -1)) {
        hitInfo.uv = vec2(1.0 - localPoint.x / cubeSize.x, localPoint.y / cubeSize.y);
    } else if (hitNormal == vec3(0, 0, 1)) {
        hitInfo.uv = vec2(localPoint.x / cubeSize.x, localPoint.y / cubeSize.y);
    }

    // Apply a small depth bias to avoid z-fighting
    hitInfo.point += hitNormal * EPSILON;

    return hitInfo;
}

vec2 getVoxelUv(int voxelID, float u, float v) {
    int textureID = voxelID;
    int texturePerRow = AtlasWidth / VoxelTextureSize;
    float unit = 1.0f / texturePerRow;

    // Padding to avoid bleeding
    const float padding = 0.0001f;

    float x = (textureID % texturePerRow) * unit + padding;
    float y = (textureID / texturePerRow) * unit + padding;
    float adjustedUnit = unit - 2 * padding;

    return vec2(x + u * adjustedUnit, y + v * adjustedUnit);
}

float remap(float value, float outputMin, float outputMax)
{
    return outputMin + (outputMax - outputMin) * 0.5 * (value + 1.0);
}

vec3 transformPoint(vec3 point, mat4 matrix) {
    vec4 transformedPoint = matrix * vec4(point, 1.0);
    return transformedPoint.xyz;
}