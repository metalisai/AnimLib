using System;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib;

internal partial class DepthPeelRenderBuffer : IBackendRenderBuffer, IDisposable {
    public int _depthTex1 = -1, _depthTex2 = -1, _fbo = -1;
    public int _colorTex = -1;
    public int _entityIdTex = -1;
    int _width = 0, _height = 0;
    int _boundDepthTexture;

    int _entBlitProgram = -1;
    bool _isHDR = true;

    int _blitvao = -1, _blitvbo = -1;
    IPlatform platform;

    public (int w, int h) Size {
        get {
            return (_width, _height);
        }
    }

    public int PeelTex {
        get {
            return _boundDepthTexture == _depthTex2 ? _depthTex1 : _depthTex2;
        }
    }

    public DepthPeelRenderBuffer(IPlatform platform) {
        this.platform = platform;            
        _entBlitProgram = platform.AddShader(canvasBlitVert, canvasBlitFrag, null);
    }

    public int Texture() {
        return _colorTex;
    }

    public int FBO {
        get { return _fbo; }
    }

    public int GetEntityAtPixel(int x, int y) 
    {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
        var pixs = new int[1];
        GL.ReadPixels(x, y, 1, 1, PixelFormat.RedInteger, PixelType.Int, pixs);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
        return pixs[0];
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
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, 0, 0);
                }
                GL.DeleteTexture(_depthTex1);

            }
            if(_depthTex2 != -1) {
                if(_boundDepthTexture == _depthTex2) {
                    _boundDepthTexture = -1;
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, 0, 0);
                }
                GL.DeleteTexture(_depthTex2);
            }
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
        }
    }

    public void Resize(int width, int height) {

        // don't need to resize
        if(this._width == width && this._height == height)
            return;

        // blit VAO hasn't been created yet
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

        // store current state to restore it later
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        // fbo hasn't been created yet
        if(_fbo == -1) {
            _fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        }
        // delete old buffers if they exist
        DeleteBuffers();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        // create new buffers

        _colorTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _colorTex);
        // NOTE: this is RGBA16F HDR
        var internalFormat = _isHDR ? PixelInternalFormat.Rgba16f : PixelInternalFormat.Rgba;
        GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex, 0);

        _entityIdTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _entityIdTex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32i, width, height, 0, PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _entityIdTex, 0);
        
        _depthTex1 = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depthTex1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex1, 0);
        _boundDepthTexture = _depthTex1;

        _depthTex2 = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _depthTex2);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, IntPtr.Zero);
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
        GL.DrawBuffers(2, new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1});
    }

    public void BlitTextureWithEntityId(Texture2D tex, int entityId) {
        var loc = GL.GetUniformLocation(_entBlitProgram, "_EntityId");
        if(loc < 0) {
            //Debug.Error("_EntityId uniform not found in blit shader");
        }
        GL.ProgramUniform1(_entBlitProgram, loc, entityId);
        BlitTexture(tex, _entBlitProgram);
    }

    public void BlitTextureWithEntityId(int tex, int entityId) {
        var loc = GL.GetUniformLocation(_entBlitProgram, "_EntityId");
        if(loc < 0) {
            Debug.Error("_EntityId uniform not found in blit shader");
        }
        GL.ProgramUniform1(_entBlitProgram, loc, entityId);
        BlitTexture(tex, _entBlitProgram);
    }

    public void BlitTexture(int handle, int? blitProgram = null) {
        if(handle < 0) {
            return;
        }
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
        Bind();
        GL.BindVertexArray(_blitvao);
        var bp = blitProgram ?? platform.BlitProgram;
        var loc = GL.GetUniformLocation(bp, "_MainTex");
        GL.UseProgram(bp);
        GL.Uniform1(loc, 0);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
        GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.Zero);
        //GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.BindTextureUnit(0, handle);
        //GL.BindSampler(0, platform.GetSampler(PlatformTextureSampler.Blit));
        GL.BindSampler(0, 0);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public void BlitTexture(Texture2D tex, int? blitProgram = null) {
        platform.LoadTexture(tex);
        BlitTexture(tex.GLHandle, blitProgram);
    }

    public void BlendToScreen(int sw, int sh) {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.DrawBuffer(DrawBufferMode.Back);
        //GL.Clear(ClearBufferMask.ColorBufferBit);
        //GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

        //GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

        GL.BindVertexArray(_blitvao);
        var loc = GL.GetUniformLocation(platform.BlitProgram, "_MainTex");
        GL.UseProgram(platform.BlitProgram);
        GL.Uniform1(loc, 0);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        //GL.BlendEquation(BlendEquationMode.FuncAdd);
        //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
        GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.Zero, BlendingFactorSrc.One, BlendingFactorDest.Zero);

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
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex2, 0);
        } else if(_boundDepthTexture == _depthTex2) {
            _boundDepthTexture = _depthTex1;
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex1, 0);
        }
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public void Dispose() {
        Debug.TLogWithTrace("DepthPeelRenderBuffer Disposing");
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

    public void OnPreRender() {
    }

    public void OnPostRender() {
    }

    public bool IsHDR {
        get {
            return _isHDR;
        }
    }
}
