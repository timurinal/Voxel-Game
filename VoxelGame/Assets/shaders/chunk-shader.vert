#version 450 core

#define ATLAS_WIDTH 256
#define ATLAS_HEIGHT ATLAS_WIDTH
#define VOXEL_TEXTURE_SIZE 16
#define PADDING 0.0001;

layout (location = 0) in uint vData;

struct VertexData {
    vec3 position;
    vec3 normal;
    int tex;
    vec2 uvs;
    ivec2 uvi;
    int faceId;
};

out vec3 normal;
out vec3 tangent;
out vec3 bitangent;

flat out vec3 vertexPos;
out vec3 fragPos;

out vec2 uv;

flat out int faceId;

uniform mat4 m_proj;
uniform mat4 m_view;

uniform vec3 chunkPosition;

const vec3 normals[] = vec3[6](
vec3(0, 0, -1), vec3( 0,  0, 1),
vec3(0, 1,  0), vec3( 0, -1, 0),
vec3(1, 0,  0), vec3(-1,  0, 0)
);

VertexData unpackData(uint data);

vec2 computeTextureCoordinates(int voxelId, int face, int u, int v);

void main() {
    VertexData unpackedData = unpackData(vData);

    vec3 vertexPosition = unpackedData.position + chunkPosition;
    vec4 pos = vec4(vertexPosition, 1.0);
    gl_Position = m_proj * m_view * pos;

    normal = unpackedData.normal;
    
    if (abs(normal.z) > abs(normal.y)) {
        tangent = normalize(cross(vec3(0.0, 1.0, 0.0), normal));
    } else {
        tangent = normalize(cross(vec3(0.0, 0.0, 1.0), normal));
    }
        bitangent = normalize(cross(normal, tangent));
    
    normal = unpackedData.normal;
    tangent = tangent;
    bitangent = bitangent;

    vertexPos = pos.xyz;
    fragPos = pos.xyz;

    uv = computeTextureCoordinates(unpackedData.tex - 1, unpackedData.faceId, unpackedData.uvi.x, unpackedData.uvi.y);
    // uv = unpackedData.uvs;
    
    faceId = unpackedData.faceId;
}

VertexData unpackData(uint data) {
    VertexData vData;

    vData.tex = int((data >> 22) & 0xFF);
    int texU = int((data >> 31) & 0x01);    // u
    int texV = int((data >> 30) & 0x01);    // v
    vData.uvs = vec2(texU, texV);
    vData.uvi = ivec2(texU, texV);

    int normalId = int((data >> 19) & 0x07);
    vData.normal = normals[normalId];
    vData.faceId = normalId;

    vec3 pos;
    pos.z = float((data >> 13) & 0x3F);
    pos.y = float((data >> 7) & 0x3F);
    pos.x = float(data & 0x3F);

    vData.position = pos;

    return vData;
}

vec2 computeTextureCoordinates(int voxelId, int face, int u, int v) {
    if (face < 0 || face > 6) return vec2(0, 0); // The face can only be 6 possible values, so if it is outside this range the data is likely incorrect

    int texId = voxelId; // TODO: calculate the texture id based on the face to allow faces to have unique textures
    // this will have to be done by sending a buffer of texture information over, but I'll do that later

    int texturesPerRow = ATLAS_WIDTH / VOXEL_TEXTURE_SIZE;
    float unit = 1.0 / texturesPerRow;
    float adjustedUnit = unit - 2 * PADDING;

    float x = (texId % texturesPerRow) * unit + PADDING;

    // Flipping the y-coordinate to start from the top left
    float y = 1.0 - ((texId / texturesPerRow) + 1) * unit + PADDING;

    return vec2(x + u * adjustedUnit, y + v * adjustedUnit);
}
