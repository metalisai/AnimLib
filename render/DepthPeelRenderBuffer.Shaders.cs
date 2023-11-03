namespace AnimLib {
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
                discard;
            }
        }";
    }
}
