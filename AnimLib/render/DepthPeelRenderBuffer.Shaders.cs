namespace AnimLib;
internal partial class DepthPeelRenderBuffer {
string canvasBlitVert = @"#version 330 core
layout(location = 0) in vec4 position;
void main() {
    gl_Position = position;
}";

string canvasBlitFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
uniform sampler2D _MainTex;
uniform int _EntityId;
void main() {
    vec4 srcColor = texelFetch(_MainTex, ivec2(gl_FragCoord.xy), 0);
    if(srcColor.a > 0.0) {
        outColor = srcColor;
        outEntityId = _EntityId;
    } else {
        outColor = srcColor;
    }
}";

string canvasBlitFragFloat = @"#version 330 core
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
uniform sampler2D _MainTex;
uniform int _EntityId;
void main() {
    vec2 textureSize = vec2(textureSize(_MainTex, 0));
    vec2 texCoord = gl_FragCoord.xy / textureSize;
    vec4 srcColor = texture(_MainTex, texCoord, 0);
    if(srcColor.a > 0.0) {
        outColor = srcColor;
        outEntityId = _EntityId;
    } else {
        outColor = srcColor;
    }
}";

string effectVert = @"#version 330 core
layout(location = 0) in vec4 position;
void main() {
    gl_Position = position;
}";

string blurFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;
uniform sampler2D _MainTex;
float weight[5] = float[] (0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);
uniform bool _Horizontal;
void main() {
    vec4 srcColor = texelFetch(_MainTex, ivec2(gl_FragCoord.xy), 0) * weight[0];

    if (_Horizontal) {
        for(int i = 1; i < 5; i++) {
            srcColor += weight[i]*texelFetch(_MainTex, ivec2(gl_FragCoord.xy) + ivec2(i, 0), 0);
            srcColor += weight[i]*texelFetch(_MainTex, ivec2(gl_FragCoord.xy) - ivec2(i, 0), 0);
        }
    } else {
        for(int i = 1; i < 5; i++) {
            srcColor += weight[i]*texelFetch(_MainTex, ivec2(gl_FragCoord.xy) + ivec2(0, i), 0);
            srcColor += weight[i]*texelFetch(_MainTex, ivec2(gl_FragCoord.xy) - ivec2(0, i), 0);
        }
    }

    outColor = srcColor;
}";

string bloomFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;
uniform sampler2D _MainTex;
float weight[5] = float[] (1.0, 0.645946, 0.4216216, 0.154054, 0.006216);
uniform bool _Horizontal;
uniform ivec2 _ViewportSize;

uniform float _BloomThreshold = 1.0;
uniform float _Radius = 3.0f;
uniform float _Sigma = 3.5;
uniform float _Amount = 1.0;
uniform float _Saturation = 0.15;

vec4 colorFilter(vec4 finput) {
    vec3 filtered = max(finput.rgb-vec3(_BloomThreshold), vec3(0.0));
    // calculate luminance
    float brightness = dot(filtered, vec3(0.2126, 0.7152, 0.0722));
    filtered += _Saturation*vec3(brightness);
    return vec4(filtered, clamp(brightness, 0.0, 1.0));
}

void main() {
    vec2 texCoord = gl_FragCoord.xy / _ViewportSize;

    vec4 srcColor = colorFilter(texture(_MainTex, texCoord, 0));

    vec3 bloomColor = srcColor.rgb;
    float alpha = 0.0;

    const int steps = 20;
    vec2 stepSize = vec2(1.0, 1.0) / textureSize(_MainTex, 0) * _Radius;

    float sigma = _Sigma;
    float amount = _Amount;

    for(int i = -steps+1; i < steps; i++) {
    for(int j = -steps+1; j < steps; j++) {
        float val = -(i*i + j*j)/(2.0*sigma*sigma);
        float weight = exp(val) / (2.0*3.141592*sigma*sigma);
        vec2 offset = vec2(i, j)*stepSize;
        vec4 sample = colorFilter(texture(_MainTex, texCoord + offset, 0));
        bloomColor += amount*sample.rgb*weight;
        alpha += amount*sample.a*weight;
    }}

    outColor = vec4(bloomColor, clamp(alpha, 0.0, 1.0));
    //outColor = vec4(bloomColor, 0.0);
    //outColor = vec4(srcColor.rgb, 0.5);
    //outColor = vec4(1.0, 0.0, 0.0, 1.0);
}";

}
