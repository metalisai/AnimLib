using System;

namespace AnimLib {
    public interface IRenderBuffer : IDisposable{
        void OnPreRender();
        void OnPostRender();
        void Bind();
        void BlendToScreen(int screenWidth, int screenHeight, int program);
        void Resize(int width, int height);
        int GetEntityAtPixel(int x, int y);
        int Texture();
        void Clear();
        (int, int) Size { get; }
        int FBO { get; }
        void ReadPixels(ref byte data);
    }
}
