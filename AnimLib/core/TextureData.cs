using System;

namespace AnimLib;

/// <summary>
/// Color space of the captured frame.
/// </summary>
public enum FrameColorSpace {
    /// <summary> Linear color space. If gamma correction is enabled the frames are in this format and need to be converted back into SRGB space.</summary>
    Linear,
    /// <summary> SRGB color space. The color space most monitors use. Can usually be passed directly to other applications.</summary>
    sRGB
}

internal struct CapturedFrame {
    public int width;
    public int height;
    public byte[] data;
    public Texture2D.TextureFormat format;
    public required FrameColorSpace colorSpace;

    public CapturedFrame(int w, int h, Texture2D.TextureFormat f) {
        width = w;
        height = h;
        format = f;
        if (format == Texture2D.TextureFormat.RGB8)
            data = new byte[width * height * 3];
        else if (format == Texture2D.TextureFormat.RGB16)
            data = new byte[width * height * 6];
        else
            throw new Exception("Only RGB8 or RGB16 supported here");
    }
}
