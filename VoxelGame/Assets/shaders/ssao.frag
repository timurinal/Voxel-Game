#version 450 core

out vec4 fCol;

in vec2 uv;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gAlbedo;
uniform sampler2D gMaterial;
uniform sampler2D gSpecular;
uniform sampler2D gDepth;

uniform mat4 m_proj;

float hash(inout uint state);
vec3 randomHemisphere(vec3 normal, inout uint hash);
uint hashVec2(vec2 v);

vec3 getRandomVector(vec2 uv) {
    float random = hashVec2(uv);
    float angle = random * 2.0 * 3.14159;
    float x = cos(angle);
    float y = sin(angle);
    // Return a random vector in tangent space
    return vec3(x, y, 0.0);
}

void main() {
    vec3 fragPosition = texture(gPosition, uv).xyz;
    vec3 normal = normalize(texture(gNormal, uv).xyz);

    // Generate random rotation in tangent space
    vec3 randomVec = getRandomVector(uv);

    // Compute occlusion using your SSAO method
    // Example SSAO computation
    float radius = 0.5;  // Example radius
    float bias = 0.025;  // Example bias

    int sampleCount = 16; // Number of samples
    float occlusionFactor = 0.0;

    for (int i = 0; i < sampleCount; ++i) {
        // Generate sample vectors in tangent space
        vec3 samplePoint = fragPosition + randomVec * radius * float(i) / float(sampleCount);
        vec4 offset = m_proj * vec4(samplePoint, 1.0);
        offset.xyz /= offset.w;
        offset.xyz = offset.xyz * 0.5 + 0.5;

        float sampleDepth = texture(gDepth, offset.xy).r;
        float rangeCheck = smoothstep(0.0, 1.0, radius / abs(fragPosition.z - sampleDepth));
        occlusionFactor += (sampleDepth >= samplePoint.z + bias ? 1.0 : 0.0) * rangeCheck;
    }

    float occlusion = 1.0 - occlusionFactor / float(sampleCount);
    fCol = vec4(occlusion);
}

float hash(inout uint state) {
    state = state * 747796405u + 2891336453u;
    uint result = ((state >> (state >> 28u + 4u)) ^ state) * 277803737u;
    return float(result) / 4294967265.0f;
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

vec3 randomHemisphere(vec3 normal, inout uint state) {
    // Generate two random numbers between 0 and 1
    float u1 = hash(state);
    float u2 = hash(state);

    // Map u1 and u2 to spherical coordinates
    float r = sqrt(1.0 - u1 * u1);
    float phi = 2.0 * 3.1415926535897932384626433832795 * u2;

    float x = r * cos(phi);
    float y = r * sin(phi);
    float z = u1;

    // Create a random point in the hemisphere biased towards the normal
    vec3 randomPoint = vec3(x, y, abs(z));
    return normalize(mix(normalize(randomPoint), normal, 0.8)); // Bias towards the normal
}