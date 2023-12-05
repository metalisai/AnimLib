using System;
using OpenTK;
using OpenTK.Input;
using System.Collections.Generic;

namespace AnimLib;

internal enum PlatformTextureSampler {
    Mipmap,
    Blit,
    Linear,
}

/// <summary>
/// Platform interface for AnimLib that isn't provided by the C# runtime. This is the interface that the platform-specific code must implement.
/// </summary>
internal interface IPlatform {
    /// <summary>
    /// The color space of the presented frame.
    /// If your platform supports gamma correction then you present in linear color space and this value is FrameColorSpace.Linear.
    /// Desktop OpenGL supports hardware gamma correction, but a lot of platforms don't.
    /// </summary>
    FrameColorSpace PresentedColorSpace { get; }

    delegate void OnSizeChangedDelegate(int width, int height);
    event OnSizeChangedDelegate OnSizeChanged;
    delegate void OnDisplayChangedDelegate(int w, int h, double rate);
    event OnDisplayChangedDelegate OnDisplayChanged;
    event EventHandler OnLoaded;

    event EventHandler<MouseButtonEventArgs> mouseDown;
    event EventHandler<MouseButtonEventArgs> mouseUp;
    event EventHandler<MouseMoveEventArgs> mouseMove;
    event EventHandler<MouseWheelEventArgs> mouseScroll;
    event EventHandler<KeyboardKeyEventArgs> PKeyDown;
    event EventHandler<KeyboardKeyEventArgs> PKeyUp;
    event EventHandler<KeyPressEventArgs> PKeyPress;
    
    event EventHandler<OpenTK.Input.FileDropEventArgs> PFileDrop;
    event EventHandler<FrameEventArgs> PRenderFrame;

    void LoadTexture(Texture2D tex2d);
    int AddShader(string v, string f, string? g, string? tcs = null, string? tes = null);
    void DestroyOwner(string owner);

    void RenderGUI(Imgui.DrawList data, IList<SceneView> views, IBackendRenderBuffer rb);
    void ClearBackbuffer(int x, int y, int w, int h); 
    //void RenderImGui(ImDrawDataPtr data, Texture2D atlas);

    int GetSampler(PlatformTextureSampler sampler);


    // TODO: this isn't good (could have multiple windows etc..)
    int WinWidth { get; }
    int WinHeight { get; }

    int BlitProgram { get; }
}
