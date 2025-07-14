using System;
using OpenTK.Windowing.Common;
using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace AnimLib;

internal enum PlatformTextureSampler
{
    Mipmap,
    Blit,
    Linear,
}

internal interface IUserInterfacePlatform
{
    delegate void OnSizeChangedDelegate(int width, int height);
    event OnSizeChangedDelegate OnSizeChanged;
    delegate void OnDisplayChangedDelegate(int w, int h, double rate);
    event OnDisplayChangedDelegate OnDisplayChanged;
    event Action<MouseButtonEventArgs> mouseDown;
    event Action<MouseButtonEventArgs> mouseUp;
    event Action<MouseMoveEventArgs> mouseMove;
    event Action<MouseWheelEventArgs> mouseScroll;
    event Action<KeyboardKeyEventArgs> PKeyDown;
    event Action<KeyboardKeyEventArgs> PKeyUp;
    event Action<TextInputEventArgs> PTextInput;
    event EventHandler OnLoaded;

    KeyboardState KeyboardState { get; }

    event Action<FileDropEventArgs> PFileDrop;
    void RenderGUI(Imgui.DrawList data, IList<SceneView> views, IBackendRenderBuffer rb);
    // TODO: this isn't good (could have multiple windows etc..)
    int WinWidth { get; }
    int WinHeight { get; }
}

/// <summary>
/// Platform interface for AnimLib that isn't provided by the C# runtime. This is the interface that the platform-specific code must implement.
/// </summary>
internal interface IRendererPlatform
{
    /// <summary>
    /// The color space of the presented frame.
    /// If your platform supports gamma correction then you present in linear color space and this value is FrameColorSpace.Linear.
    /// Desktop OpenGL supports hardware gamma correction, but a lot of platforms don't.
    /// </summary>
    FrameColorSpace PresentedColorSpace { get; }

    event EventHandler OnLoaded;

    event Action<FrameEventArgs> PRenderFrame;

    void LoadTexture(Texture2D tex2d);
    int AddShader(string v, string f, string? g, string? tcs = null, string? tes = null);
    void DestroyOwner(string owner);

    void ClearBackbuffer(int x, int y, int w, int h);
    //void RenderImGui(ImDrawDataPtr data, Texture2D atlas);

    int GetSampler(PlatformTextureSampler sampler);

    int BlitProgram { get; }

    // Force rendering a frame, for headless platforms
    void RenderFrame(FrameEventArgs e);
    IEnumerable<int> Programs { get; }
    void DeleteShader(int shader);
    internal SkiaRenderer Skia { get; }
    int BlitVao { get; }
}

internal interface IInteractivePlatform : IRendererPlatform, IUserInterfacePlatform
{

}