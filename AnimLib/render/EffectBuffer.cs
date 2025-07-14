using OpenTK.Graphics.OpenGL4;
using System;

namespace AnimLib;

internal partial class EffectBuffer : IDisposable {
    int _colorTex1 = -1;
    int _fbo = -1;
    int _sampler = -1;

    int _acesProgram = -1;

    int _width;
    int _height;
    private bool disposedValue;

    IRendererPlatform platform;

    public EffectBuffer(IRendererPlatform platform) {
        _sampler = GL.GenSampler();
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _acesProgram = platform.AddShader(effectVert, acesFrag, null);
        this.platform = platform;
    }

    public void Resize(int width, int height) {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        if(_fbo == -1) {
            _fbo = GL.GenFramebuffer();
        }
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        _colorTex1 = GL.GenTexture();
        var internalFormat = PixelInternalFormat.Rgba16f;
        GL.BindTexture(TextureTarget.Texture2D, _colorTex1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex1, 0);

        var err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if(err != FramebufferErrorCode.FramebufferComplete) {
            Debug.Error("Frame buffer not complete: " + err);
        }
        _width = width;
        _height = height;
        Debug.Log("EffectBuffer resized to " + width + "x" + height);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public void Bind() {
        if (_fbo == -1) {
            Debug.Error("EffectBuffer not initialized");
        }
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.Viewport(0, 0, _width, _height);
    }

    public void ApplyAcesColorMap(IBackendRenderBuffer rb) {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        var (w, h) = rb.Size;
        if (w != _width || h != _height) {
            Resize(w, h);
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.StencilTest);

        GL.BindVertexArray(platform.BlitVao);
        this.Bind();
        GL.UseProgram(_acesProgram);
        int mainTexLoc = GL.GetUniformLocation(_acesProgram, "_MainTex");
        int viewportSizeLoc = GL.GetUniformLocation(_acesProgram, "_ViewportSize");
        GL.Uniform1(mainTexLoc, 0);
        GL.Uniform2(viewportSizeLoc, _width, _height);
        GL.BindTextureUnit(0, rb.Texture());
        GL.BindSampler(0, _sampler);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        
        rb.BindForPostProcess();
        GL.UseProgram(platform.BlitProgram);
        mainTexLoc = GL.GetUniformLocation(platform.BlitProgram, "_MainTex");
        viewportSizeLoc = GL.GetUniformLocation(_acesProgram, "_ViewportSize");
        GL.Uniform2(viewportSizeLoc, _width, _height);
        GL.Uniform1(mainTexLoc, 0);
        GL.BindTextureUnit(0, _colorTex1);
        GL.BindSampler(0, _sampler);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public int Width => _width;
    public int Height => _height;

    public int GetColorTex() {
        return _colorTex1;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            if (_fbo != -1) {
                GL.DeleteFramebuffer(_fbo);
                _fbo = -1;
            }
            if (_colorTex1 != -1) {
                GL.DeleteTexture(_colorTex1);
                _colorTex1 = -1;
            }
            GL.DeleteSampler(_sampler);
            GL.DeleteProgram(_acesProgram);
            Debug.Log("EffectBuffer disposed");
            disposedValue = true;
        }
    }

    ~EffectBuffer()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
