#version 450 core

#define INFINITY 3.402823466e+38

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

struct HitInfo {
    bool didHit;
    float dst;
    vec3 point;
    vec3 normal;
    vec2 uv;
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

struct RTMaterial {
    vec3 colour;
};

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

out vec4 finalColour;

in vec2 texcoord;
in vec3 pos;

layout (std430, binding = 0) buffer CubeBuffer {
    Cube cubes[];
};

uniform int NumCubes;

uniform mat4 m_proj;
uniform mat4 m_view;
uniform mat4 m_invProj;
uniform mat4 m_invView;

uniform vec3 _CamPos;

uniform float NearPlane;
uniform float FarPlane;

uniform sampler2D _MainTex;

uniform sampler2D _TestTexture;

vec3 sampleMainTexture(vec2 uv);
float linearizeDepth(float nonLinearDepth, float near, float far);

HitInfo calcRayCollision(Ray ray);

HitInfo raySphere(Ray ray, vec3 sphereCentre, float sphereRadius);
HitInfo rayCube(Ray ray, vec3 cubeMin, vec3 cubeMax);

void main() {    
    vec4 clipPos = vec4(pos.xy, 1.0, 1.0);
    vec4 viewPos = m_invProj * clipPos;
    viewPos /= viewPos.w; // Perspective divide
    viewPos = m_invView * viewPos;
    vec3 rayDir = normalize(viewPos.xyz);
    
    Ray ray = createRay(_CamPos, rayDir);
    
    HitInfo worldHit = calcRayCollision(ray);
    
    if (worldHit.didHit) {
        finalColour = vec4(texture(_TestTexture, worldHit.uv).rgb, 0.0);
    } else {
        finalColour = vec4(0);
    }
}

vec3 sampleMainTexture(vec2 uv) {
    return texture(_MainTex, uv).rgb;
}

float linearizeDepth(float nonLinearDepth, float near, float far) {
    float z = nonLinearDepth;
    return (2.0 * near * far) / (far + near - z * (far - near));
}

vec3 trace(Ray ray) {
    return ray.dir;
}

HitInfo calcRayCollision(Ray ray) {
    HitInfo closestHit = HitInfo(false, 0, vec3(0), vec3(0), vec2(0));
    
    // As we've not hit anything (yet), the 'closest' hit is infinitely far away
    closestHit.dst = INFINITY;
    
    // Raycast against each cube
    for (int i = 0; i < NumCubes; i++) {
       Cube cube = cubes[i];
       HitInfo hitInfo = rayCube(ray, cube.min, cube.max);
        
        if (hitInfo.didHit && hitInfo.dst < closestHit.dst) {
            closestHit = hitInfo;
        }
    }
    
    return closestHit;
}

HitInfo raySphere(Ray ray, vec3 sphereCentre, float sphereRadius) {
    HitInfo hitInfo = HitInfo(false, 0, vec3(0), vec3(0), vec2(0));
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
    HitInfo hitInfo = HitInfo(false, 0, vec3(0), vec3(0), vec2(0));
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
