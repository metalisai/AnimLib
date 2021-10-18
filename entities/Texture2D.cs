using System;

namespace AnimLib {
    public class Texture2D : RendererResource {
        public enum TextureFormat {
            None,
            RGBA8,
            RGB8,
            ARGB8,
            BGR8,
            R8,
        }
        public byte[] RawData;
        public TextureFormat Format;
        public int Width;
        public int Height;
        public int GLHandle = -1;
        public int Alignment = 4;
        public string ownerGuid = null;

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
    }

}
