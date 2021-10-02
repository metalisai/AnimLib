/*using SharpFont;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib {

    public enum TextHorizontalAlignment {
        None,
        Left,
        Center,
        Right,
    }

    public enum TextVerticalAlignment {
        None,
        Up,
        Center,
        Down,
    }

    public class FontCache {

        public class CachedGlyph {
            // NOTE: maybe it would be better to call thse U and V?
            public float X;
            public float Y;
            public float MaxX;
            public float MaxY;
            public float w;
            public float h;
            public float BearingX;
            public float BearingY;
            public float HAdvance;
        }

        public struct GlyphKey {
            public char c;
            public float size;

            public override bool Equals(object obj) {
                if(obj is GlyphKey) {
                    var other = (GlyphKey)obj;
                    return other.c == this.c && other.size == this.size;
                } else {
                    return false;
                }
            }

            public override int GetHashCode() {
                int hash = 17;
                hash = hash * 31 + c;
                hash = hash * 31 + (int)(size*10f);
                return hash;
            }
        }

        Dictionary<GlyphKey, CachedGlyph> _cachedGlyphs = new Dictionary<GlyphKey, CachedGlyph>();

        int vertexIndex = 0;
        float[] vertexBuffer = new float[2400];

        byte[,] cache = new byte[1024,1024];
        int tex;
        int vao, vbo;

        int curMaxHeight; // max height in current row
        int curX; // current offset in font atlas
        int curY;

        FontFace font;
        FontFace symbola;
        bool cacheDirty = false;

        public void PushGlyph(char c, float size, Glyph glyph, Surface surface) {
            var key = new GlyphKey() {
                c = c,
                size = size
            };
            int gend = curX + surface.Width;
            if(gend >= 1024) {
                curY += curMaxHeight;
                curMaxHeight = surface.Height;
                curX = 0;
            }
            if(surface.Height > curMaxHeight) {
                curMaxHeight = surface.Height;
            }
            int startX = curX;
            int startY = curY;
            int endX = curX + surface.Width;
            int endY = curY + surface.Height;
            curX += surface.Width;
            if(curY + curMaxHeight >= 1024) {
                throw new Exception();
            }
            if(endX >= 1024) {
                throw new Exception();
            }
            var cg = new CachedGlyph() {
                X = ((float)startX / 1024f),
                Y = 1f - ((float)endY / 1024f),
                MaxX = ((float)endX / 1024f),
                MaxY = 1f - ((float)startY / 1024f),
                w = surface.Width,
                h = surface.Height,
                BearingX = glyph.HorizontalMetrics.Bearing.X,
                BearingY = glyph.HorizontalMetrics.Bearing.Y,
                HAdvance = glyph.HorizontalMetrics.Advance
            };
            _cachedGlyphs.Add(key, cg);
            Span<byte> nativeSpan;
            unsafe { 
                nativeSpan = new Span<byte>(surface.Bits.ToPointer(), surface.Width*surface.Height);
            }
            // TODO: copy to image!
            for(int i = 0; i < surface.Height; i++) {
                for(int j = 0; j < surface.Width; j++) {
                    cache[startY+i, startX+j] = nativeSpan[i*surface.Width + j];
                }
            }
            cacheDirty = true;
        }

        public FontCache() {
            font = new FontFace(File.OpenRead("/usr/share/fonts/truetype/ubuntu/Ubuntu-M.ttf"));
            symbola = new FontFace(File.OpenRead("/usr/share/fonts/truetype/ancient-scripts/Symbola_hint.ttf"));
            //for(int i = 33; i < 127; i++) {
                //var surface = RenderSurface((char)i, font);
                //textures[i] = surface;
            //}
            SetupVAO();
            tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1024, 1024, 0, PixelFormat.Red, PixelType.UnsignedByte, ref cache[0,0]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.BindTexture(TextureTarget.Texture2D, 0);

        }

        void UpdateCache() {
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1024, 1024, 0, PixelFormat.Red, PixelType.UnsignedByte, ref cache[0,0]);
            cacheDirty = false;
        }

        public void SetupVAO() {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            var vData = new float[] {
               -200f, -200f, 0.0f, 0.0f,
                200f, -200f, 1.0f, 0.0f,
               -200f,  200f, 0.0f, 1.0f,
                200f, -200f, 1.0f, 0.0f,
                200f,  200f, 1.0f, 1.0f,
               -200f,  200f, 0.0f, 1.0f,
            };
            vData.CopyTo(vertexBuffer, 0);
            vertexIndex = vData.Length;
            GL.BufferData(BufferTarget.ArrayBuffer, vertexBuffer.Length * sizeof(float), vertexBuffer, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 16, 0);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 16, 8);
            GL.BindVertexArray(0);


            //PushString("Testing!", new Vector2(-200.0f, 200.0f));
        }

        public int RenderSurface(char c, FontFace font, float size) {
            var glyph = font.GetGlyph(c, size);
            var surface = new Surface() {
                Bits = Marshal.AllocHGlobal(glyph.RenderWidth * glyph.RenderHeight),
                Width = glyph.RenderWidth,
                Height = glyph.RenderHeight,
                Pitch = glyph.RenderWidth,
            };
            Span<byte> nativeSpan;

            unsafe { 
                nativeSpan = new Span<byte>(surface.Bits.ToPointer(), surface.Width*surface.Height);
            }
            for(int i = 0; i < surface.Width * surface.Height; i++) {
                nativeSpan[i] = 0;
            }
            glyph.RenderTo(surface);

            PushGlyph(c, size, glyph, surface);
            
            Marshal.FreeHGlobal(surface.Bits);
            surface.Bits = new IntPtr(0);
            return tex;
        }

        public void PushString(string s, Vector2 pos, float size, TextHorizontalAlignment horizontalAlignment = TextHorizontalAlignment.Left, TextVerticalAlignment verticalAlignment = TextVerticalAlignment.Up) {
            float x = pos.x;
            var vbuf = new List<float>();
            Vector2 offset;
            for(int i = 0; i < s.Length; i++) {
                var c = s[i];
                var key = new GlyphKey {
                    c = c,
                    size = size    
                };
                CachedGlyph g;
                if(!_cachedGlyphs.TryGetValue(key, out g)) {
                    var surface = RenderSurface((char)c, font, size);
                    g = _cachedGlyphs[key];
                }
                float kernX = 0.0f;
                if(i > 0) {
                    kernX = font.GetKerning(s[i-1], c, size);
                }
                float baseY = pos.y + size;

                float yd = g.h - g.BearingY;

                float lowX = x + g.BearingX + kernX;
                float highX = lowX+g.w;
                float lowY = baseY+yd;
                float highY = baseY-g.h+yd;

                // TODO: use lookup table!

                // lower left
                // pos
                vbuf.Add(lowX);
                vbuf.Add(lowY);
                // tex coord
                vbuf.Add(g.X);
                vbuf.Add(g.Y);

                // lower right
                vbuf.Add(highX);
                vbuf.Add(lowY);
                vbuf.Add(g.MaxX);
                vbuf.Add(g.Y);

                // upper left
                vbuf.Add(lowX);
                vbuf.Add(highY);
                vbuf.Add(g.X);
                vbuf.Add(g.MaxY);

                // lower right
                vbuf.Add(highX);
                vbuf.Add(lowY);
                vbuf.Add(g.MaxX);
                vbuf.Add(g.Y);

                // upper right
                vbuf.Add(highX);
                vbuf.Add(highY);
                vbuf.Add(g.MaxX);
                vbuf.Add(g.MaxY);

                // upper left
                vbuf.Add(lowX);
                vbuf.Add(highY);
                vbuf.Add(g.X);
                vbuf.Add(g.MaxY);
                x = x + g.HAdvance + kernX;
            }
            switch(horizontalAlignment) {
                default:
                case TextHorizontalAlignment.Left:
                    offset.x = 0.0f;
                    break;
                case TextHorizontalAlignment.Right:
                    offset.x = -(x-pos.x);
                    break;
                case TextHorizontalAlignment.Center:
                    offset.x = -(x-pos.x)/2.0f;
                    break;
            }
            switch(verticalAlignment) {
                default:
                case TextVerticalAlignment.Up:
                    offset.y = 0.0f;
                    break;
                case TextVerticalAlignment.Center:
                    offset.y = -size/2.0f;
                    break;
                case TextVerticalAlignment.Down:
                    offset.y = -size;
                    break;
            }
            for(int i = 0 ; i < vbuf.Count; i+=4, vertexIndex+=4) {
                vertexBuffer[vertexIndex+0] = vbuf[i+0]+offset.x;
                vertexBuffer[vertexIndex+1] = vbuf[i+1]+offset.y;
                vertexBuffer[vertexIndex+2] = vbuf[i+2];
                vertexBuffer[vertexIndex+3] = vbuf[i+3];
            }
        }

        public void RenderTest(int program, M4x4 mat) {

            if(cacheDirty) {
                UpdateCache();
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexBuffer.Length * sizeof(float), vertexBuffer, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindVertexArray(vao);
            GL.UseProgram(program);
            var loc = GL.GetUniformLocation(program, "_ModelToClip");
            GL.UniformMatrix4(loc, 1, false, ref mat.m11);
            loc = GL.GetUniformLocation(program, "_MainTex");
            GL.Uniform1(loc, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexIndex / 4);
            GL.Enable(EnableCap.DepthTest);
            GL.BindVertexArray(0);
            vertexIndex = 0;
        }
    }
}
*/