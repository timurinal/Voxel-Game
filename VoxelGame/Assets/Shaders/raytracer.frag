#version 450 core

#define INFINITY 3.402823466e+38

#define AtlasWidth 256
#define VoxelTextureSize 16

struct Ray {
    vec3 origin;
    vec3 dir;
};
Ray createRay(vec3 origin, vec3 dir) {
    Ray ray;
    ray.origin = origin;
    ray.dir = dir;
    return ray;
}

struct RTMaterial {
    vec3 colour;
};

struct HitInfo {
    bool didHit;
    float dst;
    vec3 point;
    vec3 normal;
    vec2 uv;
    RTMaterial material;
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
    
    RTMaterial material;
};
Cube createCube(vec3 min, vec3 max, RTMaterial material) {
    Cube cube;
    cube.min = min;
    cube.max = max;
    cube.material = material;
    return cube;
}
Cube createCube(vec3 offset, RTMaterial material) {
    Cube cube = createCube(vec3(-0.5, -0.5, -0.5) + offset, vec3(0.5, 0.5, 0.5) + offset, material);
    cube.material = material;
    return cube;
}

struct VoxelData {
    uint id;
    int texFaces[6];
};

out vec4 finalColour;

in vec2 texcoord;
in vec3 pos;

layout (std430, binding = 0) buffer VoxelDataBuffer {
    VoxelData voxels[];
};

layout (std430, binding = 1) buffer CubeBuffer {
    Cube cubes[];
};

uniform int NumCubes;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_invProj;
uniform mat4 m_invView;

uniform int MaxLightBounces;

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

uniform sampler2D _TestTexture;

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

bool rayAabb(Ray ray, vec3 aabbMin, vec3 aabbMax);
HitInfo raySphere(Ray ray, vec3 sphereCentre, float sphereRadius);
HitInfo rayCube(Ray ray, vec3 cubeMin, vec3 cubeMax);

vec2 getVoxelUv(int voxelID, float u, float v);

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
    vec3 rayDir = normalize(viewPos.xyz);
    
    uint rngState = hashVec2(texcoord);
    
    Ray ray = createRay(_CamPos, rayDir);
    
    HitInfo worldHit = calcRayCollision(ray);

    if (worldHit.didHit) {
        int faceId = 0;
        
        if (worldHit.normal == vec3(0, 0, 1)) { faceId = 0; }
        else if (worldHit.normal == vec3(0, 0, -1)) { faceId = 1; }
        else if (worldHit.normal == vec3(0, 1, 0)) { faceId = 2; }
        else if (worldHit.normal == vec3(0, -1, 0)) { faceId = 3; }
        else if (worldHit.normal == vec3(1, 0, 0)) { faceId = 4; }
        else if (worldHit.normal == vec3(-1, 0, 0)) { faceId = 5; }
        
        vec3 colour = texture(_TestTexture, getVoxelUv(8, worldHit.uv.x, 1 - worldHit.uv.y)).rgb;
        finalColour = vec4(colour * faceShading[faceId], 1.0f);
    } else {
        finalColour = vec4(getEnvironmentLight(ray), 1.0);
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
    float sun = pow(max(0.0, dot(ray.dir, SunLightDirection)), SunFocus) * SunIntensity;
    
    // combine ground, sky, and sun
    float groundToSkyT = smoothstep(-0.01, 0.0, ray.dir.y);
    float sunMask = float(groundToSkyT >= 1.0);
    // return mix(GroundColour, skyGradient, groundToSkyT) + sun * sunMask;
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
    float x = normalDistHash(state);
    float y = normalDistHash(state);
    float z = normalDistHash(state);
    return normalize(vec3(x, y, z));
}

vec3 randHemisphereDir(vec3 normal, inout uint state) {
    vec3 dir = randDir(state);
    return dir * sign(dot(normal, dir));
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

vec3 trace(Ray ray, inout uint rngState) {
    
    vec3 incomingLight = vec3(0);
    vec3 rayColour = vec3(0);
    
    for (int i = 0; i <= MaxLightBounces; i++) {
        HitInfo hitInfo = calcRayCollision(ray);
        
        if (hitInfo.didHit) {
            rayColour += hitInfo.material.colour;
        } else {
            break;
        }
    }
    
    return rayColour;
}

HitInfo calcRayCollision(Ray ray) {
    HitInfo closestHit = HitInfo(false, 0, vec3(0), vec3(0), vec2(0), RTMaterial(vec3(0)));
    
    // As we've not hit anything (yet), the 'closest' hit is infinitely far away
    closestHit.dst = INFINITY;
    
    // Raycast against each cube
    for (int i = 0; i < NumCubes; i++) {
       Cube cube = cubes[i];
        
       if (rayAabb(ray, cube.min, cube.max)) {
           HitInfo hitInfo = rayCube(ray, cube.min, cube.max);

           if (hitInfo.didHit && hitInfo.dst < closestHit.dst) {
               closestHit = hitInfo;
               closestHit.material = cube.material;
           }
       }
    }
    
    return closestHit;
}

bool rayAabb(Ray ray, vec3 aabbMin, vec3 aabbMax) {
    vec3 dirfrac = 1.0 / ray.dir;

    // Calculate distances for x, y, and z
    float t1 = (aabbMin.x - ray.origin.x) * dirfrac.x;
    float t2 = (aabbMax.x - ray.origin.x) * dirfrac.x;
    float t3 = (aabbMin.y - ray.origin.y) * dirfrac.y;
    float t4 = (aabbMax.y - ray.origin.y) * dirfrac.y;
    float t5 = (aabbMin.z - ray.origin.z) * dirfrac.z;
    float t6 = (aabbMax.z - ray.origin.z) * dirfrac.z;

    // Calculate max and min for x and y
    float tMin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    float tMax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));

    // The ray intersection is a hit if tMin < tMax and tMax > 0
    return (tMax >= 0) && (tMin <= tMax);
}

HitInfo raySphere(Ray ray, vec3 sphereCentre, float sphereRadius) {
    HitInfo hitInfo = HitInfo(false, 0, vec3(0), vec3(0), vec2(0), RTMaterial(vec3(0)));
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
    HitInfo hitInfo = HitInfo(false, 0, vec3(0), vec3(0), vec2(0), RTMaterial(vec3(0)));
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
    float epsilon = 1e-5; // Small epsilon value to handle floating point precision

    if (abs(hitInfo.point.x - cubeMin.x) < epsilon) hitNormal = vec3(-1, 0, 0);
    else if (abs(hitInfo.point.x - cubeMax.x) < epsilon) hitNormal = vec3(1, 0, 0);
    else if (abs(hitInfo.point.y - cubeMin.y) < epsilon) hitNormal = vec3(0, -1, 0);
    else if (abs(hitInfo.point.y - cubeMax.y) < epsilon) hitNormal = vec3(0, 1, 0);
    else if (abs(hitInfo.point.z - cubeMin.z) < epsilon) hitNormal = vec3(0, 0, -1);
    else if (abs(hitInfo.point.z - cubeMax.z) < epsilon) hitNormal = vec3(0, 0, 1);

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
