using System;

namespace AnimLib;

/// <summary>
/// A bitmap texture.
/// </summary>
public class Texture2D : IRendererResource {
    /// <summary>
    /// The memory format of the texture.
    /// </summary>
    public enum TextureFormat {
        /// <summary>
        /// No format.
        /// </summary>
        None,
        /// <summary>
        /// 4 bytes per pixel, alpha channel invalid.
        /// </summary>
        RGBx8, // same as RGBA8 but alpha channel invalid
        /// <summary>
        /// 4 bytes per pixel, alpha channel valid.
        /// </summary>
        RGBA8,
        /// <summary>
        /// 3 bytes per pixel.
        /// </summary>
        RGB8,
        /// <summary>
        /// 4 bytes per pixel, ARGB.
        /// </summary>
        ARGB8,
        /// <summary>
        /// 3 bytes per pixel, BGR.
        /// </summary>
        BGR8,
        /// <summary>
        /// 4 bytes per pixel, BGRA.
        /// </summary>
        BGRA8,
        /// <summary>
        /// 1 byte per pixel.
        /// </summary>
        R8,
        /// <summary>
        /// float16/half, 2 bytes per channel, 8 bytes per pixel.
        /// </summary>
        RGBA16F,
        /// <summary>
        /// 16 bit normalized integer, 2 bytes per channel, 6 bytes per pixel.
        /// </summary>
        RGB16,
    }

    /// <summary>
    /// The raw texture data.
    /// </summary>
    public byte[] RawData;

    /// <summary>
    /// The memory format of the texture.
    /// </summary>
    public TextureFormat Format;

    /// <summary>
    /// The width of the texture in pixels.
    /// </summary>
    public int Width;

    /// <summary>
    /// The height of the texture in pixels.
    /// </summary>
    public int Height;

    /// <summary>
    /// Renderer handle for the texture.
    /// </summary>
    public int GLHandle = -1;

    /// <summary>
    /// Per row alignment in bytes.
    /// </summary>
    public int Alignment = 4;

    /// <summary>
    /// The GUID of the owner of this texture.
    /// </summary>
    public string ownerGuid;

    /// <summary>
    /// Whether to generate mipmaps for this texture.
    /// </summary>
    public bool GenerateMipmap = true;

    internal Texture2D(string guid) {
        this.ownerGuid = guid;
        RawData = Array.Empty<byte>();
    }

    string IRendererResource.GetOwnerGuid() {
        return ownerGuid;
    }

    /// <summary>
    /// Get the pixel at the given coordinates.
    /// </summary>
    public Color GetPixel(int x, int y) {
        byte blue, green, red, alpha;
        int offset;
        switch(this.Format) {
            case TextureFormat.BGR8:
            int alignmentBytes = (Alignment - (Width*3)%Alignment)%Alignment;
            offset = y*(Width*3+alignmentBytes) + x*3;
            blue = RawData[offset];
            green = RawData[offset+1];
            red = RawData[offset+2];
            alpha = 0xFF;
            break;
            case TextureFormat.ARGB8:
            // TODO: this is defnitely wrong!
            offset = y*Width*4 + (Alignment - ((y*Width*4)%Alignment)) + x*4;
            red = RawData[offset+1];
            green = RawData[offset+2];
            blue = RawData[offset+3];
            alpha = RawData[offset];
            break;
            default:
            throw new NotImplementedException();
        }
        return new Color(red, green, blue, alpha);
    }

    /// <summary>
    /// Convert the texture to the given format.
    /// </summary>
    public void ConvertColor(TextureFormat newFormat) {
        // someone has already loaded the texture, can't pull the rug under their feet
        System.Diagnostics.Debug.Assert(GLHandle < 0);
        // need this for Skia (they only support 8, 16, 32, 64bit formats)
        if(Format == TextureFormat.BGR8 && newFormat == TextureFormat.RGBx8) {
            var newData = new byte[Width*Height*4];
            int newIdx = 0;
            int oldIdx = 0;
            for(int row = 0; row < Height; row++) {
                for(int col = 0; col < Width; col++) {
                    newData[newIdx] = RawData[oldIdx+2];
                    newData[newIdx+1] = RawData[oldIdx+1];
                    newData[newIdx+2] = RawData[oldIdx+0];
                    newData[newIdx+3] = 255;
                    newIdx += 4;
                    oldIdx += 3;
                }
                // skip alignment bytes
                oldIdx += (4 - (oldIdx%Alignment)) % 4;
            }
            RawData = newData;
            Format = TextureFormat.RGBA8;
        }
        if(Format == TextureFormat.BGRA8 && newFormat == TextureFormat.RGBA8) {
            int newIdx = 0;
            for(int row = 0; row < Height; row++) {
                for(int col = 0; col < Width; col++) {
                    byte c0 = RawData[newIdx];
                    byte c1 = RawData[newIdx+1];
                    byte c2 = RawData[newIdx+2];
                    byte c3 = RawData[newIdx+3];
                    RawData[newIdx] = c2;
                    RawData[newIdx+1] = c1;
                    RawData[newIdx+2] = c0;
                    RawData[newIdx+3] = c3;
                    newIdx += 4;
                }
            }
            Format = TextureFormat.RGBA8;

        }
        else throw new NotImplementedException();
    }
}
