using System;

namespace AnimLib;
internal struct CapturedFrame {
    public int width;
    public int height;
    public byte[] data;
    public Texture2D.TextureFormat format;

    public CapturedFrame(int w, int h, Texture2D.TextureFormat f) {
        width = w;
        height = h;
        format = f;
        if(format == Texture2D.TextureFormat.RGB8)
            data = new byte[width * height * 3];
        else
            throw new Exception("Only RGB8 supported here");
    }
}
