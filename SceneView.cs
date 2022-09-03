using ImGuizmoNET;
using ImGuiNET;
using System.Collections.Generic;
using System;

namespace AnimLib {
    
    public class SceneView
    {
        public struct TransformResult {
            public bool posChanged;
            public bool rotChanged;
            public bool scaleChanged;
            public Vector3 newPosition;
            public Quaternion newRotation;
            public Vector3 newScale;
        }

        public struct OrthoResult {
            public bool posChanged;
            public bool rotChanged;
            public Vector2 newPosition;
            public float newRotation;
        }

        public struct PointGizmoState {
            public bool dragging;
        }

        private IRenderBuffer renderBuffer;
        private Dictionary<string, PointGizmoState> pointGizmos = new Dictionary<string, PointGizmoState>();
        int x, y, width, height; // coordnates inside window 
        int bufferWidth, bufferHeight;
        (int,int,int,int)? lastArea;
        CameraState lastCam = new PerspectiveCameraState();

        public CameraState LastCamera {
            get {
                return lastCam;
            }
        }

        public SceneView(IRenderBuffer rb, int x, int y, int width, int height)
        {
            renderBuffer = rb;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.bufferWidth = rb.Size.Item1;
            this.bufferHeight = rb.Size.Item2;
        }

        public SceneView(int x, int y, int width, int height, int bufferWidth, int bufferHeight)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.bufferWidth = bufferWidth;
            this.bufferHeight = bufferHeight;
        }

        public void ResizeBuffer(int w, int h) {
            renderBuffer?.Resize(w, h);
            this.bufferWidth = w;
            this.bufferHeight = h;
        }

        public int TextureHandle {
            get {
                return renderBuffer?.Texture() ?? 0;
            }
        }

        public int BufferWidth {
            get {
                return this.bufferWidth;
            }
        }

        public int BufferHeight {
            get {
                return this.bufferHeight;
            }
        }

        public IRenderBuffer Buffer {
            get {
                return renderBuffer;
            }
            set {
                renderBuffer?.Dispose();
                Debug.TLog(value.Size.ToString());
                System.Diagnostics.Debug.Assert(value.Size == (this.bufferWidth, this.bufferHeight));
                renderBuffer = value;
            }
        }

        public (int,int,int,int)? LastArea {
            get {
                return lastArea;
            }
        }

        public void BeginFrame() {

        }

        public void PostRender(CameraState cam) {
            this.lastCam = cam.Clone() as CameraState;
        }

        public (int,int,int,int) CalculateArea(int x, int y, int width, int height)
        {
            // calculate scene view "bars" (aspect ratio correction)
            double targetAspect = (double)this.bufferWidth/(double)this.bufferHeight;
            double aspect = (double)width / (double)height;
            int w, h;
            int wShrink = 0;
            int hShrink = 0;
            if (aspect > targetAspect) { // too wide, vertical bars
                int nw = (int)(height * targetAspect);
                wShrink = (int)width - nw;
                if((wShrink&1) == 1) // make even to please the pixel-perfect OCD people
                    wShrink++;
            } else if (aspect < targetAspect) { // too tall, horizontal bars
                int nh = (int)((double)width / targetAspect);
                hShrink = (int)height - nh;
                if((hShrink&1) == 1)
                    hShrink++;
            }
            w = (int)width - wShrink;
            h = (int)height - hShrink;
            var ret = (wShrink/2 , y, w, h);
            return ret;
        }

        public void SetArea(int x, int y, int width, int height)
        {
            lastArea = (x,y,width,height);
        }

        public TransformResult? DoSelectionGizmo(Vector3 pos, Quaternion rot, Vector3 scale) {
            if (lastArea == null)
                return null;
            var ret = new TransformResult {
                newPosition = pos,
                newRotation = rot,
                newScale = scale,
            };
            var a = lastArea.Value;
            ImGuizmo.SetRect(a.Item1, a.Item2, a.Item3, a.Item4);
            ImGuizmo.Enable(true);
            ImGuizmo.SetOrthographic(false);
            var cam = lastCam as PerspectiveCameraState;
            if (cam != null) {
                var viewM = cam.CreateWorldToViewMatrix();
                var projM = cam.CreateViewToClipMatrix((float)renderBuffer.Size.Item1/(float)renderBuffer.Size.Item2);
                var mat = M4x4.TRS(pos, rot, scale);
                var op = OPERATION.TRANSLATE;
                var mode = MODE.WORLD;
                if(ImGuizmo.Manipulate(ref viewM.m11, ref projM.m11, op, mode, ref mat.m11))
                {
                    Vector3 translation = new Vector3(), s = new Vector3();
                    M3x3 r = new M3x3();
                    ImGuizmo.DecomposeMatrixToComponents(ref mat.m11, ref translation.x, ref r.m11, ref s.x);
                    switch(op) {
                    case OPERATION.TRANSLATE:
                        ret.newPosition = translation;
                        ret.posChanged = true;
                    break;
                    case OPERATION.ROTATE:
                        ret.newRotation = new Quaternion(ref r);
                        ret.rotChanged = true;
                    break;
                    case OPERATION.SCALE:
                        ret.newScale = s;
                        ret.scaleChanged = true;
                    break;
                    }
                }
            }
            return ret;
        }

        public OrthoResult? DoOrthoGizmo(Vector2 pos, float rotation) {
            if(lastArea == null)
                return null;
            var ret = new OrthoResult {
                newPosition = pos,
                newRotation = rotation,
            };
            var a = lastArea.Value;
            ImGuizmo.SetRect(a.Item1, a.Item2, a.Item3, a.Item4);
            ImGuizmo.Enable(true);
            ImGuizmo.SetOrthographic(true);

            var projM = M4x4.Ortho(0.0f, renderBuffer.Size.Item1, 0.0f, renderBuffer.Size.Item2, -1.0f, 1.0f);
            var viewM = M4x4.IDENTITY;
            M4x4 mat = M4x4.TRS((Vector3)pos, Quaternion.IDENTITY, Vector3.ONE);
            if(ImGuizmo.Manipulate(ref viewM.m11, ref projM.m11, OPERATION.TRANSLATE, MODE.WORLD, ref mat.m11)) {
                Vector3 translation = new Vector3(), s = new Vector3();
                M3x3 r = new M3x3();
                ImGuizmo.DecomposeMatrixToComponents(ref mat.m11, ref translation.x, ref r.m11, ref s.x);
                var op = OPERATION.TRANSLATE;
                switch(op) {
                case OPERATION.TRANSLATE:
                    ret.newPosition = translation;
                    ret.posChanged = true;
                break;
                }
            }

            return ret;
        }

        public Ray RaycastBuffer(Vector2 coord) {
            var cam = lastCam as PerspectiveCameraState;
            float w = renderBuffer.Size.Item1;
            float h = renderBuffer.Size.Item2;
            var r = cam.RayFromClip(new Vector2((coord.x/w)*2.0f - 1.0f, ((h-coord.y)/h)*2.0f - 1.0f), w/h);
            return r;
        }

        protected Vector2 bufferToScreen(Vector2 bufP) {
            var rect = lastArea.Value;
            var viewOrigin = new Vector2(rect.Item1, rect.Item2);
            var viewSize = new Vector2(rect.Item3, rect.Item4);
            var bufferSize = new Vector2(renderBuffer.Size.Item1, renderBuffer.Size.Item2);
            var normalizedP = bufP / bufferSize;
            var screenP = viewOrigin + normalizedP*viewSize;
            return screenP;
        }

        protected Vector2 screenToBuffer(Vector2 screenP) {
            var rect = lastArea.Value;
            var viewOrigin = new Vector2(rect.Item1, rect.Item2);
            var mposView = screenP - viewOrigin;
            var viewSize = new Vector2(rect.Item3, rect.Item4);
            var bufferSize = new Vector2(renderBuffer.Size.Item1, renderBuffer.Size.Item2);
            var mposNormalized = mposView / viewSize;
            var newp = mposNormalized * bufferSize;
            return newp;
        }

        public bool DoScreenCircleButton(Vector2 pos, Vector2 anchor, uint color, string label = null) {
            if(lastArea == null)
                return false;
            var screenP = bufferToScreen(pos);
            var inGizmo = ((Vector2)ImGui.GetMousePos() - screenP).Length < 5.0f;
            ImGui.GetForegroundDrawList().AddCircleFilled(screenP, 6.0f, 0xFF000000);
            ImGui.GetForegroundDrawList().AddCircleFilled(screenP, 5.0f, inGizmo ? 0xFF5555FF : color);

            if (label != null && inGizmo)
                ImGui.GetForegroundDrawList().AddText(screenP-new Vector2(0.0f, 20.0f), 0xFF000000, label);
            if (inGizmo && ImGui.IsMouseClicked(0))
                return true;
            else
                return false;
        }

        public Vector2? DoScreenPointGizmo(string uid, Vector2 pos, Vector2 anchor, out bool endupdate, uint color = 0xFF0000FF, bool showlabel = false) {
            endupdate = false;
            if(lastArea == null)
                return null;

            var newp = pos;
            var screenP = bufferToScreen(pos);
            var inGizmo = ((Vector2)ImGui.GetMousePos() - screenP).Length < 5.0f;
            PointGizmoState state = new PointGizmoState { dragging = false };
            if(!pointGizmos.TryGetValue(uid, out state)) {
                if(ImGui.IsMouseClicked(0) && inGizmo) {
                    state.dragging = true;
                    pointGizmos.Add(uid, state);
                }
            } else {
                if(!ImGui.IsMouseDown(0)) {
                    state.dragging = false;
                    pointGizmos.Remove(uid);
                    endupdate = true;
                }
            }

            if(state.dragging) {
                newp = screenToBuffer((Vector2)ImGui.GetMousePos());
            }
            bool active = inGizmo || state.dragging;
            // use new coordinates for drawing (if it got overriden)
            screenP = bufferToScreen(newp);
            ImGui.GetForegroundDrawList().AddCircleFilled(screenP, 6.0f, 0xFF000000);
            ImGui.GetForegroundDrawList().AddCircleFilled(screenP, 5.0f, active ? 0xFF5555FF : color);
            if (active && showlabel)
                ImGui.GetForegroundDrawList().AddText(screenP-new Vector2(0.0f, 20.0f), 0xFF000000, uid);
            if(!state.dragging)
                return null;
            else {
                return newp;
            }
        }

        protected Vector2? worldToBuffer(CameraState cam, Vector3 pos) {
            var s = renderBuffer.Size;
            var w2c = cam.CreateWorldToClipMatrix((float)s.Item1/s.Item2);
            var clipPos = w2c * new Vector4(pos, 1.0f);
            float x = ((clipPos.x/clipPos.w)*0.5f + 0.5f) * s.Item1;
            float y = ((clipPos.y/clipPos.w)*0.5f + 0.5f) * s.Item2;
            y = s.Item2 - y;
            var ret = clipPos.w > 0.0f ? new Vector2(x, y) : (Vector2?)null;
            if (ret == null || (ret.Value.x < 0.0f || ret.Value.x > s.Item1 || ret.Value.y < 0.0f || ret.Value.y > s.Item2))
                return null;
            return ret;
        }

        public Vector3? DoWorldSurfPointGizmo(string uid, Vector3 pos, Plane plane, out bool endupdate) {
            endupdate = false;
            if(lastArea == null)
                return null;
            // TODO: have camera in view
            var cam = lastCam as PerspectiveCameraState;
            if(cam == null)
                return null;
            var screenP = worldToBuffer(cam, pos);
            Vector2? in2d = null;
            if (screenP != null) {
                in2d = DoScreenPointGizmo(uid+"3D", screenP.Value, Vector2.ZERO, out endupdate );
            }
            if(in2d == null)
                return null;
            else
            {
                var r = RaycastBuffer(in2d.Value);
                var newwp = r.Intersect(plane);
                return newwp;
            }
        }

        public bool DoWorldCircleButton(Vector3 pos, string label = null) {
            if(lastArea == null)
                return false;
            // TODO: have camera in view
            var cam = lastCam as PerspectiveCameraState;
            if(cam == null)
                return false;
            var screenP = worldToBuffer(cam, pos);
            if(screenP != null) {
                return DoScreenCircleButton(screenP.Value, Vector2.ZERO, 0xFF00FF00, label);
            } else {
                return false;
            }
        }

        public CapturedFrame CaptureScene()
        {
            var guid = Guid.NewGuid().ToString();
            var tex = new CapturedFrame(renderBuffer.Size.Item1, renderBuffer.Size.Item2, Texture2D.TextureFormat.RGB8);
            renderBuffer.Bind();
            renderBuffer.ReadPixels(ref tex.data[0]);
            return tex;
        }
    }
}
