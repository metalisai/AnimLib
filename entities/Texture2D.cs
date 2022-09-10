using System;

namespace AnimLib {
    public class Texture2D : RendererResource {
        public enum TextureFormat {
            None,
            RGBx8, // same as RGBA8 but alpha channel invalid
            RGBA8,
            RGB8,
            ARGB8,
            BGR8,
            BGRA8,
            R8,
        }
        public byte[] RawData;
        public TextureFormat Format;
        public int Width;
        public int Height;
        public int GLHandle = -1;
        public int Alignment = 4;
        public string ownerGuid = null;
        public bool GenerateMipmap = true;

        public Texture2D(string guid) {
            this.ownerGuid = guid;
        }

        public string GetOwnerGuid() {
            return ownerGuid;
        }

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

}
