namespace AnimLib;

internal class NullRenderBuffer : IBackendRenderBuffer
{
    private int _width;
    private int _height;

    public NullRenderBuffer(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public FrameColorSpace ColorSpace => FrameColorSpace.Linear;

    public bool IsHDR => false;

    public bool IsMultisampled => false;

    public (int w, int h) Size => (_width, _height);

    public int FBO => -1;

    public void BindForPostProcess() {}

    public void BindForRender() {}

    public void BlendToScreen(int screenWidth, int screenHeight) {}

    public void Clear() {}

    public void Dispose() {}

    public int GetEntityAtPixel(int x, int y) => -1;

    public void MakePresentable() {}

    public void OnPostRender() {}

    public void OnPreRender() {}

    public void ReadPixels(ref byte data, Texture2D.TextureFormat format = Texture2D.TextureFormat.RGB8) {}

    public void Resize(int width, int height) {
        _width = width;
        _height = height;
    }

    public int Texture() => -1;
}
