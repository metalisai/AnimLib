using System;

namespace AnimLib;

/// <summary>
/// A render buffer is a framebuffer object that can be used to render a scene to a texture.
/// </summary>
public interface IRenderBuffer : IDisposable{
    void OnPreRender();
    void OnPostRender();
    void Bind();
    void BlendToScreen(int screenWidth, int screenHeight);
    void Resize(int width, int height);
    int GetEntityAtPixel(int x, int y);
    int Texture();
    void Clear();
    (int, int) Size { get; }
    int FBO { get; }
    void ReadPixels(ref byte data);
}