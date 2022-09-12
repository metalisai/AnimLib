using System;
using ImGuiNET;
using ImGuizmoNET;
using OpenTK.Input;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace AnimLib {
    public class ImguiContext {

        string guid = Guid.NewGuid().ToString();

        int _width = 1920;
        int _height = 1080;
        public int Width { 
            get {
                return _width;
            }
        }
        public int Height { 
            get {
                return _height;
            }
        }

        IntPtr nativeCtx;
        Dictionary<int, Texture2D> textures = new Dictionary<int, Texture2D>();
        public ImguiContext(int width, int height) {
            nativeCtx = ImGui.CreateContext();
            ImGuizmo.SetImGuiContext(nativeCtx);
            ImGui.SetCurrentContext(nativeCtx);
            //ImGui.NewFrame();
            ImGuiIOPtr imGuiIO = ImGui.GetIO();

            ImGui.StyleColorsDark();

            imGuiIO.Fonts.AddFontDefault();
            imGuiIO.Fonts.SetTexID(new IntPtr(1));

            imGuiIO.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            //imGuiIO.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

            var style = ImGui.GetStyle();
            if((imGuiIO.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                style.WindowRounding = 0.0f;
                style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
            }

            imGuiIO.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            imGuiIO.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            imGuiIO.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            imGuiIO.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            imGuiIO.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            imGuiIO.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            imGuiIO.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            imGuiIO.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            imGuiIO.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            imGuiIO.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            imGuiIO.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.Back;
            imGuiIO.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            imGuiIO.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            imGuiIO.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            imGuiIO.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            imGuiIO.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            imGuiIO.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            imGuiIO.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            imGuiIO.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;

            int tWidth, tHeight, tBytesPerPixel;
            IntPtr tPixels;
            imGuiIO.Fonts.GetTexDataAsRGBA32(out tPixels, out tWidth, out tHeight, out tBytesPerPixel);
            var dstPixels = new byte[tWidth*tHeight*tBytesPerPixel];
            Marshal.Copy(tPixels, dstPixels, 0, tWidth*tHeight*tBytesPerPixel);

            var tex = new Texture2D(guid) {
                Format = Texture2D.TextureFormat.RGBA8,
                Width = tWidth,
                Height = tHeight,
                RawData = dstPixels,
                GenerateMipmap = true,
            };

            textures.Add(1, tex);
            
            imGuiIO.Fonts.Build();
            //Update(width, height, 1.0f / 60.0f);
        }

        public void Update(int width, int height, float dt, Vector2 mousePos, bool left, bool right, bool middle, float scrollDelta) {
            _width = width;
            _height = height;

            var io = ImGui.GetIO();

            io.DisplaySize = new System.Numerics.Vector2(width, height);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1.0f, 1.0f);
            io.DeltaTime = dt;

            io.MouseDown[0] = left;
            io.MouseDown[1] = right;
            io.MouseDown[2] = middle;
            io.MousePos = new System.Numerics.Vector2(mousePos.x, mousePos.y);
            io.MouseWheel = scrollDelta;

            System.Diagnostics.Debug.Assert(io.Fonts.IsBuilt(), "Font atlas not built! It is generally built by the renderer back-end. Missing call to renderer _NewFrame() function? e.g. ImGui_ImplOpenGL3_NewFrame().");
            ImGui.NewFrame();
            ImGuizmo.BeginFrame();
        }

        public (ImDrawDataPtr, Texture2D) Render() {
            //ImGui.ShowDemoWindow();
            ImGui.Render();
            return (ImGui.GetDrawData(), textures[1]);
        }

    }
}
