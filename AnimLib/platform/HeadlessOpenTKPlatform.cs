using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AnimLib;
using OpenTK.Windowing.Common;

internal class HeadlessOpenTKPlatform : IRendererPlatform
{
    [DllImport("libEGL.so")]
    private static extern IntPtr eglGetDisplay(IntPtr native_display);

    public HeadlessOpenTKPlatform()
    {
        var dsp = eglGetDisplay(IntPtr.Zero);
        Debug.Log($"Display: {dsp}");
    }

    public FrameColorSpace PresentedColorSpace => throw new NotImplementedException();

    public int BlitProgram => throw new NotImplementedException();

    public event EventHandler OnLoaded;
    public event Action<FrameEventArgs> PRenderFrame;

    public int AddShader(string v, string f, string? g, string? tcs = null, string? tes = null)
    {
        throw new NotImplementedException();
    }

    public void ClearBackbuffer(int x, int y, int w, int h)
    {
        throw new NotImplementedException();
    }

    public void DestroyOwner(string owner)
    {
        throw new NotImplementedException();
    }

    public int GetSampler(PlatformTextureSampler sampler)
    {
        throw new NotImplementedException();
    }

    public void LoadTexture(Texture2D tex2d)
    {
        throw new NotImplementedException();
    }
}