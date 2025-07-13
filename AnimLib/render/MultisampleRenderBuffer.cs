using System;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib;

internal class MultisampleRenderBuffer : IBackendRenderBuffer, IDisposable{
    int _fbo = -1;
    int _blitFbo = -1;
    int _colorTex = -1;
    int _blitTex = -1;
    int _blitEntTex = -1;
    int _entityIdTex = -1;
    int _width, _height;
    int _blitvao = -1, _blitvbo = -1;
    IRendererPlatform platform;

    public FrameColorSpace ColorSpace { get; private set; }

    public MultisampleRenderBuffer(IRendererPlatform platform, FrameColorSpace colorSpace) {
        ColorSpace = colorSpace;
        this.platform = platform;
    }

    public void BindForRender()
    {
        if(_fbo != -1)
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        else {
            Debug.Warning("Binding framebuffer before its created!");
        }
    }

    public int GetEntityAtPixel(int x, int y) 
    {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _blitFbo);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
        var pixs = new int[1];
        GL.ReadPixels(x, y, 1, 1, PixelFormat.RedInteger, PixelType.Int, pixs);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
        return pixs[0];
    }

    public void BlendToScreen(int screenWidth, int screenHeight)
    {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.DrawBuffer(DrawBufferMode.Back);
        GL.BindVertexArray(_blitvao);
        var loc = GL.GetUniformLocation(platform.BlitProgram, "_MainTex");
        GL.UseProgram(platform.BlitProgram);
        GL.Uniform1(loc, 0);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BindTextureUnit(0, _blitTex);
        GL.BindSampler(0, platform.GetSampler(PlatformTextureSampler.Blit));
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);

        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    const int MULTISAMPLE_SAMPLES = 4;

    public void Resize(int width, int height)
    {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        // don't need to resize
        if(this._width == width && this._height == height)
            return;

        // blit VAO hasn't been created yet
        // TODO: this belongs to renderer not here
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

        if(_fbo == -1) {
            _fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            _blitFbo = GL.GenFramebuffer();
        }
        DeleteBuffers();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        _colorTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DMultisample, _colorTex);
        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, MULTISAMPLE_SAMPLES, PixelInternalFormat.Rgba, width, height, true);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, _colorTex, 0);

        _entityIdTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DMultisample, _entityIdTex);
        GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, MULTISAMPLE_SAMPLES, PixelInternalFormat.R32i, width, height, true);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2DMultisample, _entityIdTex, 0);

        var err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if(err != FramebufferErrorCode.FramebufferComplete) {
            System.Diagnostics.Debug.Fail("Framebuffer not complete: " + err);
        }

        // create non multisampled fbo (to blit final texture to)

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _blitFbo);

        _blitTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _blitTex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _blitTex, 0);

        _blitEntTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _blitEntTex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32i, width, height, 0, PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _blitEntTex, 0);

        err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if(err != FramebufferErrorCode.FramebufferComplete) {
            System.Diagnostics.Debug.Fail("Blit Framebuffer not complete: " + err);
        }

        _width = width;
        _height = height;
    }

    public int Texture()
    {
        return _blitTex;
    }

    public int FBO {
        get {
            return _fbo;
        }
    }

    public void Clear()
    {
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

    public (int, int) Size { get { return (_width, _height); } }

    public void ReadPixels(ref byte data, Texture2D.TextureFormat format = Texture2D.TextureFormat.RGB8)
    {
        if(format != Texture2D.TextureFormat.RGB8)
            throw new NotImplementedException();
        GL.BindTexture(TextureTarget.Texture2D, _blitTex);
        GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ref data);
    }

    void DeleteBuffers() {
        if(_fbo != -1) {
            int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
            int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
            // delete multisampled buffers
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            if(_colorTex != -1) {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, 0, 0);
                GL.DeleteTexture(_colorTex);
                _colorTex = -1;
            }
            if(_entityIdTex != -1) {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, 0, 0);
                GL.DeleteTexture(_entityIdTex);
                _entityIdTex = -1;
            }
            // delete regular buffers
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _blitFbo);
            if(_blitTex != -1) {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, 0, 0);
                GL.DeleteTexture(_blitTex);
                _blitTex = -1;
            }
            if(_blitEntTex != -1) {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, 0, 0);
                GL.DeleteTexture(_blitEntTex);
                _blitEntTex = -1;
            }

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
        }
    }

    public void OnPreRender() {
    }

    public void OnPostRender() {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
        // Note: must blit, can't read multisampled texture directly
        // blit color buffer
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _blitFbo);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        // blit entity id buffer
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _blitFbo);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment1);
        GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public bool IsMultisampled { get { return true; } }

    public void Dispose() {
        DeleteBuffers();
        GL.DeleteFramebuffer(_fbo);
        GL.DeleteFramebuffer(_blitFbo);
        if(_blitvao != -1) {
            GL.DeleteVertexArray(_blitvao);
            GL.DeleteBuffer(_blitvbo);
            _blitvao = -1;
            _blitvbo = -1;
        }
        _fbo = -1;
        _blitFbo = -1;
    }

    public bool IsHDR { get { return false; } }

    public void MakePresentable() {
        throw new NotImplementedException();
    }

    public void BindForPostProcess() {
        throw new NotImplementedException();
    }
}
