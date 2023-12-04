using System;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib;

internal partial class GlKawaseBlur : IDisposable
{
    private bool disposedValue;

    private int _kawaseDownProgram = -1;
    private int _kawaseUpProgram = -1;

    int _colorTex1 = -1;
    int _colorTex2 = -1;
    int _fbo = -1;
    int _width;
    int _height;

    int _sampler = -1;

    OpenTKPlatform platform;

    public float Radius { get; set; } = 1.0f;
    public float Threshold { get; set; } = 1.0f;

    public GlKawaseBlur(OpenTKPlatform platform) {
        _kawaseDownProgram = platform.AddShader(effectVert, kawaseBlurDown13Frag, null);
        _kawaseUpProgram = platform.AddShader(effectVert, kawaseBlurUpFrag, null);

        _sampler = GL.GenSampler();
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        this.platform = platform;
    }

    public void Resize(int width, int height) {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        if(_fbo == -1) {
            _fbo = GL.GenFramebuffer();
        }

        if(_colorTex1 != -1) {
            GL.DeleteTexture(_colorTex1);
        }

        if(_colorTex2 != -1) {
            GL.DeleteTexture(_colorTex2);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        var internalFormat = PixelInternalFormat.Rgba16f;
        int CreateTex() {
            var ret = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, ret);
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            return ret;
        }

        _colorTex1 = CreateTex();
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex1, 0);

        _colorTex2 = CreateTex();
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _colorTex2, 0);

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

    public void ApplyBlur(IBackendRenderBuffer rb) {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        var (w, h) = rb.Size;
        // first mip level of target is half the size of the source
        var selfW = w / 2;
        var selfH = h / 2;

        if (_width != selfW || _height != selfH) {
            Resize(selfW, selfH);
            Debug.Log("Resized kawase blur buffer to " + selfW + "x" + selfH);
        }

        GL.UseProgram(_kawaseDownProgram);
        var viewportLoc = GL.GetUniformLocation(_kawaseDownProgram, "_ViewportSize");
        var texLoc = GL.GetUniformLocation(_kawaseDownProgram, "_MainTex");
        var usePrevTexLoc = GL.GetUniformLocation(_kawaseDownProgram, "_UsePrevTex");
        var thresholdLoc = GL.GetUniformLocation(_kawaseDownProgram, "_Threshold");
        GL.Uniform2(viewportLoc, selfW, selfH);
        GL.Uniform1(texLoc, 0);
        GL.BindTextureUnit(0, rb.Texture());
        GL.BindSampler(0, _sampler);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.StencilTest);
        GL.BindVertexArray(platform.blitvao);
        GL.DrawBuffers(1, new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0 });

        GL.Uniform1(thresholdLoc, this.Threshold);

        // down pass 1 (from source to mip level 0)
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex1, 0);
        GL.Viewport(0, 0, selfW, selfH);
        GL.Uniform2(viewportLoc, selfW, selfH);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.Uniform1(thresholdLoc, 0.0f);

        GL.BindTexture(TextureTarget.Texture2D, _colorTex1);
        GL.BindTextureUnit(0, _colorTex1);

        int passes = 3;

        // down passes
        for (int i = 0; i < passes; i++) {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex1, i+1);
            int div = 2 << i;
            GL.Viewport(0, 0, selfW/div, selfH/div);
            GL.Uniform2(viewportLoc, selfW/div, selfH/div);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, i);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, i);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        // restore mip state
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 1000);

        GL.UseProgram(_kawaseUpProgram);
        viewportLoc = GL.GetUniformLocation(_kawaseUpProgram, "_ViewportSize");
        texLoc = GL.GetUniformLocation(_kawaseUpProgram, "_MainTex");
        usePrevTexLoc = GL.GetUniformLocation(_kawaseUpProgram, "_UsePrevTex");
        int prevTexLoc = GL.GetUniformLocation(_kawaseUpProgram, "_PrevTex");
        int lodLoc = GL.GetUniformLocation(_kawaseUpProgram, "_MipLevel");
        int radiusLoc = GL.GetUniformLocation(_kawaseUpProgram, "_Radius");
        GL.Uniform1(radiusLoc, this.Radius);

        GL.BindSampler(1, _sampler);
        GL.BindSampler(0, _sampler);
        GL.Uniform1(texLoc, 0);
        GL.Uniform1(prevTexLoc, 1);

        // up passes _colorTex1 -> _colorTex2
        // blur current mip level and add to previous mip level (if any)
        for (int i = passes ; i >= 0; i--) {
            if (i == passes) {
                GL.Uniform1(usePrevTexLoc, 0);
            } else {
                GL.Uniform1(usePrevTexLoc, 1);
            }
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex2, i);
            int div = 1 << i;
            GL.Viewport(0, 0, selfW/div, selfH/div);
            GL.Uniform2(viewportLoc, selfW/div, selfH/div);
            GL.Uniform1(lodLoc, i);

            GL.BindTexture(TextureTarget.Texture2D, _colorTex2);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, i+1);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, i+1);
            GL.BindTextureUnit(1, _colorTex2);
            GL.BindTextureUnit(0, _colorTex1);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        GL.BindTexture(TextureTarget.Texture2D, _colorTex2);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 1000);

        rb.Bind();
        GL.UseProgram(platform.BlitProgram);

        // blit back onto source with additive blending
        viewportLoc = GL.GetUniformLocation(platform.BlitProgram, "_ViewportSize");
        texLoc = GL.GetUniformLocation(platform.BlitProgram, "_MainTex");
        GL.Uniform1(texLoc, 0);
        GL.Uniform2(viewportLoc, w, h);
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.One, BlendingFactorSrc.Zero, BlendingFactorDest.One);
        GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);

        GL.Viewport(0, 0, w, h);
        GL.Uniform2(viewportLoc, w, h);
        GL.Uniform1(lodLoc, 0);
        GL.BindTextureUnit(0, _colorTex2);
        GL.BindSampler(0, _sampler);
        GL.BindVertexArray(platform.blitvao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // managed state
            }

            // unmanaged state
            platform.DeleteShader(_kawaseDownProgram);
            platform.DeleteShader(_kawaseUpProgram);
            GL.DeleteSampler(_sampler);

            if (_colorTex1 != -1) {
                GL.DeleteTexture(_colorTex1);
                _colorTex1 = -1;
            }

            if (_colorTex2 != -1) {
                GL.DeleteTexture(_colorTex2);
                _colorTex2 = -1;
            }

            if (_fbo != -1) {
                GL.DeleteFramebuffer(_fbo);
                _fbo = -1;
            }

            disposedValue = true;
        }
    }

     ~GlKawaseBlur()
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
