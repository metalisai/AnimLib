using OpenTK.Graphics.OpenGL4;
using System;

namespace AnimLib;

internal partial class EffectBuffer : IDisposable {
    int _colorTex1;
    int _fbo = -1;

    int _width;
    int _height;
    private bool disposedValue;

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
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, _colorTex1, 0);

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
        GL.DrawBuffer(DrawBufferMode.ColorAttachment2);
        GL.Viewport(0, 0, _width, _height);
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
