using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace AnimLib {
    public class UserInterface {

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

        public static float Width = 1920;
        public static float Height = 1080;

        static ImguiContext imgui = null;

        public static void BeginFrame(in MouseState ms, float width, float height) {
            if(frameStarted) {
                throw new Exception("What are you doing!!!?");
            }

            Width = width;
            Height = height;

            frameStarted = true;

            scrollDelta = ms.scroll-scrollLast;
            scrollLast = ms.scroll;
            mouseDelta = ms.position - mousePosition;
            mousePosition = ms.position;

            if(imgui == null) {
                imgui = new ImguiContext((int)Width, (int)Height);
            }
            imgui.Update((int)width, (int)height, 1.0f/60.0f, mousePosition, ms.left, ms.right, false, scrollDelta);
        }

        public static (ImDrawDataPtr, Texture2D) EndFrame() {
            if(!frameStarted) {
                throw new Exception("EndFrame() before BeginFrame() is not valid!");
            }
            frameStarted = false;

            var cmds = imgui.Render();

            return cmds;
        }

        public static void End() {
        }
    }

}
