using System;
using Cairo;

namespace AnimLib {
    class CairoRenderer : IRenderer, IDisposable {
        private RenderState _rs;

        public CairoRenderer(RenderState rs) {
            this._rs = rs;
        }

        public void RenderCircles(CircleState[] circles) {
        }

        public void RenderCubes(CubeState[] cubes, M4x4 mat) {
        }

        public void RenderRecangles(RectangleState[] rectangles, M4x4 mat, M4x4 orthoMat) {
        }

        public void RenderScene(WorldSnapshot ss, SceneView sv) {
            var pb = sv.Buffer;
            var pbSize = pb.Size; // render buffer size
            var sceneCamera = ss.Camera;
            // TODO: blit software buffer to GL
        }

        public void Dispose() {
        }
    }
}
