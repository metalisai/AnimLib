using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace AnimLib {
    public class UserInterface {

        public enum UIEntityId {
            None = -1,
            XAxis = -2,
            YAxis = -3,
            ZAxis = -4,
        }

        public struct ItemState {
            public bool dragging;
            public Vector2 grabPoint;
        }

        public struct WindowState {
            public bool open;
        }

        public struct UIContext {
            public Rect rect;
            public Vector2 anchor;
            public Vector3 origin;
            public float currentDepth;
        }

        public struct MouseState {
            public Vector2 position;
            public bool left;
            public bool right;
            public float scroll;
        }

        static bool frameStarted = false;
        public static Vector2 mousePosition;
        public static Vector2 mouseDelta;
        public static float scrollLast;
        public static float scrollDelta;

        public static bool left;
        public static bool right;
        public static bool leftDown;
        public static bool rightDown;
        public static bool leftUp;
        public static bool rightUp;

        static WorldSnapshot uiFrame;

        public static Vector2 Size = Vector2.ZERO;
        public static int MouseEntityId = -1;
        public static bool UseDebugCamera = false;
        private static CameraState _worldCamera;
        public static CameraState WorldCamera {
            get {
                return UseDebugCamera ? DebugCamera : _worldCamera;
            } set {
                _worldCamera = value;
            }
        }
        public static CameraState DebugCamera;

        public static LinkedList<UIContext> contexts = new LinkedList<UIContext>();

        public static float Width = 1920;
        public static float Height = 1080;

        static ImguiContext imgui = null;

        public static void BeginFrame(in MouseState ms, float width, float height) {
            if(frameStarted) {
                throw new Exception("What are you doing!!!?");
            }

            Width = width;
            Height = height;

            // TODO: reuse?
            uiFrame = new WorldSnapshot();

            frameStarted = true;

            leftDown = false;
            leftUp = false;
            rightDown = false;
            rightUp = false;

            scrollDelta = ms.scroll-scrollLast;
            scrollLast = ms.scroll;
            mouseDelta = ms.position - mousePosition;
            mousePosition = ms.position;
            if(!left && ms.left) {
                leftDown = true;
            } else if(left && !ms.left) {
                leftUp = true;
            }
            if(!right && ms.right) {
                rightDown = true;
            } else if(right && !ms.right) {
                rightUp = true;
            }
            left = ms.left;

            if(imgui == null) {
                imgui = new ImguiContext((int)Width, (int)Height);
            }
            imgui.Update((int)width, (int)height, 1.0f/60.0f, mousePosition, left, right, false, scrollDelta);
        }

        public static (WorldSnapshot, (ImDrawDataPtr, Texture2D)) EndFrame() {
            if(!frameStarted) {
                throw new Exception("EndFrame() before BeginFrame() is not valid!");
            }
            uiFrame.Camera = WorldCamera;
            frameStarted = false;
            var uf = uiFrame;
            uiFrame = null;

            var cmds = imgui.Render();

            return (uf, cmds);
        }

        private static bool mouseInCircle(Vector2 anchor, Vector2 pos, float r) {
            Vector2 origin = getOrigin(anchor);
            Vector2 oanchor = getAnchor(anchor);
            float x = oanchor.x * Size.x + origin.x;
            float y = oanchor.y * Size.y + origin.y;
            return ((new Vector2(x,y)+pos) - mousePosition).Length <= r;
        }

        private static bool mouseInRectangle(Vector2 anchor, Rect rect) {
            Vector2 origin = getOrigin(anchor);
            Vector2 oanchor = getAnchor(anchor);
            float x = oanchor.x * Size.x + origin.x;
            float y = oanchor.y * Size.y + origin.y;
            return mousePosition.x >= rect.x+x && mousePosition.x <= rect.x+rect.width+x
                    && mousePosition.y >= rect.y+y && mousePosition.y <= rect.y+rect.height+y;
        }

        public static Vector2 mousePosAnchor(Vector2 anchor) {
            Vector2 origin = getOrigin(anchor);
            var oanchor = getAnchor(anchor);
            return mousePosition - origin - new Vector2(oanchor.x*Size.x, oanchor.y*Size.y);
        }

        private static Vector3 getOrigin(Vector2 anchor) {
            UIContext prevCtx;
            if(contexts.Last != null) {
                prevCtx = contexts.Last.Value;
                return prevCtx.origin + new Vector3(prevCtx.rect.width*anchor.x, prevCtx.rect.height*anchor.y, -0.01f);
            }
            return new Vector3(0.0f, 0.0f, -0.01f);
        }

        public static Vector2 getAnchor(Vector2 anchor) {
            if(contexts.Count > 0) {
                return contexts.First.Value.anchor;
            }
            return anchor;
        }

        public static void End() {
            contexts.RemoveLast();
        }
    }

}
