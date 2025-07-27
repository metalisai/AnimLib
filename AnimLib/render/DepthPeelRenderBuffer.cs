using System;
using System.Linq;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib;

internal partial class DepthPeelRenderBuffer : IBackendRenderBuffer, IDisposable {
    int _depthTex1 = -1, _depthTex2 = -1, _fbo = -1;
    int _colorTex = -1;
    int _entityIdTex = -1;
    int _width = 0, _height = 0;
    int _boundDepthTexture;

    int _entBlitProgram = -1;
    bool _isHDR = true;

    int _presentTex = -1;

    bool _multisample = true;

    int _sampler = -1;

    int _samples = 1;

    // multisampled render buffer needs to be blitted to a non-multisampled texture
    int _msPresentTex = -1;
    int _msPresentFBO = -1;

    IRendererPlatform renderPlatform;

    public (int w, int h) Size
    {
        get
        {
            return (_width, _height);
        }
    }

    public int PeelTex {
        get {
            return _boundDepthTexture == _depthTex2 ? _depthTex1 : _depthTex2;
        }
    }

    public bool IsMultisampled {
        get {
            return _multisample;
        }
    }

    public FrameColorSpace ColorSpace { get; private set; }

    public DepthPeelRenderBuffer(IRendererPlatform platform, FrameColorSpace colorSpace, bool multisample) {
        // this is an OpenGL implementation and requires an OpenGL platform
        ColorSpace = colorSpace;
        this.renderPlatform = platform;
        _entBlitProgram = platform.AddShader(canvasBlitVert, canvasBlitFrag, null);
        _sampler = GL.GenSampler();
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.SamplerParameter(_sampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        if (multisample) {
            int maxSamples = GL.GetInteger(GetPName.MaxSamples);
            int maxColorSamples = GL.GetInteger(GetPName.MaxColorTextureSamples);
            int maxIntSamples = GL.GetInteger(GetPName.MaxIntegerSamples);
            int maxDepthSamples = GL.GetInteger(GetPName.MaxDepthTextureSamples);
            Debug.TLog($"OpenGL max supported samples: {maxSamples}");
            Debug.TLog($"OpenGL max supported color texture samples: {maxColorSamples}");
            Debug.TLog($"OpenGL max supported integer texture samples: {maxIntSamples}");
            Debug.TLog($"OpenGL max supported depth texture samples: {maxDepthSamples}");
            var minSamples = ((int[])[maxSamples, maxColorSamples, maxIntSamples, maxDepthSamples]).Min();
            Debug.Log($"Usable sample count: {minSamples}");
            if (minSamples <= 1) {
                Debug.Warning("Multisampling requested, but usable sample count is 1. Disabling MSAA.");
                _multisample = false;
            } else {
                _multisample = true;
            }
            _samples = minSamples;
        } else {
            _multisample = false;
        }
    }

    public int Texture() {
        return _presentTex;
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
        if (_msPresentFBO != -1) {
            if (_msPresentTex != -1) {
                GL.DeleteTexture(_msPresentTex);
                _msPresentTex = -1;
            }
        }
    }

    public void Resize(int width, int height) {

        // don't need to resize
        if(this._width == width && this._height == height)
            return;

        // store current state to restore it later
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        // fbo hasn't been created yet
        if(_fbo == -1) {
            _fbo = GL.GenFramebuffer();
        }
        // delete old buffers if they exist
        DeleteBuffers();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        // create new buffers

        _colorTex = GL.GenTexture();
        _entityIdTex = GL.GenTexture();
        _depthTex1 = GL.GenTexture();
        _depthTex2 = GL.GenTexture();
        _boundDepthTexture = _depthTex1;
        // NOTE: this is RGBA16F HDR
        var internalFormat = _isHDR ? PixelInternalFormat.Rgba16f : PixelInternalFormat.Rgba;

        if (!_multisample) {
            Debug.Log("Creating non-multisampled render buffer");
            GL.BindTexture(TextureTarget.Texture2D, _colorTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTex, 0);

            GL.BindTexture(TextureTarget.Texture2D, _entityIdTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32i, width, height, 0, PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _entityIdTex, 0);

            GL.BindTexture(TextureTarget.Texture2D, _depthTex1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, IntPtr.Zero);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex1, 0);

            GL.BindTexture(TextureTarget.Texture2D, _depthTex2);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, IntPtr.Zero);
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex2, 0);

            _presentTex = _colorTex;
        } else {
            Debug.Log($"Creating multisampled render buffer (MSAA x{_samples})");

            // this seems to be the max on most (even recent) hardware
            // some drivers fake 16x with supersampling
            Debug.Log("Creating color texture");
            GL.BindTexture(TextureTarget.Texture2DMultisample, _colorTex);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, _samples, internalFormat, width, height, true);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, _colorTex, 0);

            Debug.Log("Creating entityId texture");
            // this gives FramebufferIncompleteMultisample
            //GL.BindTexture(TextureTarget.Texture2D, _entityIdTex);
            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32i, width, height, 0, PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _entityIdTex, 0);
            GL.BindTexture(TextureTarget.Texture2DMultisample, _entityIdTex);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, _samples, PixelInternalFormat.R32i, width, height, true);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2DMultisample, _entityIdTex, 0);

            Debug.Log("Creating depth texture 1");
            GL.BindTexture(TextureTarget.Texture2DMultisample, _depthTex1);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, _samples, PixelInternalFormat.Depth24Stencil8, width, height, true);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2DMultisample, _depthTex1, 0);

            Debug.Log("Creating depth texture 2");
            GL.BindTexture(TextureTarget.Texture2DMultisample, _depthTex2);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, _samples, PixelInternalFormat.Depth24Stencil8, width, height, true);
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2DMultisample, _depthTex2, 0);

            // make a non-multisampled for presentable texture
            {
                Debug.Log("Creating non-multisampled render texture");
                if (_msPresentFBO == -1) {
                    _msPresentFBO = GL.GenFramebuffer();
                }
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, _msPresentFBO);
                _msPresentTex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, _msPresentTex);
                GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _msPresentTex, 0);
                var err2 = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if(err2 != FramebufferErrorCode.FramebufferComplete) {
                    throw new Exception("Present frame buffer not complete: " + err2);
                }
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            _presentTex = _msPresentTex;
        }

        var err = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if(err != FramebufferErrorCode.FramebufferComplete) {
            throw new Exception("Render frame buffer not complete: " + err);
        }
        _width = width;
        _height = height;

        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public void BindForRender() {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        GL.DrawBuffers(2, new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1});
        GL.Viewport(0, 0, _width, _height);
    }

    public void BlitTextureWithEntityId(Texture2D tex, int entityId) {
        var loc = GL.GetUniformLocation(_entBlitProgram, "_EntityId");
        var viewPortLoc = GL.GetUniformLocation(_entBlitProgram, "_ViewportSize");
        if(loc < 0) {
            //Debug.Error("_EntityId uniform not found in blit shader");
        }
        GL.ProgramUniform1(_entBlitProgram, loc, entityId);
        GL.ProgramUniform2(_entBlitProgram, viewPortLoc, _width, _height);
        BlitTexture(tex, _entBlitProgram);
    }

    public void BlitTextureWithEntityId(int tex, int entityId) {
        var loc = GL.GetUniformLocation(_entBlitProgram, "_EntityId");
        var viewPortLoc = GL.GetUniformLocation(_entBlitProgram, "_ViewportSize");
        if(loc < 0) {
            Debug.Error("_EntityId uniform not found in blit shader");
        }
        GL.ProgramUniform2(_entBlitProgram, viewPortLoc, _width, _height);
        GL.ProgramUniform1(_entBlitProgram, loc, entityId);
        BlitTexture(tex, _entBlitProgram);
    }

    public void ApplyEffect(EffectBuffer ebuf, bool horizontal) {
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);

        GL.BindVertexArray(renderPlatform.BlitVao);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);

        ebuf.Bind();
        GL.BindTextureUnit(0, _colorTex);
        GL.BindSampler(0, _sampler);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.UseProgram(renderPlatform.BlitProgram);
        var loc = GL.GetUniformLocation(renderPlatform.BlitProgram, "_MainTex");
        var vpsLoc = GL.GetUniformLocation(renderPlatform.BlitProgram, "_ViewportSize");
        GL.Uniform1(loc, 0);
        GL.Uniform2(vpsLoc, _width, _height);

        this.BindForRender();
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);

        // copy back to color tex
        var buffers2 = new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0 };
        GL.DrawBuffers(buffers2.Length, buffers2);
        GL.BindTextureUnit(0, ebuf.GetColorTex());
        GL.BindSampler(0, _sampler);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        GL.BindSampler(0, 0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public void BlitTexture(int handle, int? blitProgram = null) {
        if(handle < 0) {
            return;
        }
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
        BindForRender();
        GL.BindVertexArray(renderPlatform.BlitVao);
        var bp = blitProgram ?? renderPlatform.BlitProgram;
        var loc = GL.GetUniformLocation(bp, "_MainTex");
        var vpsLoc = GL.GetUniformLocation(bp, "_ViewportSize");
        GL.UseProgram(bp);
        GL.Uniform1(loc, 0);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
        GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.Zero);
        //GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.BindTextureUnit(0, handle);
        GL.BindSampler(0, _sampler);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
        GL.BindSampler(0, 0);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public void BlitTexture(Texture2D tex, int? blitProgram = null) {
        renderPlatform.LoadTexture(tex);
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

        GL.BindVertexArray(renderPlatform.BlitVao);
        var loc = GL.GetUniformLocation(renderPlatform.BlitProgram, "_MainTex");
        var vpsLoc = GL.GetUniformLocation(renderPlatform.BlitProgram, "_ViewportSize");
        GL.UseProgram(renderPlatform.BlitProgram);
        GL.Uniform1(loc, 0);
        GL.Uniform2(vpsLoc, sw, sh);
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
        /*GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);*/
        GL.BindTextureUnit(0, _colorTex);
        GL.BindSampler(0, _sampler);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        GL.BindSampler(0, 0);
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
            if (!_multisample) {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex2, 0);
            }
            else {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2DMultisample, _depthTex2, 0);
            }
        } else if(_boundDepthTexture == _depthTex2) {
            _boundDepthTexture = _depthTex1;
            if (!_multisample) {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, _depthTex1, 0);
            }
            else {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2DMultisample, _depthTex1, 0);
            }
        }
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
    }

    public void Dispose() {
        Debug.TLogWithTrace("DepthPeelRenderBuffer Disposing");
        DeleteBuffers();
        GL.DeleteFramebuffer(_fbo);
        if (_msPresentFBO != -1) {
            GL.DeleteFramebuffer(_msPresentFBO);
        }
        _fbo = -1;
        _msPresentFBO = -1;
        GL.DeleteSampler(_sampler);
    }

    public void Clear()
    {
        //GL.DrawBuffers(3, new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2});
        int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);
        var buffers = new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2};
        GL.DrawBuffers(buffers.Length, buffers);
        //int clearId = -1;
        //GL.ClearBuffer(ClearBuffer.Color, 1, ref clearId);
        //GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.ColorMask(true, true, true, true);
        GL.ClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
    }

    public void ReadPixels(ref byte data, Texture2D.TextureFormat format = Texture2D.TextureFormat.RGB8)
    {
        PixelFormat fmt;
        PixelType type;
        switch (format)
        {
            case Texture2D.TextureFormat.RGB8:
                fmt = PixelFormat.Rgb;
                type = PixelType.UnsignedByte;
                break;
            case Texture2D.TextureFormat.RGB16:
                fmt = PixelFormat.Rgb;
                type = PixelType.UnsignedShort;
                break;
            default:
                throw new Exception("Unsupported texture format");
        }
        
        // NOTE: _colorTex is blit to _presentTex when MakePresentable() is called
        GL.BindTexture(TextureTarget.Texture2D, _presentTex);
        GL.GetTexImage(TextureTarget.Texture2D, 0, fmt, type, ref data);
    }

    public void MakePresentable() {
        if(_multisample) {
            int dbuf = GL.GetInteger(GetPName.DrawFramebufferBinding);
            int rbuf = GL.GetInteger(GetPName.ReadFramebufferBinding);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _msPresentFBO);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
            GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, dbuf);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rbuf);
        }
    }

    public void BindForPostProcess() {
        if (_multisample) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _msPresentFBO);
        }
        else {
            BindForRender();
        }
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
