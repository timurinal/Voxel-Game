#define PI 3.14159265

vec4 boxBlur(sampler2D image, vec2 uv, int xSize, int ySize) {
    int totalSamples = (xSize + 1) * (ySize + 1);
    int hXSize = xSize / 2;
    int hYSize = ySize / 2;
    
    vec4 totalColour = vec4(0.0);
    
    vec2 texelSize = 1.0 / textureSize(image, 0);
    for (int x = -hXSize; x <= hXSize; x++) {
        for (int y = -hYSize; y <= hYSize; y++) {
            vec2 sampleUv = uv + vec2(x, y) * texelSize;
            sampleUv = clamp(sampleUv, 0.0, 1.0);
            vec4 col = texture(image, sampleUv);
            totalColour += col;
        }
    }
    
    totalColour /= float(totalSamples);
    
    return totalColour;
}

vec4 gaussianBlur(sampler2D image, vec2 uv, float sigma, int kernelRadius) {
    float twoSigmaSquared = 2.0 * sigma * sigma;
    float sigmaRoot = sqrt(twoSigmaSquared * PI);

    vec4 totalColour = vec4(0.0);
    float weightSum = 0.0;

    vec2 texelSize = 1.0 / textureSize(image, 0);
    
    for (int i = -kernelRadius; i <= kernelRadius; i++) {
        for (int j = -kernelRadius; j <= kernelRadius; j++) {
            // Calculate weight based on Gaussian function
            float distance = float(i * i + j * j);
            float weight = exp(-distance / twoSigmaSquared) / sigmaRoot;

            vec4 color = texture(image, uv + vec2(float(i), float(j)) * texelSize);
            totalColour += color * weight;
            weightSum += weight;
        }
    }

    totalColour /= weightSum;
    return totalColour;
}