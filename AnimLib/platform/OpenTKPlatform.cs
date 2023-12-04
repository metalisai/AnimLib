using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace AnimLib;

/// <summary>
/// A platform implementation using OpenTK.
/// </summary>
internal partial class OpenTKPlatform : GameWindow, IPlatform
{
    public class AllocatedResources {
        public List<int> vaos = new List<int>();
        public List<int> buffers = new List<int>();
        public List<int> textures = new List<int>();
    }

    public event IPlatform.OnSizeChangedDelegate? OnSizeChanged;
    public event IPlatform.OnDisplayChangedDelegate? OnDisplayChanged;
    public event EventHandler? OnLoaded;
    public event EventHandler<MouseButtonEventArgs>? mouseDown;
    public event EventHandler<MouseButtonEventArgs>? mouseUp;
    public event EventHandler<MouseMoveEventArgs>? mouseMove;
    public event EventHandler<MouseWheelEventArgs>? mouseScroll;
    public event EventHandler<KeyboardKeyEventArgs>? PKeyDown;
    public event EventHandler<KeyboardKeyEventArgs>? PKeyUp;
    public event EventHandler<KeyPressEventArgs>? PKeyPress;
    public event EventHandler<OpenTK.Input.FileDropEventArgs>? PFileDrop;
    public event EventHandler<FrameEventArgs>? PRenderFrame;

    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

    public int WinWidth { get { return Width; } }
    public int WinHeight { get { return Height; } }

    private int imguiVao, imguiVbo, imguiEbo;

    public SkiaRenderer? Skia;

    public int rectVao;
    public int dynVao, dynVbo;
    public int blitvao = -1, blitvbo = -1;

    int blitSampler, mipmapSampler, linearSampler;

    private int _defaultProgram;
    private int _blitProgram, _imguiProgram;

    List<int> _programs = new List<int>();

    public int BlitProgram {
        get { return _blitProgram; }
    }

    public int[] Programs {
        get {
            return _programs.ToArray();
        }
    }

    static DebugProc? proc;

    public static ConcurrentBag<string> destroyedOwners = new ConcurrentBag<string>();
    public Dictionary<string, AllocatedResources> allocatedResources = new Dictionary<string, AllocatedResources>();

    public OpenTKPlatform(int width, int height) : base(width, height, new OpenTK.Graphics.GraphicsMode(), "Test", 0 , DisplayDevice.Default, 3, 3, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible) {
        Width = width;
        Height = height;

        FileDrop += (object? sender, OpenTK.Input.FileDropEventArgs args) => {
            if(PFileDrop != null) {
                PFileDrop(this, args);
            }
        };

        this.VSync = VSyncMode.On;
        Context.SwapInterval = 1;
    }

    public int GetSampler(PlatformTextureSampler sampler) {
        switch(sampler) {
            case PlatformTextureSampler.Mipmap:
                return mipmapSampler;
            case PlatformTextureSampler.Blit:
                return blitSampler;
            case PlatformTextureSampler.Linear:
                return linearSampler;
        }
        return -1;
    }

    public void DestroyOwner(string hash) {
        destroyedOwners.Add(hash);
    }

    protected override void OnResize(EventArgs e) {
        if(OnSizeChanged != null) {
            OnSizeChanged(this.Width, this.Height);
        }
        if(OnDisplayChanged != null) {
            double maxrate = 0.0;
            foreach (DisplayIndex index in Enum.GetValues(typeof(DisplayIndex))) { 
                var dsp = DisplayDevice.GetDisplay(index);
                if (dsp != null) {
                    maxrate = Math.Max(dsp.RefreshRate, maxrate);
                }
            }
            if (maxrate == 0.0) maxrate = 60.0;
            OnDisplayChanged(this.Width, this.Height, maxrate);
        }
        base.OnResize(e);
    }

    protected void SetupImgui() {
        imguiVao = GL.GenVertexArray();
        GL.BindVertexArray(imguiVao);
        imguiVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, imguiVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 4*1024*1024, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        imguiEbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, imguiEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, 1024*1024, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(Imgui.ImDrawVert)), Marshal.OffsetOf<Imgui.ImDrawVert>("pos"));
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, Marshal.SizeOf(typeof(Imgui.ImDrawVert)), Marshal.OffsetOf<Imgui.ImDrawVert>("col"));
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(Imgui.ImDrawVert)), Marshal.OffsetOf<Imgui.ImDrawVert>("uv"));
        GL.BindVertexArray(0);
    }

    protected override void OnLoad(EventArgs e) {
        proc = new DebugProc(debugCallback);
        GL.DebugMessageCallback(proc, IntPtr.Zero);
        GL.Disable(EnableCap.Dither);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(CullFaceMode.Front);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Enable(EnableCap.DepthTest);

        mipmapSampler = GL.GenSampler();
        GL.SamplerParameter(mipmapSampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.SamplerParameter(mipmapSampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.SamplerParameter(mipmapSampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.SamplerParameter(mipmapSampler, SamplerParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        blitSampler = GL.GenSampler();
        GL.SamplerParameter(blitSampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.SamplerParameter(blitSampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.SamplerParameter(blitSampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.SamplerParameter(blitSampler, SamplerParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        linearSampler = GL.GenSampler();
        GL.SamplerParameter(linearSampler, SamplerParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.SamplerParameter(linearSampler, SamplerParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.SamplerParameter(linearSampler, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.SamplerParameter(linearSampler, SamplerParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

        CompileShaders();
        Debug.Log("Shader compilation complete");
        CreateMeshes();
        Debug.Log("Platform meshes created");
        SetupImgui();
        Debug.Log("ImGui resources created");

        string version = GL.GetString(StringName.Version);
        string shadingLang = GL.GetString(StringName.ShadingLanguageVersion);
        string rendererStr = GL.GetString(StringName.Renderer);
        Debug.Log($"OpenGL context info\n\tGL version: {version}\n\tShading language version: {shadingLang}\n\tRenderer: {rendererStr}");

        this.KeyDown += (object? sender, KeyboardKeyEventArgs args) => {
            if(PKeyDown != null) {
                PKeyDown(this, args);
            }
        };
        this.KeyUp += (object? sender, KeyboardKeyEventArgs args) => {
            if(PKeyUp != null) {
                PKeyUp(this, args);
            }
        };
        this.KeyPress += (object? sender, KeyPressEventArgs args) => {
            if(PKeyPress != null) {
                PKeyPress(this, args);
            }
        };

        this.MouseDown += (object? sender, MouseButtonEventArgs args) => {
            if(mouseDown != null) {
                mouseDown(this, args);
            }
        };
        this.MouseUp += (object? sender, MouseButtonEventArgs args) => {
            if(mouseUp != null) {
                mouseUp(this, args);
            }
        };
        this.MouseMove += (object? sender, MouseMoveEventArgs args) => {
            if(mouseMove != null) {
                mouseMove(this, args);
            }
        };
        this.MouseWheel += (object? sender, MouseWheelEventArgs args) => {
            if(mouseScroll != null) {
                mouseScroll(this, args);
            }
        };            

        // skia
        Skia = new SkiaRenderer(this);
        Skia.CreateGL(true); // messes up render state when scene has textures, arc rendering buggy
        //Skia.CreateSW(true); // HDR is very slow

        if(OnLoaded != null) {
            OnLoaded(this, new());
        }

        base.OnLoad(e);
    }

    public void LoadTexture(Texture2D tex2d) {
        PixelFormat fmt = default;
        PixelType typ = default;
        PixelInternalFormat pif = default;
        switch(tex2d.Format) {
            case Texture2D.TextureFormat.R8:
                fmt = PixelFormat.Red;
                typ = PixelType.UnsignedByte;
                pif = PixelInternalFormat.R8;
                break;
            case Texture2D.TextureFormat.RGB8:
                fmt = PixelFormat.Rgb;
                typ = PixelType.UnsignedByte;
                pif = PixelInternalFormat.Rgb8;
                break;
            case Texture2D.TextureFormat.RGBA8:
                fmt = PixelFormat.Rgba;
                typ = PixelType.UnsignedByte;
                pif = PixelInternalFormat.Rgba;
                break;
            case Texture2D.TextureFormat.ARGB8:
                fmt = PixelFormat.Bgra;
                typ = PixelType.UnsignedInt8888Reversed;
                pif = PixelInternalFormat.Rgba;
                break;
            case Texture2D.TextureFormat.BGR8:
                fmt = PixelFormat.Bgr;
                typ = PixelType.UnsignedByte;
                pif = PixelInternalFormat.Rgb8;
                break;
            case Texture2D.TextureFormat.RGBA16F:
                fmt = PixelFormat.Rgba;
                typ = PixelType.HalfFloat;
                pif = PixelInternalFormat.Rgba16f;
                break;
            default:
                throw new NotImplementedException();
        }
        int tex = tex2d.GLHandle;
        if(tex < 0) {
            tex = GL.GenTexture();
            tex2d.GLHandle = tex;
        }

        if(tex2d.ownerGuid != "") {
            if(!allocatedResources.TryGetValue(tex2d.ownerGuid, out var res)) {
                res = new AllocatedResources();
                allocatedResources.Add(tex2d.ownerGuid, res);
            }
            res.textures.Add(tex);
        }

        GL.BindTexture(TextureTarget.Texture2D, tex);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, tex2d.Alignment);
        GL.TexImage2D(TextureTarget.Texture2D, 
            0, pif, 
            tex2d.Width, tex2d.Height, 0, 
            fmt, typ, 
            ref tex2d.RawData[0]
        );

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        if(tex2d.GenerateMipmap) {
            Debug.TLog($"Generate mipmap for {tex}");
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
        //Debug.TLog($"Load texture {tex2d.Width}x{tex2d.Height} {Environment.StackTrace}");
    }

    protected override void OnUpdateFrame(FrameEventArgs e) {
    }

    protected override void OnRenderFrame(FrameEventArgs e) {
        base.OnRenderFrame(e);

        // destory resources that are no longer needed
        sw.Restart();
        if(destroyedOwners.Count > 0) {
            while(destroyedOwners.TryTake(out var owner)) {
                if(!allocatedResources.TryGetValue(owner, out var res)) {
                    break;
                } else {
                    Debug.Log($"Renderer resource owner {owner} with resources destroyed");
                    foreach(var tx in res.textures) {
                        GL.DeleteTexture(tx);
                    }
                    foreach(var vao in res.vaos) {
                        GL.DeleteVertexArray(vao);
                    }
                    foreach(var buf in res.buffers) {
                        GL.DeleteBuffer(buf);
                    }
                }
            }
        }

        if(PRenderFrame != null) {
            PRenderFrame(this, e);
        }
        sw.Stop();
        Performance.TimeToProcessFrame = sw.Elapsed.TotalSeconds;
        
        sw.Restart();
        GL.Flush();
        SwapBuffers();
        sw.Stop();
        Performance.TimeToWaitSync = sw.Elapsed.TotalSeconds;
    }

    public void RenderImGui(Imgui.DrawList data, IList<SceneView> views, IBackendRenderBuffer rb) {
        GL.BindBuffer(BufferTarget.ArrayBuffer, imguiVbo);
        if (data.vertices.Length > 0)
        {
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.vertices.Length*Marshal.SizeOf(typeof(Imgui.ImDrawVert)), ref data.vertices[0]);
        }
        else
        {
            Debug.Warning("No vertices to render");
        }
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, imguiEbo);
        if (data.indices.Length > 0)
        {
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, data.indices.Length*sizeof(ushort), ref data.indices[0]);
        }
        else
        {
            Debug.Warning("No indices to render");
        } 

        GL.BindVertexArray(imguiVao);
        GL.Enable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.UseProgram(_imguiProgram);
        var matLoc = GL.GetUniformLocation(_imguiProgram, "_ModelToClip");
        var mat = M4x4.Ortho(0.0f, rb.Size.Item1, 0.0f, rb.Size.Item2, -1.0f, 1.0f);
        GL.UniformMatrix4(matLoc, 1, false, ref mat.m11);
        var texLoc = GL.GetUniformLocation(_imguiProgram, "_AtlasTex");
        var entIdLoc = GL.GetUniformLocation(_imguiProgram, "_entityId");

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(texLoc, 0);

        foreach (var dc in data.commands)
        {
            GL.BindTexture(TextureTarget.Texture2D, (int)dc.texture);
            int haveMipmap = 0;
            if(haveMipmap > 0 && (int)dc.texture>= 0) {
                GL.BindSampler(0, mipmapSampler);
            } else {
                GL.BindSampler(0, linearSampler);
            }
            // rendering a view within gui
            if(views.Any(x => x.TextureHandle == (int)dc.texture)) {
                GL.Uniform1(entIdLoc, -1);
                GL.Disable(EnableCap.Blend);
            } else { // rendering gui
                GL.Uniform1(entIdLoc, 0xAAAAAAA);
            }
            GL.Scissor((int)dc.clipRect.Item1, Height - (int)dc.clipRect.Item4, (int)(dc.clipRect.Item3-dc.clipRect.Item1), (int)(dc.clipRect.Item4-dc.clipRect.Item2));
            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)dc.elemCount, DrawElementsType.UnsignedShort, new IntPtr(dc.idxOffset*sizeof(ushort)), (int)dc.vOffset);
            GL.Enable(EnableCap.Blend);
        }
        GL.BindSampler(0, 0);
        GL.Disable(EnableCap.ScissorTest);
        //GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        return;
    }

    public void RenderGUI(Imgui.DrawList data, IList<SceneView> views, IBackendRenderBuffer rb)
    {
        GL.Viewport(0, 0, rb.Size.Item1, rb.Size.Item2);
        var pb = rb as DepthPeelRenderBuffer;

        GL.Enable(EnableCap.PolygonOffsetFill);
        System.Diagnostics.Debug.Assert(pb is DepthPeelRenderBuffer);

        pb.Bind();
        GL.DepthMask(true);
        GL.ClearDepth(1.0f);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        // this makes sure the other buffer is clear! (for first pass)
        pb.NextLayer();
        GL.ClearDepth(0.0f);
        GL.DepthFunc(DepthFunction.Greater);
        GL.Enable(EnableCap.DepthTest);

        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

        var bufs = new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1};
        GL.DrawBuffers(2, bufs);

        GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        //GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.Zero, BlendingFactorSrc.One, BlendingFactorDest.Zero); 
        GL.Enable(EnableCap.Blend);

        var smat = M4x4.Ortho(0.0f, pb.Size.Item1, 0.0f, pb.Size.Item2, -1.0f, 1.0f);
        GL.ColorMask(true, true, true, true);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        GL.Clear(ClearBufferMask.DepthBufferBit/* | ClearBufferMask.ColorBufferBit*/);
        foreach(var prog in _programs) {
            var loc = GL.GetUniformLocation(prog, "_depthPeelTex");
            GL.UseProgram(prog);
            GL.ProgramUniform1(prog, loc, 1);
            GL.BindTextureUnit(1, pb.PeelTex);
        }
        RenderImGui(data, views, rb);
        pb.NextLayer();
        if (data != null) pb.BlendToScreen(Width, Height);
    }

    public void ClearBackbuffer(int x, int y, int w, int h) {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.DrawBuffer(DrawBufferMode.Back);
        GL.Viewport(x, y, w, h);
        GL.ColorMask(true, true, true, true);
        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.ClearDepth(1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void DeleteShader(int shader) {
        GL.DeleteShader(shader);
    }

    /// <summary>
    /// Add a shader program.
    /// </summary>
    /// <param name="v">Vertex shader source.</param>
    /// <param name="f">Fragment shader source.</param>
    /// <param name="g">Geometry shader source.</param>
    /// <param name="tcs">Tessellation control shader source.</param>
    /// <param name="tes">Tessellation evaluation shader source.</param>
    public int AddShader(string v, string f, string? g, string? tcs = null, string? tes = null) {
        int[] ps = new int[1];
        var vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, v);
        GL.CompileShader(vs);
        GL.GetShader(vs, ShaderParameter.CompileStatus, ps);
        if(ps[0] != 1) {
            int slen;
            string slog = new string('*', 256);
            GL.GetShaderInfoLog(vs, 256, out slen, out slog);
            Debug.Error($"Failed to compile vertex shader!\n LOG: {slog}");
            throw new Exception();
        }

        var fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, f);
        GL.CompileShader(fs);
        GL.GetShader(fs, ShaderParameter.CompileStatus, ps);
        if(ps[0] != 1) {
            int slen;
            string slog = new string('*', 256);
            GL.GetShaderInfoLog(fs, 256, out slen, out slog);
            Debug.Error($"Failed to compile fragment shader!\n LOG: {slog}");
            throw new Exception();
        }

        var ret = GL.CreateProgram();
        GL.AttachShader(ret, vs);
        GL.AttachShader(ret, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        if(g != null) {
            var gs = GL.CreateShader(ShaderType.GeometryShader);
            GL.ShaderSource(gs, g);
            GL.CompileShader(gs);
            GL.GetShader(gs, ShaderParameter.CompileStatus, ps);
            if(ps[0] != 1) {
                int slen;
                string slog = "";
                GL.GetShaderInfoLog(gs, 256, out slen, out slog);
                Debug.Error($"Failed to compile geometry shader!\n LOG: {slog}");
            }
            GL.AttachShader(ret, gs);
            GL.DeleteShader(gs);
        }

        if((tcs == null) != (tes == null)) {
            Debug.Error("To use tessellation, both TCS and TES need to be set!");
        }
        if(tcs != null) {
            var tcss = GL.CreateShader(ShaderType.TessControlShader); 
            GL.ShaderSource(tcss, tcs);
            GL.CompileShader(tcss);
            GL.GetShader(tcss, ShaderParameter.CompileStatus, ps);
            if(ps[0] != 1) {
                int slen;
                string slog = "";
                GL.GetShaderInfoLog(tcss, 256, out slen, out slog);
                Debug.Error($"Failed to compile TCS shader! Log: {slog}");
            }
            var tess = GL.CreateShader(ShaderType.TessEvaluationShader);
            GL.ShaderSource(tess, tes);
            GL.CompileShader(tess);
            GL.GetShader(tess, ShaderParameter.CompileStatus, ps);
            if(ps[0] != 1) {
                int slen;
                string slog = "";
                GL.GetShaderInfoLog(tess, 256, out slen, out slog);
                Debug.Error($"Failed to compile TES shader! Log: {slog}");
            }
            GL.AttachShader(ret, tcss);
            GL.AttachShader(ret, tess);
            GL.DeleteShader(tcss);
            GL.DeleteShader(tess);
        }

        GL.LinkProgram(ret);
        GL.GetProgram(ret, GetProgramParameterName.LinkStatus, ps);
        if(ps[0] != 1) {
            int slen;
            string slog = "";
            GL.GetProgramInfoLog(ret, 256, out slen, out slog);
            Debug.Error($"Failed to link program! LOG: {slog}");
        } else {
            Debug.Log("Program linked!");
        }
        _programs.Add(ret);
        // TODO: delete shaders?
        return ret;
    }

    public void DynRect(Vector2 bl, Vector2 tr) {
        //Debug.Log($"{bl.ToString()} {tr.ToString()}");
        var vData = new float[] {
            bl.x, bl.y, 0.0f, 1.0f,
            tr.x, bl.y, 0.0f, 1.0f,
            bl.x, tr.y, 0.0f, 1.0f,
            tr.x, bl.y, 0.0f, 1.0f,
            tr.x, tr.y, 0.0f, 1.0f,
            bl.x, tr.y, 0.0f, 1.0f,

            0.0f,  0.0f,
            1.0f,  0.0f,
            0.0f,  1.0f,
            1.0f,  0.0f,
            1.0f,  1.0f,
            0.0f,  1.0f,
        };
        GL.BindBuffer(BufferTarget.ArrayBuffer, dynVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vData.Length * sizeof(float), vData, BufferUsageHint.DynamicDraw);
    }

    private void CreateMeshes() {
        var vData = new float[] {
           -0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, 0.0f, 1.0f,
            -0.5f, 0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, 0.0f, 1.0f,
            0.5f,  0.5f, 0.0f, 1.0f,
            -0.5f, 0.5f, 0.0f, 1.0f,

            0.0f,  0.0f,
            1.0f,  0.0f,
            0.0f,  1.0f,
            1.0f,  0.0f,
            1.0f,  1.0f,
            0.0f,  1.0f,
        };
        rectVao = GL.GenVertexArray();
        GL.BindVertexArray(rectVao);
        var vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vData.Length * sizeof(float), vData, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 6*4*4);
        GL.BindVertexArray(0);

        dynVao = GL.GenVertexArray();
        GL.BindVertexArray(dynVao);
        dynVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, dynVbo);
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 6*4*4);
        GL.BindVertexArray(0);

        // blit VAO (used to blit textures)
        // blit VAO hasn't been created yet
        if(blitvao == -1) {
            blitvao = GL.GenVertexArray();
            GL.BindVertexArray(blitvao);
            blitvbo = GL.GenBuffer();
            float[] quad = new float[] {
                -1.0f, -1.0f, 0.0f, 1.0f,
                1.0f, -1.0f, 0.0f, 1.0f,
                -1.0f,  1.0f, 0.0f, 1.0f,
                1.0f, -1.0f, 0.0f, 1.0f,
                1.0f,  1.0f, 0.0f, 1.0f,
                -1.0f,  1.0f, 0.0f, 1.0f,
            };
            GL.BindBuffer(BufferTarget.ArrayBuffer, blitvbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);
            GL.BindVertexArray(0);
        }
    }
    
    private void CompileShaders() {
        _blitProgram = AddShader(blitVert, blitFrag, null);
        _defaultProgram = AddShader(vertShader, fragShader, null);
        _imguiProgram = AddShader(imguiVert, imguiFrag, null);
    }

    static void debugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
        if(type == DebugType.DebugTypeOther)
            return;
        Debug.Warning($"OpenGL debug ({type}) src:{source} message: {Marshal.PtrToStringAnsi(message)}");
    }

    protected override void Dispose(bool dispose) {
        // TODO: a lot of stuf needs to be disposed here
        if (!IsDisposed) {
            GL.DeleteVertexArray(blitvao);
            GL.DeleteBuffer(blitvbo);
            blitvao = -1;
            blitvbo = -1;
        }
        base.Dispose(dispose);
    }
}
