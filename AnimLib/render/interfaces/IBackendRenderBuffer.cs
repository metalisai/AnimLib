using System;

namespace AnimLib;

/// <summary>
/// A render buffer is a framebuffer object that can be used to render a scene to a texture.
/// </summary>
internal interface IBackendRenderBuffer : IDisposable{
    FrameColorSpace ColorSpace { get; }
    void OnPreRender();
    void OnPostRender();
    void Bind();
    void BlendToScreen(int screenWidth, int screenHeight);
    void Resize(int width, int height);
    int GetEntityAtPixel(int x, int y);
    int Texture();
    void Clear();
    bool IsHDR { get; }
    (int w, int h) Size { get; }
    int FBO { get; }
    void ReadPixels(ref byte data, Texture2D.TextureFormat format = Texture2D.TextureFormat.RGB8);
}
