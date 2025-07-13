using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.Linq;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace AnimLib;

internal class UserInterface
{
    // TODO: no global state
    public static int MouseEntityId = -1;

    IUserInterfacePlatform uiPlatform;
    bool mouseLeft;
    bool mouseRight;
    Vector2 mousePos;
    float scrollValue;
    float scrollDelta;

    IBackendRenderBuffer uiRenderBuffer;
    RenderState rstate;
    public Imgui imgui;

    public bool overrideCamera = false;
    public PerspectiveCameraState? debugCamera;
    Vector2 debugCamRot;
    bool wasOverridden = false;


    public UserInterface(IUserInterfacePlatform platform, RenderState rstate)
    {
        this.uiPlatform = platform;
        this.rstate = rstate;

        platform.OnSizeChanged += UpdateSize;
        platform.mouseDown += mouseDown;
        platform.mouseUp += mouseUp;
        platform.mouseMove += mouseMove;
        platform.mouseScroll += mouseScroll;
        platform.OnLoaded += Load;

        uiRenderBuffer = new DepthPeelRenderBuffer(rstate.platform, rstate.platform.PresentedColorSpace, false);
        imgui = new Imgui((int)uiRenderBuffer.Size.Item1, (int)uiRenderBuffer.Size.Item2, rstate.platform);
    }

    // resize buffers, UI etc
    public void UpdateSize(int width, int height)
    {
        if (uiRenderBuffer.Size.Item1 != width || uiRenderBuffer.Size.Item2 != height)
        {
            Debug.TLog($"Resize window to {width}x{height}");
            uiRenderBuffer.Resize(width, height);
        }
    }

    private void mouseDown(MouseButtonEventArgs args)
    {
        if (args.Button == MouseButton.Left)
        {
            mouseLeft = true;
        }
        if (args.Button == MouseButton.Right)
        {
            mouseRight = true;
        }
    }

    private void mouseUp(MouseButtonEventArgs args)
    {
        if (args.Button == MouseButton.Left)
        {
            mouseLeft = false;
        }
        if (args.Button == MouseButton.Right)
        {
            mouseRight = false;
        }
    }

    private void mouseMove(MouseMoveEventArgs args)
    {
        mousePos = new Vector2(args.Position.X, args.Position.Y);
    }

    private void mouseScroll(MouseWheelEventArgs args)
    {
        // TODO: wrong
        scrollValue = args.OffsetY;
        scrollDelta = args.OffsetY;
    }

    public int WindowWidth
    {
        get
        {
            return uiRenderBuffer.Size.Item1;
        }
    }

    public int WindowHeight
    {
        get
        {
            return uiRenderBuffer.Size.Item2;
        }
    }

    public void Load(object? sender, EventArgs args)
    {
        uiRenderBuffer.Resize(1024, 1024);

        uiPlatform.PKeyDown += (KeyboardKeyEventArgs args) =>
        {
            if (args.Key == Keys.F && !args.IsRepeat)
            {
                overrideCamera = !overrideCamera;
                if (overrideCamera)
                {
                    debugCamera = new PerspectiveCameraState();
                    debugCamera.position.z = -13.0f;
                }
                else
                {
                    if (debugCamera != null)
                    {
                        debugCamera.position = Vector3.ZERO;
                    }
                    this.debugCamRot = Vector2.ZERO;
                }
            }
            Imgui.KeyEdge((uint)args.Key, true);
        };
        uiPlatform.PKeyUp += (KeyboardKeyEventArgs args) =>
        {
            Imgui.KeyEdge((uint)args.Key, false);
        };
        uiPlatform.PTextInput += (TextInputEventArgs args) =>
        {
            Imgui.AddInputCharacter((uint)args.Unicode);
        };

        uiPlatform.mouseMove += (MouseMoveEventArgs args) =>
        {
            var state = uiPlatform.KeyboardState;
            if (overrideCamera && !state.IsKeyDown(Keys.LeftControl))
            {
                this.debugCamRot.x += args.DeltaX * 0.01f;
                this.debugCamRot.y += args.DeltaY * 0.01f;
                this.debugCamRot.x %= 2 * MathF.PI;
                this.debugCamRot.y %= 2 * MathF.PI;
                var qx = Quaternion.AngleAxis(debugCamRot.x, Vector3.UP);
                var qy = Quaternion.AngleAxis(debugCamRot.y, Vector3.RIGHT);
                if (debugCamera != null)
                {
                    debugCamera.rotation = qy * qx;
                }
            }
        };
    }

    public void OnPreRender()
    {
        rstate.OverrideCamera = overrideCamera ? debugCamera : null;
        // TODO: use actual frame rate
        imgui.Update(uiRenderBuffer.Size.Item1, uiRenderBuffer.Size.Item2, 1.0f / 60.0f, mousePos, mouseLeft, mouseRight, false, scrollDelta);
        scrollDelta = 0.0f;
        uiRenderBuffer.Clear();
        rstate.platform.ClearBackbuffer(0, 0, uiRenderBuffer.Size.Item1, uiRenderBuffer.Size.Item2);
        rstate.SceneDirty = overrideCamera || wasOverridden;
    }

    public void OnPostRender(IEnumerable<SceneView> ieviews)
    {
        var views = ieviews.ToArray();
        // Render UI
        {
            using var _ = new Performance.Call("Render UI");
            var drawList = imgui.Render();
            uiPlatform.RenderGUI(drawList, views, uiRenderBuffer);
        }

        /*int sceneEntity = -2;
        if (rstate.Views.Count() > 0)
        {
            sceneEntity = views[0].GetEntityIdAtPixel(Imgui.GetMousePos());
        }
        var guiEntity = GetGuiEntityAtPixel(uiRenderBuffer, Imgui.GetMousePos());
        if (sceneEntity == -2)
        { // out of scene viewport
            UserInterface.MouseEntityId = guiEntity;
        }
        else
        {
            if (guiEntity == -1)
                UserInterface.MouseEntityId = sceneEntity;
            else
                UserInterface.MouseEntityId = guiEntity;
        }*/
        wasOverridden = overrideCamera;
    }

    public void OnUpdate(FrameEventArgs args)
    {
        float dt = (float)args.Time;
        var kstate = uiPlatform.KeyboardState;
        if (overrideCamera && debugCamera != null)
        {
            float s = 5.0f;
            if (kstate.IsKeyDown(Keys.LeftShift))
            {
                s = 0.01f;
            }
            if (kstate.IsKeyDown(Keys.W))
            {
                debugCamera.position += debugCamera.rotation * Vector3.FORWARD * dt * s;
            }
            if (kstate.IsKeyDown(Keys.S))
            {
                debugCamera.position -= debugCamera.rotation * Vector3.FORWARD * dt * s;
            }
            if (kstate.IsKeyDown(Keys.A))
            {
                debugCamera.position -= debugCamera.rotation * Vector3.RIGHT * dt * s;
            }
            if (kstate.IsKeyDown(Keys.D))
            {
                debugCamera.position += debugCamera.rotation * Vector3.RIGHT * dt * s;
            }
        }
    }
    
    public int GetGuiEntityAtPixel(IBackendRenderBuffer pb, Vector2 pixel) {
        if(pixel.x >= 0.0f && pixel.x < pb.Size.Item1 && pixel.y >= 0.0f && pixel.y < pb.Size.Item2) {
            return pb.GetEntityAtPixel((int)pixel.x, pb.Size.Item2-(int)pixel.y-1);
        } else {
            return -2;
        }
    }
}
