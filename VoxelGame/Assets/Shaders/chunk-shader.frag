#version 450 core

const float MIN_SHADOW_INTENSITY = 0.5;

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

layout (std430, binding = 0) buffer PointLights {
    PointLight LightData[];
};
uniform int NumPointLights;

out vec4 finalCol;

in vec2 texcoord;
in float faceId;
in vec3 normal;
in vec3 fragPos;
in vec4 fragPosLightSpace;

uniform vec3 viewPos;

uniform vec3 fogColour;
uniform float fogDensity;

uniform Material material;
uniform DirLight dirLight;

uniform sampler2D shadowMap;

vec2 hash2(vec2 p);

vec3 calcDirLight(DirLight light, vec3 normal, vec3 viewDir, float shadow);
vec3 calcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);

float calcShadow(vec4 lightSpaceFragPos);

void main() {
    vec3 norm = normalize(normal);
    vec3 viewDir = normalize(viewPos - fragPos);
    
    float shadow = calcShadow(fragPosLightSpace);
    vec3 result = calcDirLight(dirLight, norm, viewDir, shadow);
    //result *= 1 - (shadow);
    
    if (NumPointLights > 0)
        for (int i = 0; i < NumPointLights; i++)
                result += calcPointLight(LightData[i], norm, fragPos, viewDir);
            
    // TODO?: Maybe spotlights, although might not fit with the style

    float dist = length(fragPos - viewPos);
    float fogAmount = 1.0 - exp(-pow((dist * fogDensity), 2));
    fogAmount = clamp(fogAmount, 0.0, 1.0);

    float ditherAmount = 1.0 / 16.0;
    vec2 screenResolution = vec2(1920, 1080);
    vec2 uv = gl_FragCoord.xy / screenResolution.xy;
    vec2 noise = hash2(uv);
    
    // finalCol = mix(vec4(result, 1.0), vec4(fogColour, 1.0), fogAmount);
    vec4 lit = vec4(result, 1.0);
    lit = clamp(lit, 0.0, 1.0);
    
    // TODO: dithering
    
    finalCol = lit;
}

vec3 calcDirLight(DirLight light, vec3 normal, vec3 viewDir, float shadow) {
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
    return (ambient + (1.0 - shadow) * (diffuse + specular));
}

vec3 calcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir) {
    vec3 lightDir = normalize(light.position - fragPos);

    // diffuse lighting
    float diff = max(dot(normal, lightDir), 0.0);

    // specular lighting
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

    // combine
    vec3 ambient  = light.ambient  * vec3(texture(material.diffuse, texcoord));
    vec3 diffuse  = light.diffuse  * diff * vec3(texture(material.diffuse, texcoord));
    vec3 specular = light.specular * spec + vec3(texture(material.specular, texcoord));
    ambient  *= attenuation;
    diffuse  *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}

vec2 hash2(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    p += viewPos.zy;
    p += viewPos.xy;
    p += viewPos.yx;
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

float calcShadow(vec4 lightSpaceFragPos) {
    vec3 projCoords = lightSpaceFragPos.xyz / lightSpaceFragPos.w;
    // transform to [0-1] range
    projCoords = projCoords * 0.5 + 0.5;

    float closestDepth = texture(shadowMap, projCoords.xy).r;
    float currentDepth = projCoords.z;

    float bias = max(0.05 * (1.0 - dot(normal, -dirLight.direction)), 0.000000001);
    float shadow = currentDepth - bias > closestDepth ? 1.0 : 0.0;
//    float shadow = 0.0;
//    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
//    for (int x = -4; x <= 4; x++) {
//        for (int y = -4; y <= 4; y++) {
//            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
//            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
//        }
//    }
//    shadow /= 64.0;

    // keep shadow at 0 ouside far plane
    if (projCoords.z > 1.0)
        shadow = 0.0;

    return clamp(shadow - MIN_SHADOW_INTENSITY, 0.0, 1.0);
}

