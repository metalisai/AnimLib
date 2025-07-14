using System;
using System.Runtime.InteropServices;
using AnimLib;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.Egl;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace AnimLib;

internal class HeadlessGlPlatform : IRendererPlatform
{
    public class AllocatedResources
    {
        public List<int> vaos = new List<int>();
        public List<int> buffers = new List<int>();
        public List<int> textures = new List<int>();
    }

    static DebugProc proc = OpenTKPlatform.debugCallback;
    int blitSampler, mipmapSampler, linearSampler;
    int _blitProgram, _defaultProgram;
    public SkiaRenderer? SkiaRenderer;
    public Dictionary<string, AllocatedResources> allocatedResources = new();
    public static ConcurrentBag<string> destroyedOwners = new();
    List<int> _programs = new List<int>();

    public int rectVao;
    public int dynVao, dynVbo;
    public int blitvao = -1, blitvbo = -1;

    public class EGLBindingsContext : OpenTK.IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
        {
            return Egl.GetProcAddress(procName);
        }
    }

    public IEnumerable<int> Programs => _programs;

    public SkiaRenderer Skia => SkiaRenderer!;

    public int BlitVao => blitvao;

    [DllImport("libEGL.so")]
    private static extern IntPtr eglGetDisplay(IntPtr native_display);

    public HeadlessGlPlatform()
    {
        //var dsp = eglGetDisplay(IntPtr.Zero);
        //Debug.Log($"Display: {dsp}");
        IntPtr dpy = Egl.GetDisplay(IntPtr.Zero);
        Egl.Initialize(dpy, out var major, out var minor);
        Debug.Log($"EGL {major}.{minor}");
        nint[] configBuf = new nint[128];
        var success = Egl.ChooseConfig(dpy, [Egl.SURFACE_TYPE, Egl.PBUFFER_BIT, Egl.NONE], configBuf, configBuf.Length, out var num_config);
        Debug.Log($"EGL ChooseConfig success: {success}. Config count: {num_config} {string.Join(' ', configBuf.Select(x => x.ToString()))}");
        var surf = Egl.CreatePbufferSurface(dpy, configBuf.First(), [Egl.WIDTH, 1920, Egl.HEIGHT, 1080, Egl.VG_COLORSPACE, Egl.VG_COLORSPACE_sRGB, Egl.NONE]);
        var ecode = Egl.GetError();
        Debug.Log($"Surface: {surf} Error: {ecode}");
        Egl.BindAPI(RenderApi.GL);
        ecode = Egl.GetError();
        Debug.Log($"Egl.BindAPI: {ecode}");

        var ctx = Egl.CreateContext(dpy, configBuf[0], IntPtr.Zero, [Egl.CONTEXT_MAJOR_VERSION, 3, Egl.CONTEXT_MINOR_VERSION, 3, Egl.CONTEXT_OPENGL_DEBUG, 1, Egl.NONE]);
        ecode = Egl.GetError();
        Debug.Log($"Egl.CreateContext: {ecode}");

        Egl.MakeCurrent(dpy, surf, surf, ctx);
        ecode = Egl.GetError();
        Debug.Log($"Egl.MakeCurrent: {ecode}");

        OpenTK.Graphics.OpenGL4.GL.LoadBindings(new EGLBindingsContext());

        OnLoaded = (s, e) => { };
        PRenderFrame = (e) => { };

        Init();
    }

    public FrameColorSpace PresentedColorSpace => FrameColorSpace.Linear;

    public int BlitProgram => _blitProgram;

    public event EventHandler OnLoaded;
    public event Action<FrameEventArgs> PRenderFrame;

    public int AddShader(string v, string f, string? g, string? tcs = null, string? tes = null)
    {
        var ret = OpenTKPlatform.GlAddShader(v, f, g, tcs, tes);
        _programs.Add(ret);
        return ret;
    }

    public void DeleteShader(int shader) {
        GL.DeleteShader(shader);
        _programs.Remove(shader);
    }

    public void ClearBackbuffer(int x, int y, int w, int h)
    {
        OpenTKPlatform.GlClearBackbuffer(x, y, w, h);
    }

    public void DestroyOwner(string hash)
    {
        destroyedOwners.Add(hash);
    }

    public int GetSampler(PlatformTextureSampler sampler)
    {
        switch (sampler)
        {
            case PlatformTextureSampler.Mipmap:
                return mipmapSampler;
            case PlatformTextureSampler.Blit:
                return blitSampler;
            case PlatformTextureSampler.Linear:
                return linearSampler;
        }
        return -1;
    }

    public void LoadTexture(Texture2D tex2d)
    {
        int tex = tex2d.GLHandle;
        OpenTKPlatform.GlLoadTexture(tex2d);
        if (tex2d.ownerGuid != "" && tex < 0 && tex2d.GLHandle >= 0)
        {
            tex = tex2d.GLHandle;
            if (!allocatedResources.TryGetValue(tex2d.ownerGuid, out var res))
            {
                res = new AllocatedResources();
                allocatedResources.Add(tex2d.ownerGuid, res);
            }
            res.textures.Add(tex);
        }
    }

    private void CompileShaders()
    {
        _blitProgram = AddShader(OpenTKPlatform.blitVert, OpenTKPlatform.blitFrag, null);
        _defaultProgram = AddShader(OpenTKPlatform.vertShader, OpenTKPlatform.fragShader, null);
    }

    public void Load()
    {
        if (OnLoaded != null)
        {
            OnLoaded(this, new());
        }
    }

    protected void Init()
    {
        GL.DebugMessageCallback(proc, IntPtr.Zero);
        GL.Disable(EnableCap.Dither);
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(TriangleFace.Front);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Enable(EnableCap.DepthTest);

        GL.Enable(EnableCap.FramebufferSrgb);

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
        OpenTKPlatform.CreateMeshes(out rectVao, out dynVao, out dynVbo, out blitvao, out blitvbo);

        string version = GL.GetString(StringName.Version);
        string shadingLang = GL.GetString(StringName.ShadingLanguageVersion);
        string rendererStr = GL.GetString(StringName.Renderer);
        Debug.Log($"OpenGL context info\n\tGL version: {version}\n\tShading language version: {shadingLang}\n\tRenderer: {rendererStr}");

        // skia
        SkiaRenderer = new SkiaRenderer(this);
        SkiaRenderer.CreateSW(true);
    }

    public void RenderFrame(FrameEventArgs e)
    {
        OnRenderFrame(e);
    }
    
    protected void OnRenderFrame(FrameEventArgs e)
    {
        var sw = new System.Diagnostics.Stopwatch();
        // destory resources that are no longer needed
        sw.Restart();
        if (destroyedOwners.Count > 0)
        {
            while (destroyedOwners.TryTake(out var owner))
            {
                if (!allocatedResources.TryGetValue(owner, out var res))
                {
                    break;
                }
                else
                {
                    Debug.Log($"Renderer resource owner {owner} with resources destroyed");
                    foreach (var tx in res.textures)
                    {
                        GL.DeleteTexture(tx);
                    }
                    foreach (var vao in res.vaos)
                    {
                        GL.DeleteVertexArray(vao);
                    }
                    foreach (var buf in res.buffers)
                    {
                        GL.DeleteBuffer(buf);
                    }
                }
            }
        }

        if (PRenderFrame != null)
        {
            PRenderFrame(e);
        }
        sw.Stop();
        Performance.TimeToProcessFrame = sw.Elapsed.TotalSeconds;

        sw.Restart();
        GL.Flush();
        //SwapBuffers();
        sw.Stop();
        Performance.TimeToWaitSync = sw.Elapsed.TotalSeconds;
    }
}