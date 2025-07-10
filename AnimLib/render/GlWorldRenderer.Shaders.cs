namespace AnimLib;

internal partial class GlWorldRenderer {

string vertShader = @"#version 330 core
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
out vec4 v_color;
uniform mat4 _ModelToClip;
void main() {
    gl_Position = _ModelToClip*position;
    v_color = color;
}";

string arrowFrag = @"#version 330
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
in vec3 v_color_hsv;
in vec2 v_texCoord;
uniform vec4 _Color;
uniform sampler2D _depthPeelTex;
uniform float Length;
uniform float Width;

uniform int _EntityId;

vec3 hsv2rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float triangleDist(vec2 p, float width, float height)
{
	vec2 n = normalize(vec2(height/2.0, width));
	return max(	abs(p).y*n.y + p.x*n.x - (width*n.x), -p.x);
}

float boxDist(vec2 p, vec2 size, float radius)
{
	size -= vec2(radius);
	vec2 d = abs(p) - size;
  	return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - radius;
}

void main() {
    float depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    //vec3 outColorRGB = _Color.rgb*v_color.rgb;
    vec3 outColorRGB = _Color.rgb*hsv2rgb(v_color_hsv);
    //float dist = triangleDist(v_texCoord-vec2(0.8, 0.5), 0.2, 1.0);
    float uvLen = (0.3/Length)*(Width/0.1);

    const float smoothAmount = 1.0;

    float dist = min(boxDist(v_texCoord-vec2((1.0-uvLen*0.85)/2.0, 0.5), vec2(0.5-uvLen*0.85/2.0, 0.25), 0.01), triangleDist(v_texCoord-vec2(1.0-uvLen, 0.5), uvLen, 1.0));
    float xdy = abs(dFdx(dist));
    float ydy = abs(dFdy(dist));
    float dy = smoothAmount * length(vec2(xdy, ydy));
    float edge = smoothstep(dy, -dy, dist);

    float alpha = _Color.a*v_color.a*edge;
    outColor = vec4(outColorRGB, alpha);
    //outColor = vec4(vec3(v_texCoord, 1.0), alpha);
    outEntityId = _EntityId;
}";

string quadBezierVert = @"#version 330 core
layout(location = 0) in vec4 position;
uniform mat4 _ModelToClip;
void main() {
    gl_Position = /*_ModelToClip**/position;
    //v_position = position.xy/position.w;
}";

string quadBezierGeo = @"#version 330 core
layout (points) in;
layout (triangle_strip, max_vertices=4) out;
uniform vec4 _Point1;
uniform vec4 _Point2;
uniform vec4 _Point3;
uniform mat4 _ModelToClip;
uniform ivec2 _ScreenSize;
uniform float _Width;
out vec2 v_position;
flat out vec4 p1;
flat out vec4 p2;
flat out vec4 p3;
vec2 toScreen(vec4 p) {
    return ((p.xy/p.w)+vec2(1.0))*(_ScreenSize/2);
}
void main() {
    p1 = _ModelToClip*_Point1;
    p2 = _ModelToClip*_Point2;
    p3 = _ModelToClip*_Point3;
    vec3 c1 = p1.xyz / p1.w;
    vec3 c2 = p2.xyz / p2.w;
    vec3 c3 = p3.xyz / p3.w;
    vec3 cmin = min(min(c1, c2), c3);
    vec3 cmax = max(max(c1, c2), c3);
    if(cmin.z < -1.0 || p1.w < 0)
        return;
    if(cmax.z > 1.0)
        return;
    // TODO: use tight bounds
    vec2 sssize = _Width / _ScreenSize;
    vec2 min = min(min(c1.xy, c2.xy), c3.xy) - sssize;
    vec2 max = max(max(c1.xy, c2.xy), c3.xy) + sssize;
    //min = vec2(-0.9, -0.9);
    //max = vec2(0.9, 0.9);
    gl_Position = vec4(min, 0.0f, 1.0f);
    v_position = toScreen(gl_Position);
    EmitVertex();
    gl_Position = vec4(vec2(max.x, min.y), 0.0f, 1.0f);
    v_position = toScreen(gl_Position);
    EmitVertex();
    gl_Position = vec4(vec2(min.x, max.y), 0.0f, 1.0f);
    v_position = toScreen(gl_Position);
    EmitVertex();
    gl_Position = vec4(max, 0.0f, 1.0f);
    v_position = toScreen(gl_Position);
    EmitVertex();
    EndPrimitive();
}";

string quadBezierFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec2 v_position;
flat in vec4 p1;
flat in vec4 p2;
flat in vec4 p3;
uniform vec4 _Point1;
uniform vec4 _Point2;
uniform vec4 _Point3;
uniform vec4 _Color;
uniform mat4 _ModelToClip;
uniform ivec2 _ScreenSize;
uniform float _Width;
uniform int _EntityId;
uniform sampler2D _depthPeelTex;

// Copy paste from - https://www.shadertoy.com/view/MlKcDD

// Test if point p crosses line (a, b), returns sign of result
float testCross(vec2 a, vec2 b, vec2 p) {
    return sign((b.y-a.y) * (p.x-a.x) - (b.x-a.x) * (p.y-a.y));
}

// Determine which side we're on (using barycentric parameterization)
float signBezier(vec2 A, vec2 B, vec2 C, vec2 p)
{
    vec2 a = C - A, b = B - A, c = p - A;
    vec2 bary = vec2(c.x*b.y-b.x*c.y,a.x*c.y-c.x*a.y) / (a.x*b.y-b.x*a.y);
    vec2 d = vec2(bary.y * 0.5, 0.0) + 1.0 - bary.x - bary.y;
    return mix(sign(d.x * d.x - d.y), mix(-1.0, 1.0,
        step(testCross(A, B, p) * testCross(B, C, p), 0.0)),
        step((d.x - d.y), 0.0)) * testCross(A, C, B);
}

// Solve cubic equation for roots
vec3 solveCubic(float a, float b, float c)
{
    float p = b - a*a / 3.0, p3 = p*p*p;
    float q = a * (2.0*a*a - 9.0*b) / 27.0 + c;
    float d = q*q + 4.0*p3 / 27.0;
    float offset = -a / 3.0;
    if(d >= 0.0) {
        float z = sqrt(d);
        vec2 x = (vec2(z, -z) - q) / 2.0;
        vec2 uv = sign(x)*pow(abs(x), vec2(1.0/3.0));
        return vec3(offset + uv.x + uv.y);
    }
    float v = acos(-sqrt(-27.0 / p3) * q / 2.0) / 3.0;
    float m = cos(v), n = sin(v)*1.732050808;
    return vec3(m + m, -n - m, n - m) * sqrt(-p / 3.0) + offset;
}

// Find the signed distance from a point to a bezier curve
float sdBezier(vec2 A, vec2 B, vec2 C, vec2 p)
{
    B = mix(B + vec2(1e-4), B, abs(sign(B * 2.0 - A - C)));
    vec2 a = B - A, b = A - B * 2.0 + C, c = a * 2.0, d = A - p;
    vec3 k = vec3(3.*dot(a,b),2.*dot(a,a)+dot(d,b),dot(d,a)) / dot(b,b);
    vec3 t = clamp(solveCubic(k.x, k.y, k.z), 0.0, 1.0);
    vec2 pos = A + (c + b*t.x)*t.x;
    float dis = length(pos - p);
    pos = A + (c + b*t.y)*t.y;
    dis = min(dis, length(pos - p));
    pos = A + (c + b*t.z)*t.z;
    dis = min(dis, length(pos - p));
    return dis * signBezier(A, B, C, p);
}
vec2 toScreen(vec4 p) {
    return ((p.xy/p.w)+vec2(1.0))*(_ScreenSize/2);
}
void main() {
    float depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    vec2 c1 = toScreen(p1);
    vec2 c2 = toScreen(p2);
    vec2 c3 = toScreen(p3);

    float ds = abs(sdBezier(c1, c2, c3, v_position));

    float w = _Width/2.0;
    float d = smoothstep(w+1.0, w-1.0, ds);
    outColor = vec4(_Color.rgb, d);
    if(d > 0.0) {
        outEntityId = _EntityId;
    } else {
        discard;
    }
}";

string rectangleVert = @"#version 330 core
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texcoord;
out vec4 v_color;
out vec3 v_color_hsv;
out vec3 v_modelPos;
out vec2 v_texCoord;
uniform mat4 _ModelToClip;

vec3 rgb2hsv(vec3 c) {
  vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
  vec4 p = c.g < c.b ? vec4(c.bg, K.wz) : vec4(c.gb, K.xy);
  vec4 q = c.r < p.x ? vec4(p.xyw, c.r) : vec4(c.r, p.yzx);

  float d = q.x - min(q.w, q.y);
  float e = 1.0e-10;
  return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

void main() {
    gl_Position = _ModelToClip*position;
    v_modelPos = position.xyz;
    v_color = color;
    v_texCoord = texcoord;
    v_color_hsv = rgb2hsv(color.rgb);
}";
string rectangleFrag = @"#version 330 core
#extension GL_ARB_sample_shading : enable
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
in vec2 v_texCoord;
uniform vec4 _Color;
uniform vec4 _Outline;
uniform int _EntityId;
uniform sampler2DMS _depthPeelTexMs;
uniform sampler2D _depthPeelTex;
uniform bool _Multisample;
void main() {
    float depth;
    if (_Multisample) {
        depth = texelFetch(_depthPeelTexMs, ivec2(gl_FragCoord.xy), gl_SampleID).x;
    } else {
        depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    }
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    const float r = 0.5;
    const float smoothAmount = 3.0;
    vec2 cpos = v_texCoord - vec2(0.5);
    vec2 dist = abs(cpos);
    vec2 xdxy = dFdx(cpos);
    vec2 ydxy = dFdy(cpos);
    float ldx = length(vec2(xdxy.x, ydxy.x));
    float ldy = length(vec2(xdxy.y, ydxy.y));
    float bx = 1.0 - smoothAmount*ldx;
    float by = 1.0 - smoothAmount*ldy;
    vec2 b = vec2(bx, by);
    vec2 a = smoothstep(vec2(r), r*b, dist);

    // outline
    vec2 bcs = vec2(1-3.0*ldx, 1-3.0*ldy);
    vec2 blendColor = smoothstep(r*bcs, r*b*bcs, dist);
    float blendColorm = min(blendColor.x, blendColor.y);

    float alpha = _Color.a*v_color.a;
    vec3 colorRGB = _Color.rgb*v_color.rgb * alpha;
    //outColor = mix(_Outline, vec4(colorRGB, alpha), blendColorm);
    outColor = vec4(colorRGB, alpha);
    outEntityId = _EntityId;
}";

string solidColorVert = @"#version 330 core
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
out vec4 v_color;
out vec3 v_modelPos;
out vec2 v_texCoord;
uniform mat4 _ModelToClip;

void main() {
    gl_Position = _ModelToClip*position;
    v_modelPos = position.xyz;
    v_color = color;
}";
string solidColorFrag = @"#version 330 core
#extension GL_ARB_sample_shading : enable
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
uniform vec4 _Color;
uniform vec4 _Outline;
uniform int _EntityId;
uniform sampler2DMS _depthPeelTexMs;
uniform sampler2D _depthPeelTex;
uniform bool _Multisample;
void main() {
    float depth;
    if (_Multisample) {
        depth = texelFetch(_depthPeelTexMs, ivec2(gl_FragCoord.xy), gl_SampleID).x;
    } else {
        depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    }
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    float alpha = _Color.a*v_color.a;
    vec3 colorRGB = _Color.rgb*v_color.rgb * alpha;
    outColor = vec4(colorRGB, alpha);
    outEntityId = _EntityId;
}";


string circleVert = @"#version 330 core
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
out vec4 v_color;
out vec3 v_modelPos;
uniform mat4 _ModelToClip;
void main() {
    gl_Position = _ModelToClip*position;
    v_modelPos = position.xyz;
    v_color = color;
}";
string circleFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
in vec3 v_modelPos;
uniform vec4 _Color;
uniform vec4 _Outline;
uniform sampler2D _depthPeelTex;
uniform int _EntityId;
void main() {
    float depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    if(gl_FragCoord.z >= depth) {
        discard;
    }
    vec2 dx = dFdy(v_modelPos.xy);
    // TODO: non uniform scaling doesn't work (need derivative on y axis)
    float blendPrec = 1.0 - 4.0*length(dx);
    const float radius = 0.5;
    vec3 colRGB;
    float alpha;
    float dist = length(v_modelPos); // distance from circle center to current frag
    if(_Outline.a == 0.0) {
        colRGB = _Color.rgb*v_color.rgb;
        alpha = smoothstep(radius, radius*blendPrec, dist)*_Color.a*v_color.a;
    } else {
        alpha = smoothstep(radius, radius*blendPrec, dist)*_Color.a*v_color.a;
        float bcs = 1.0 - length(dx)*_Outline.a*3.0;
        float borderBlend = smoothstep(max(radius*bcs,0.1), radius*bcs*blendPrec, dist);
        colRGB = mix(_Outline.rgb, _Color.rgb*v_color.rgb, borderBlend);
    }
    outColor = vec4(colRGB, alpha);
    outEntityId = _EntityId;
}";

string staticLineVert = @"#version 330 core
layout(location = 0) in vec4 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 edgeCoord;
out vec4 v_color;
out vec2 v_edgeCoord;
out vec3 v_modelPos;
uniform mat4 _ModelToClip;
void main() {
    gl_Position = _ModelToClip*position;
    v_color = color;
    v_edgeCoord = edgeCoord;
    v_modelPos = position.xyz;
}";

string staticLineFrag = @"#version 330
#extension GL_ARB_sample_shading : enable
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
in vec2 v_edgeCoord;
uniform vec4 _Color;
uniform vec4 _Outline;
uniform sampler2DMS _depthPeelTexMs;
uniform sampler2D _depthPeelTex;
uniform bool _Multisample;
uniform int _EntityId;
void main() {
    float depth;
    if (_Multisample) {
        depth = texelFetch(_depthPeelTexMs, ivec2(gl_FragCoord.xy), gl_SampleID).x;
    } else {
        depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    }
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    vec3 outColorRGB = _Color.rgb*v_color.rgb;
    outColor = vec4(outColorRGB, _Color.a*v_color.a);
    outEntityId = _EntityId;
}";

string cubeFrag = @"#version 330
#extension GL_ARB_sample_shading : enable
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
in vec3 v_modelPos;
uniform vec4 _Color;
uniform vec4 _Outline;
uniform sampler2DMS _depthPeelTexMs;
uniform sampler2D _depthPeelTex;
uniform bool _Multisample;
uniform int _EntityId;
void main() {
    float depth;
    if (_Multisample) {
        depth = texelFetch(_depthPeelTexMs, ivec2(gl_FragCoord.xy), gl_SampleID).x;
    } else {
        depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    }
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    vec3 outColorRGB = _Color.rgb*v_color.rgb;
    float alpha = _Color.a*v_color.a;

    const float r = 0.5;
    const float smoothAmount = 2.0;
    vec3 absp = abs(v_modelPos);
    // sort where x is smallest
    if(absp.x > absp.y) {
        float temp = absp.y;
        absp.y = absp.x;
        absp.x = temp;
    }
    if(absp.x > absp.z) {
        float temp = absp.z;
        absp.z = absp.x;
        absp.x = temp;
    }
    if(absp.y > absp.z) {
        float temp = absp.z;
        absp.z = absp.y;
        absp.y = temp;
    }
    float dist = max(absp.x, absp.y);
    float ddist = 1.0 - 3.0*length(vec2(dFdx(dist),dFdy(dist)));
    float mul = smoothstep(0.49*ddist, 0.49, dist);

    outColor = mix(vec4(outColorRGB*alpha, alpha), _Outline, mul);
    outEntityId = _EntityId;
}";

string meshGeom = @"#version 330
layout (triangles) in;
layout (triangle_strip, max_vertices=3) out;

in vec4 v_color[];
out vec4 g_color;
out vec3 g_bary;

void main() {
    g_color = v_color[0];
    g_bary = vec3(1.0, 0.0, 0.0);
    gl_Position = gl_in[0].gl_Position;
    EmitVertex();
    g_color = v_color[1];
    g_bary = vec3(0.0, 1.0, 0.0);
    gl_Position = gl_in[1].gl_Position;
    EmitVertex();
    g_color = v_color[2];
    g_bary = vec3(0.0, 0.0, 1.0);
    gl_Position = gl_in[2].gl_Position;
    EmitVertex();
    EndPrimitive();
}
";

    string meshFrag = @"#version 330
#extension GL_ARB_sample_shading : enable
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 g_color;
in vec3 g_bary;
uniform vec4 _Color;
uniform vec4 _Outline;
uniform sampler2DMS _depthPeelTexMs;
uniform sampler2D _depthPeelTex;
uniform bool _Multisample;
uniform int _EntityId;
void main() {
    float depth;
    if (_Multisample) {
        depth = texelFetch(_depthPeelTexMs, ivec2(gl_FragCoord.xy), gl_SampleID).x;
    } else {
        depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    }
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    vec3 tint = vec3(gl_FragCoord.z);
    float d = min(min(g_bary.x, g_bary.y), g_bary.z);

    vec3 dxbary = dFdx(g_bary);
    float dxd = min(min(dxbary.x, dxbary.y), dxbary.z);
    vec3 dybary = dFdy(g_bary);
    float dyd = min(min(dybary.x, dybary.y), dybary.z);
    float dd = length(vec2(dxd, dyd));

    float edgeLocation = dd;
    float edge = smoothstep(edgeLocation-dd, edgeLocation, d);
    vec3 outColorRGB = _Color.rgb*g_color.rgb;
    float alpha = _Color.a*g_color.a;
    outColor = mix(vec4(outColorRGB*alpha, alpha), _Outline, 1.0-edge);
    outEntityId = _EntityId;
}";

    string texRectFrag = @"#version 330 core
#extension GL_ARB_sample_shading : enable
layout(location = 0) out vec4 outColor;
in vec4 v_color;
in vec3 v_modelPos;
in vec2 v_texCoord;
uniform bool _Multisample;
uniform vec4 _Color;
uniform vec4 _Outline;
uniform sampler2D _MainTex;
uniform sampler2DMS _depthPeelTexMs;
uniform sampler2D _depthPeelTex;
void main() {
    float depth;
    if (_Multisample) {
        depth = texelFetch(_depthPeelTexMs, ivec2(gl_FragCoord.xy), gl_SampleID).x;
    } else {
        depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    }
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    vec4 texColor = texture(_MainTex, vec2(v_texCoord.x, 1.0 - v_texCoord.y));
    outColor = texColor;
}";

string textVert = @"#version 330 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;
layout(location = 3) in int entityId;
out vec4 v_color;
out vec2 v_texCoord;
flat out int v_entityId;
uniform mat4 _ModelToClip;

void main() {
    gl_Position = _ModelToClip*vec4(position, 1.0f);
    v_color = color;
    v_texCoord = texCoord;
    v_entityId = entityId;
}";

string textFrag = @"#version 330 core
layout(location = 0) out vec4 outColor;
layout(location = 1) out int outEntityId;
in vec4 v_color;
in vec2 v_texCoord;
flat in int v_entityId;
uniform sampler2D _MainTex;
uniform sampler2D _depthPeelTex;
void main() {
    float depth = texelFetch(_depthPeelTex, ivec2(gl_FragCoord.xy), 0).x;
    if(gl_FragCoord.z >= depth) {
        discard;
    }

    vec2 texCoord = vec2(v_texCoord.x, 1.0 - v_texCoord.y);
    vec3 outColorRGB = v_color.rgb;
    float alpha = v_color.a*texture2D(_MainTex, texCoord).r;
    outColor = vec4(outColorRGB, alpha);
    outEntityId = v_entityId;
}";
}
