using System;
using System.Collections.Generic;
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
        }

        ITypeSetter ts;

        Dictionary<GlyphKey, CachedGlyph> _cachedGlyphs = new Dictionary<GlyphKey, CachedGlyph>();

        int vertexIndex = 0;
        float[] vertexBuffer = new float[24000];
        int[] entityIdBuffer = new int[2700];

        byte[,] cache = new byte[TEXTURE_WIDTH, TEXTURE_HEIGHT];
        int tex;
        int vao, vbo;
        int vbo2;

        int curMaxHeight; // max height in current row
        int curX; // current offset in font atlas
        int curY;

        const int TEXTURE_WIDTH = 4096;
        const int TEXTURE_HEIGHT = 4096;

        bool cacheDirty = false;

        internal void RenderGlyph(char c, float size, FontGlyph fg) {
            int gend = curX + fg.w + 1;
            if(gend >= TEXTURE_WIDTH) {
                curY += curMaxHeight+1;
                curMaxHeight = fg.h;
                curX = 0;
            }
            if(fg.h > curMaxHeight) {
                curMaxHeight = fg.h;
            }
            int startX = curX;
            int startY = curY;
            int endX = curX + fg.w;
            int endY = curY + fg.h;
            curX += fg.w + 1;
            if(curY + curMaxHeight >= TEXTURE_HEIGHT) {
                throw new Exception();
            }
            if(endX >= TEXTURE_HEIGHT) {
                throw new Exception();
            }
            var cg = new CachedGlyph() {
                X = ((float)startX / (float)TEXTURE_WIDTH),
                Y = 1f - ((float)endY / (float)TEXTURE_HEIGHT),
                MaxX = ((float)endX / (float)TEXTURE_WIDTH),
                MaxY = 1f - ((float)startY / (float)TEXTURE_HEIGHT),
            };

            var key = new GlyphKey {
                c = c,
                size = size    
            };
            _cachedGlyphs.Add(key, cg);
            ts.RenderGlyph(c, size, cache, startX, startY);
            cacheDirty = true;
        }

        internal FontCache(ITypeSetter ts) {
            /*for(int i = 33; i < 127; i++) {
                var surface = RenderSurface((char)i, font);
                textures[i] = surface;
            }*/
            SetupVAO();
            tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TEXTURE_WIDTH, TEXTURE_HEIGHT, 0, PixelFormat.Red, PixelType.UnsignedByte, ref cache[0,0]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            this.ts = ts;
        }

        void UpdateCache() {
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TEXTURE_WIDTH, TEXTURE_HEIGHT, 0, PixelFormat.Red, PixelType.UnsignedByte, ref cache[0,0]);
            cacheDirty = false;
        }

        public void SetupVAO() {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            var vData = new float[] {
               -200f, -200f, 0.0f, 0.0f, 0.0f,
                200f, -200f, 0.0f, 1.0f, 0.0f,
               -200f,  200f, 0.0f, 0.0f, 1.0f,
                200f, -200f, 0.0f, 1.0f, 0.0f,
                200f,  200f, 0.0f, 1.0f, 1.0f,
               -200f,  200f, 0.0f, 0.0f, 1.0f,
            };
            //vData.CopyTo(vertexBuffer, 0);
            //vertexIndex = vData.Length;
            GL.BufferData(BufferTarget.ArrayBuffer, vertexBuffer.Length * sizeof(float), vertexBuffer, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 36, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 36, new IntPtr(20));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 36, 12);

            // for entity id
            vbo2 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo2);
            GL.BufferData(BufferTarget.ArrayBuffer, entityIdBuffer.Length*sizeof(int), entityIdBuffer, BufferUsageHint.DynamicDraw);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribIPointer(3, 1, VertexAttribIntegerType.Int, 0, new IntPtr(0));
            GL.BindVertexArray(0);


            //PushString("Testing!", new Vector2(-200.0f, 200.0f));
        }

        public int RenderSurface(char c, float size) {
            var fg = ts.GetGlyph(c, size);
            RenderGlyph(c, size, fg);
            return tex;
        }

        public void PushCharacter(GlyphState ch, Vector3 pos) {
            var vbuf = new List<float>();
            var key = new GlyphKey {
                c = ch.glyph,
                size = ch.size   
            };
            CachedGlyph g;
            if(!_cachedGlyphs.TryGetValue(key, out g)) {
                var surface = RenderSurface((char)key.c, key.size);
                g = _cachedGlyphs[key];
            }
            FontGlyph fg = ts.GetGlyph(key.c, key.size);
            Vector2 min = pos;
            Vector2 max = (Vector2)pos + new Vector2(fg.w, fg.h);
            float z = 0.0f;
            Vector4 colorf = ch.color.ToVector4();

            vbuf.Add(min.x); vbuf.Add(min.y); vbuf.Add(z);
            vbuf.Add(g.X); vbuf.Add(g.MaxY);
            vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

            vbuf.Add(max.x); vbuf.Add(min.y); vbuf.Add(z);
            vbuf.Add(g.MaxX); vbuf.Add(g.MaxY);
            vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

            vbuf.Add(min.x); vbuf.Add(max.y); vbuf.Add(z);
            vbuf.Add(g.X); vbuf.Add(g.Y);
            vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

            vbuf.Add(max.x); vbuf.Add(min.y); vbuf.Add(z);
            vbuf.Add(g.MaxX); vbuf.Add(g.MaxY);
            vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

            vbuf.Add(max.x); vbuf.Add(max.y); vbuf.Add(z);
            vbuf.Add(g.MaxX); vbuf.Add(g.Y);
            vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

            vbuf.Add(min.x); vbuf.Add(max.y); vbuf.Add(z);
            vbuf.Add(g.X); vbuf.Add(g.Y);
            vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

            for(int i = 0 ; i < vbuf.Count; i+=9, vertexIndex+=9) {
                for(int j = 0; j < 9; j++) {
                    vertexBuffer[vertexIndex+j] = vbuf[i+j];
                }
                entityIdBuffer[vertexIndex/9] = ch.entityId;
            }
        }

        // TODO: this is not right
        public void PushString(string s, Vector3 pos, float size, Color color, int entityId, TextHorizontalAlignment horizontalAlignment = TextHorizontalAlignment.Left, TextVerticalAlignment verticalAlignment = TextVerticalAlignment.Up) {
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
                    var surface = RenderSurface((char)c, size);
                    g = _cachedGlyphs[key];
                }
                FontGlyph fg = ts.GetGlyph(c, size);
                float kernX = 0.0f;
                if(i > 0) {
                    kernX = ts.GetKerning(s[i-1], c);
                }
                float baseY = pos.y + size;

                float yd = fg.h - fg.bearingY;

                float lowX = x + fg.bearingX + kernX;
                float highX = lowX+fg.w;
                float lowY = baseY+yd;
                float highY = baseY-fg.h+yd;

                // TODO: use lookup table!

                Vector4 colorf = color.ToVector4();

                // lower left
                // pos
                vbuf.Add(lowX);
                vbuf.Add(lowY);
                vbuf.Add(pos.z);
                // tex coord
                vbuf.Add(g.X);
                vbuf.Add(g.Y);
                vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

                // lower right
                vbuf.Add(highX);
                vbuf.Add(lowY);
                vbuf.Add(pos.z);
                vbuf.Add(g.MaxX);
                vbuf.Add(g.Y);
                vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

                // upper left
                vbuf.Add(lowX);
                vbuf.Add(highY);
                vbuf.Add(pos.z);
                vbuf.Add(g.X);
                vbuf.Add(g.MaxY);
                vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

                // lower right
                vbuf.Add(highX);
                vbuf.Add(lowY);
                vbuf.Add(pos.z);
                vbuf.Add(g.MaxX);
                vbuf.Add(g.Y);
                vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

                // upper right
                vbuf.Add(highX);
                vbuf.Add(highY);
                vbuf.Add(pos.z);
                vbuf.Add(g.MaxX);
                vbuf.Add(g.MaxY);
                vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);

                // upper left
                vbuf.Add(lowX);
                vbuf.Add(highY);
                vbuf.Add(pos.z);
                vbuf.Add(g.X);
                vbuf.Add(g.MaxY);
                vbuf.Add(colorf.x); vbuf.Add(colorf.y); vbuf.Add(colorf.z); vbuf.Add(colorf.w);
                x = x + fg.hAdvance + kernX;
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
                    offset.y = -size/1.75f;
                    break;
                case TextVerticalAlignment.Down:
                    offset.y = -size;
                    break;
            }
            for(int i = 0 ; i < vbuf.Count; i+=9, vertexIndex+=9) {
                vertexBuffer[vertexIndex+0] = vbuf[i+0]+offset.x;
                vertexBuffer[vertexIndex+1] = vbuf[i+1]+offset.y;
                vertexBuffer[vertexIndex+2] = vbuf[i+2];
                vertexBuffer[vertexIndex+3] = vbuf[i+3];
                vertexBuffer[vertexIndex+4] = vbuf[i+4];
                vertexBuffer[vertexIndex+5] = vbuf[i+5];
                vertexBuffer[vertexIndex+6] = vbuf[i+6];
                vertexBuffer[vertexIndex+7] = vbuf[i+7];
                vertexBuffer[vertexIndex+8] = vbuf[i+8];
                entityIdBuffer[vertexIndex/9] = entityId;
            }
        }

        public void RenderTest(int program, M4x4 mat) {

            if(cacheDirty) {
                UpdateCache();
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexBuffer.Length * sizeof(float), vertexBuffer, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo2);
            GL.BufferData(BufferTarget.ArrayBuffer, entityIdBuffer.Length*sizeof(int), entityIdBuffer, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindVertexArray(vao);
            GL.UseProgram(program);
            var loc = GL.GetUniformLocation(program, "_ModelToClip");
            GL.UniformMatrix4(loc, 1, false, ref mat.m11);
            loc = GL.GetUniformLocation(program, "_MainTex");
            GL.Uniform1(loc, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexIndex / 9);
            GL.BindVertexArray(0);
            vertexIndex = 0;
        }
    }
}
