using System;
using OpenTK;
using OpenTK.Input;
using System.Collections.Generic;

namespace AnimLib;

public enum PlatformTextureSampler {
    Mipmap,
    Blit,
    Linear,
}

/// <summary>
/// Platform interface for AnimLib that isn't provided by the C# runtime. This is the interface that the platform-specific code must implement.
/// </summary>
internal interface IPlatform {
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
    int AddShader(string v, string f, string g, string tcs = null, string tes = null);
    void DestroyOwner(string owner);

    void RenderGUI(ImguiContext.DrawList data, IList<SceneView> views, IRenderBuffer rb);
    void ClearBackbuffer(int x, int y, int w, int h); 
    //void RenderImGui(ImDrawDataPtr data, Texture2D atlas);

    int GetSampler(PlatformTextureSampler sampler);


    // TODO: this isn't good (could have multiple windows etc..)
    int WinWidth { get; }
    int WinHeight { get; }

    int BlitProgram { get; }
}
