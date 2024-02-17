namespace AnimLib;

internal partial class GlKawaseBlur {

string effectVert = @"#version 330 core
layout(location = 0) in vec4 position;
void main() {
    gl_Position = position;
}";

// 4-tap Kawase blur
/*
string kawaseBlurDown4Frag = @"#version 330 core
layout(location = 0) out vec4 outColor;

uniform sampler2D _MainTex;
uniform ivec2 _ViewportSize;

uniform float _Threshold = 1.0f;

vec4 sample(vec4 p1, vec4 p2, vec4 p3, vec4 p4) {
    p1.rgb = max(p1.rgb - _Threshold, 0.0);
    p2.rgb = max(p2.rgb - _Threshold, 0.0);
    p3.rgb = max(p3.rgb - _Threshold, 0.0);
    p4.rgb = max(p4.rgb - _Threshold, 0.0);
    return (p1 + p2 + p3 + p4) / 4.0;
}

void main() {
    // half pixel size in normalized texture coordinates
    vec2 halfPixel = vec2(1) / (2.0 * _ViewportSize.xy);
    // coordinate of current pixel (gl_FragCoord is pixel center)
    vec2 texCoord = gl_FragCoord.xy / _ViewportSize;

    // lower left sample point
    vec2 sampleCoord1 = texCoord + vec2(-halfPixel.x, -halfPixel.y);
    // lower right sample point
    vec2 sampleCoord2 = texCoord + vec2(halfPixel.x, -halfPixel.y);
    // upper right sample point
    vec2 sampleCoord3 = texCoord + vec2(halfPixel.x, halfPixel.y);
    // upper left sample point
    vec2 sampleCoord4 = texCoord + vec2(-halfPixel.x, halfPixel.y);

    vec4 color1 = texture(_MainTex, sampleCoord1, 0);
    vec4 color2 = texture(_MainTex, sampleCoord2, 0);
    vec4 color3 = texture(_MainTex, sampleCoord3, 0);
    vec4 color4 = texture(_MainTex, sampleCoord4, 0);

    outColor = sample(color1, color2, color3, color4);
}
";
*/

// 13-tap Kawase blur
string kawaseBlurDown13Frag = @"#version 330 core
layout(location = 0) out vec4 outColor;

uniform sampler2D _MainTex;
uniform ivec2 _ViewportSize;
uniform float _Threshold = 1.0f;
uniform int _MipLevel = 0;

vec4 sample(vec4 p1, vec4 p2, vec4 p3, vec4 p4) {
    p1.rgb = max(p1.rgb - _Threshold, 0.0);
    p2.rgb = max(p2.rgb - _Threshold, 0.0);
    p3.rgb = max(p3.rgb - _Threshold, 0.0);
    p4.rgb = max(p4.rgb - _Threshold, 0.0);
    return (p1 + p2 + p3 + p4) / 4.0;
}

void main() {
    vec2 texelSize = vec2(1) / _ViewportSize.xy;

    // coordinate of current pixel (gl_FragCoord is pixel center)
    vec2 texCoord = gl_FragCoord.xy / _ViewportSize;

    // K L M
    //  I J
    // F G H
    //  D E
    // A B C

    vec4 AC = textureLod(_MainTex, texCoord + texelSize*vec2(-1.0, -1.0), _MipLevel);
    vec4 BC = textureLod(_MainTex, texCoord + texelSize*vec2( 0.0, -1.0), _MipLevel);
    vec4 CC = textureLod(_MainTex, texCoord + texelSize*vec2( 1.0, -1.0), _MipLevel);
    vec4 DC = textureLod(_MainTex, texCoord + texelSize*vec2(-0.5, -0.5), _MipLevel);
    vec4 EC = textureLod(_MainTex, texCoord + texelSize*vec2( 0.5, -0.5), _MipLevel);
    vec4 FC = textureLod(_MainTex, texCoord + texelSize*vec2(-1.0,  0.0), _MipLevel);
    vec4 GC = textureLod(_MainTex, texCoord                             , _MipLevel);
    vec4 HC = textureLod(_MainTex, texCoord + texelSize*vec2( 1.0,  0.0), _MipLevel);
    vec4 IC = textureLod(_MainTex, texCoord + texelSize*vec2(-0.5,  0.5), _MipLevel);
    vec4 JC = textureLod(_MainTex, texCoord + texelSize*vec2( 0.5,  0.5), _MipLevel);
    vec4 KC = textureLod(_MainTex, texCoord + texelSize*vec2(-1.0,  1.0), _MipLevel);
    vec4 LC = textureLod(_MainTex, texCoord + texelSize*vec2( 0.0,  1.0), _MipLevel);
    vec4 MC = textureLod(_MainTex, texCoord + texelSize*vec2( 1.0,  1.0), _MipLevel);

    vec4 LL = sample(BC, AC, FC, GC);
    vec4 LR = sample(CC, BC, GC, HC);
    vec4 UR = sample(HC, GC, LC, MC);
    vec4 UL = sample(GC, FC, KC, LC);
    vec4 MM = sample(GC, DC, EC, IC);

    outColor = 0.125 * (LL + LR + UR + UL) + 0.5 * MM;
}
";

string kawaseBlurUpFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;

uniform sampler2D _MainTex;
uniform sampler2D _PrevTex;

uniform bool _UsePrevTex = false;

// mip levels
uniform int _MipLevel = 0;

uniform ivec2 _ViewportSize;

uniform float _Radius = 1.0f;

void main() {
    vec2 texelSize = vec2(1) / _ViewportSize.xy;
    vec2 texCoord = gl_FragCoord.xy / _ViewportSize;

    vec2 d = texelSize * _Radius;

    int lod = _MipLevel;

    vec4 s = vec4(0.0);
    s += textureLod(_MainTex, texCoord + vec2(-d.x, -d.y), lod) * 1.0;
    s += textureLod(_MainTex, texCoord + vec2( 0.0, -d.y), lod) * 2.0;
    s += textureLod(_MainTex, texCoord + vec2( d.x, -d.y), lod) * 1.0;

    s += textureLod(_MainTex, texCoord + vec2(-d.x,  0.0), lod) * 2.0;
    s += textureLod(_MainTex, texCoord                   , lod) * 4.0;
    s += textureLod(_MainTex, texCoord + vec2( d.x,  0.0), lod) * 2.0;

    s += textureLod(_MainTex, texCoord + vec2(-d.x,  d.y), lod) * 1.0;
    s += textureLod(_MainTex, texCoord + vec2( 0.0,  d.y), lod) * 2.0;
    s += textureLod(_MainTex, texCoord + vec2( d.x,  d.y), lod) * 1.0;

    if ( _UsePrevTex ) {
        vec4 p = texture(_PrevTex, texCoord, 0);
        outColor = p + (1.0 / 16.0) * s;
    } else {
        outColor = (1.0 / 16.0) * s;
    }
}
";
}
