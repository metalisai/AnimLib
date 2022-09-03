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

        public static Vector2 mousePosition;
        public static int MouseEntityId = -1;
    }
}
