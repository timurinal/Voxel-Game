#version 450 core

struct Material {
    sampler2D diffuse;
    sampler2D specular;
    float     shininess;
};

struct DirLight {
    vec3 direction;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct PointLight {
    vec3 position;

    float constant;
    float linear;
    float quadratic;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

out vec4 finalCol;

in vec2 texcoord;
in float faceId;
in vec3 normal;
in vec3 fragPos;

uniform vec3 viewPos;

uniform vec3 fogColour;
uniform float fogDensity;

uniform Material material;
uniform DirLight dirLight;

vec3 calcDirLight(DirLight light, vec3 normal, vec3 viewDir);

void main() {
    vec3 norm = normalize(normal);
    vec3 viewDir = normalize(viewPos - fragPos);
    
    vec3 result = calcDirLight(dirLight, norm, viewDir);

    // TODO: Point lights
    // TODO?: Maybe spotlights, although might not fit

    float dist = length(fragPos - viewPos);
    float fogAmount = 1.0 - exp(-pow((dist * fogDensity), 2));
    fogAmount = clamp(fogAmount, 0.0, 1.0);
    
    finalCol = mix(vec4(result, 1.0), vec4(fogColour, 1.0), fogAmount);
}

vec3 calcDirLight(DirLight light, vec3 normal, vec3 viewDir) {
    vec3 lightDir = normalize(-light.direction);

    // diffuse lighting
    float diff = max(dot(normal, lightDir), 0.0);

    // specular lighting
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    // combine
    vec3 ambient  = light.ambient  * vec3(texture(material.diffuse, texcoord));
    vec3 diffuse  = light.diffuse  * diff * vec3(texture(material.diffuse, texcoord));
    vec3 specular = light.specular * spec * vec3(texture(material.specular, texcoord));
    return (ambient + diffuse + specular);
}