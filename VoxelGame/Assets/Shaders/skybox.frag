#version 450 core

in vec2 texcoord;
in vec3 fragPos;
in vec3 vertexColour;

out vec4 finalColour;

uniform vec3 viewPos;
uniform vec3 fogColour;
uniform float fogDensity;

void main()
{
    float dist = length(fragPos - viewPos);
    float fogAmount = 1.0 - exp(-pow((dist * fogDensity), 2));
    fogAmount = clamp(fogAmount, 0.0, 1.0);
    
    finalColour = mix(vec4(vertexColour, 1.0), vec4(fogColour, 1.0), fogAmount);
}