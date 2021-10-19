using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib {
    public interface IRenderBuffer : IDisposable{
            public abstract void Bind();
            public abstract void BlendToScreen(int screenWidth, int screenHeight, int program);
            public abstract void Resize(int width, int height);
            public abstract int Texture();
            public abstract void Clear();
            public (int, int) Size { get; }

            public abstract void ReadPixels(ref byte data);
        }

        public class DepthPeelRenderBuffer : IRenderBuffer, IDisposable {
            public int _depthTex1 = -1, _depthTex2 = -1, _fbo = -1;
            public int _colorTex = -1;
            public int _entityIdTex = -1;
            int _width, _height;
            int _boundDepthTexture;

            int _blitvao = -1, _blitvbo = -1;

            public (int,int) Size {
                get {
                    return (_width, _height);
                }
            }

            public int PeelTex {
                get {
                    return _boundDepthTexture == _depthTex2 ? _depthTex1 : _depthTex2;
                }
            }

            public DepthPeelRenderBuffer() {
                
            }

            public int Texture() {
                return _colorTex;
            }

            void DeleteBuffers() {
                if(_fbo != -1) {
                    int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
                    int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
                    if(_colorTex != -1) {
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, 0, 0);
                        GL.DeleteTexture(_colorTex);
                        _colorTex = -1;
                    }
                    if(_entityIdTex != -1) {
                        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, 0, 0);
                        GL.DeleteTexture(_entityIdTex);
                        _entityIdTex = -1;
                    }
                    if(_depthTex1 != -1) {
                        if(_boundDepthTexture == _depthTex1) {
                            _boundDepthTexture = -1;
                            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, 0, 0);
                        }
                        GL.DeleteTexture(_depthTex1);

                    }
                    if(_depthTex2 != -1) {
                        if(_boundDepthTexture == _depthTex2) {
                            _boundDepthTexture = -1;
                            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, 0, 0);
                        }
                        GL.DeleteTexture(_depthTex2);
                    }
                    GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
                    GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
                }
            }

            public void Resize(int width, int height) {

                if(_blitvao == -1) {
                    _blitvao = GL.GenVertexArray();
                    GL.BindVertexArray(_blitvao);
                    _blitvbo = GL.GenBuffer();
                    float[] quad = new float[] {
                        -1.0f, -1.0f, 0.0f, 1.0f,
                        1.0f, -1.0f, 0.0f, 1.0f,
                        -1.0f,  1.0f, 0.0f, 1.0f,
                        1.0f, -1.0f, 0.0f, 1.0f,
                        1.0f,  1.0f, 0.0f, 1.0f,
                        -1.0f,  1.0f, 0.0f, 1.0f,
                    };
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _blitvbo);
                    GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);
                    GL.EnableVertexAttribArray(0);
                    GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);
                    GL.BindVertexArray(0);
                }

                int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
                int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

                if(_fbo == -1) {
                    _fbo = GL.GenFramebuffer();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
                }
                DeleteBuffers();

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

                _colorTex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _colorTex);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex, 0);

                _entityIdTex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _entityIdTex);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32i, width, height, 0, PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _entityIdTex, 0);
                
                _depthTex1 = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _depthTex1);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32f, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthTex1, 0);
                _boundDepthTexture = _depthTex1;

                _depthTex2 = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _depthTex2);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32f, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex2, 0);

                var err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if(err != FramebufferErrorCode.FramebufferComplete) {
                    System.Diagnostics.Debug.Fail("Frame buffer not complete: " + err);
                }
                _width = width;
                _height = height;

                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
            }

            public void Bind() {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            }

            public void BlendToScreen(int sw, int sh, int _blitProgram) {
                int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
                int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.DrawBuffer(DrawBufferMode.Back);
                //GL.Clear(ClearBufferMask.ColorBufferBit);
                //GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

                //GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

                GL.BindVertexArray(_blitvao);
                var loc = GL.GetUniformLocation(_blitProgram, "_MainTex");
                GL.UseProgram(_blitProgram);
                GL.Uniform1(loc, 0);
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.Blend);
                GL.BlendEquation(BlendEquationMode.FuncAdd);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                /*for(int i = 0; i < _colorTex.Length; i++) {
                    GL.BindTextureUnit(0, _colorTex[_colorTex.Length-1 - i]);
                    //GL.BindTextureUnit(0, _colorTex[1]);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                }*/
                GL.BindTextureUnit(0, _colorTex);
                /*GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);*/
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

                GL.BindVertexArray(0);

                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
            }

            // next peel layer
            public void NextLayer() {
                int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
                int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
                if(_boundDepthTexture == _depthTex1) {
                    _boundDepthTexture = _depthTex2;
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthTex2, 0);
                } else if(_boundDepthTexture == _depthTex2) {
                    _boundDepthTexture = _depthTex1;
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _depthTex1, 0);
                }
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
            }

            public void Dispose() {
                DeleteBuffers();
                GL.DeleteFramebuffer(_fbo);
                GL.DeleteVertexArray(_blitvao);
                GL.DeleteBuffer(_blitvbo);
                _blitvao = -1;
                _blitvbo = -1;
                _fbo = -1;
            }

            public void Clear()
            {
                //GL.DrawBuffers(3, new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2});
                int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);
                GL.DrawBuffers(2, new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1});
                int clearId = -1;
                GL.ClearBuffer(ClearBuffer.Color, 1, ref clearId);
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                GL.ColorMask(true, true, true, true);
                GL.ClearColor(1.0f, 1.0f, 1.0f, 0.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
            }

            public void ReadPixels(ref byte data)
            {
                //GL.ReadPixels(0, 0, Width, Height, PixelFormat.Rgb, PixelType.UnsignedByte, ref data);
                GL.BindTexture(TextureTarget.Texture2D, _colorTex);
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ref data);
            }
        }
}
