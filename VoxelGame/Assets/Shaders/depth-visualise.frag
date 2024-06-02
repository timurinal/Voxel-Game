#version 450 core
out vec4 FragColour;

in vec2 TexCoords;

uniform sampler2D depthMap;

void main()
{
    float depthValue = texture(depthMap, TexCoords).r;
    FragColour = vec4(vec3(depthValue), 1.0);
}
