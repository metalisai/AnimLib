using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace AnimLib {
    public partial class OpenTKPlatform : GameWindow, IPlatform
    {
        public class AllocatedResources {
            public List<int> vaos = new List<int>();
            public List<int> buffers = new List<int>();
            public List<int> textures = new List<int>();
        }

        public event IPlatform.OnSizeChangedDelegate OnSizeChanged;
        public event IPlatform.OnDisplayChangedDelegate OnDisplayChanged;
        public event EventHandler OnLoaded;
        public event EventHandler<MouseButtonEventArgs> mouseDown;
        public event EventHandler<MouseButtonEventArgs> mouseUp;
        public event EventHandler<MouseMoveEventArgs> mouseMove;
        public event EventHandler<MouseWheelEventArgs> mouseScroll;
        public event EventHandler<KeyboardKeyEventArgs> PKeyDown;
        public event EventHandler<KeyboardKeyEventArgs> PKeyUp;
        public event EventHandler<KeyPressEventArgs> PKeyPress;
        public event EventHandler<OpenTK.Input.FileDropEventArgs> PFileDrop;
        public event EventHandler<FrameEventArgs> PRenderFrame;

        public int WinWidth { get { return Width; } }
        public int WinHeight { get { return Height; } }

        private int imguiVao, imguiVbo, imguiEbo;

        public int rectVao;

        private int _defaultProgram;
        private int _blitProgram, _imguiProgram;

        List<int> _programs = new List<int>();

        public int[] Programs {
            get {
                return _programs.ToArray();
            }
        }

        static DebugProc proc;

        public static ConcurrentBag<string> destroyedOwners = new ConcurrentBag<string>();
        public Dictionary<string, AllocatedResources> allocatedResources = new Dictionary<string, AllocatedResources>();

        public OpenTKPlatform(int width, int height) : base(width, height, new OpenTK.Graphics.GraphicsMode(), "Test", 0 , DisplayDevice.Default, 3, 3, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible) {
            Width = width;
            Height = height;

            FileDrop += (object sender, OpenTK.Input.FileDropEventArgs args) => {
                if(PFileDrop != null) {
                    PFileDrop(this, args);
                }
            };
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
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(ImDrawVert)), Marshal.OffsetOf<ImDrawVert>("pos"));
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, Marshal.SizeOf(typeof(ImDrawVert)), Marshal.OffsetOf<ImDrawVert>("col"));
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(ImDrawVert)), Marshal.OffsetOf<ImDrawVert>("uv"));
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

            CompileShaders();
            Debug.Log("Shader compilation complete");
            CreateMeshes();
            Debug.Log("Platform meshes created");
            SetupImgui();
            Debug.Log("ImGui resources created");

            string version = GL.GetString(StringName.Version);
            string shadingLang = GL.GetString(StringName.ShadingLanguageVersion);
            string rendererStr = GL.GetString(StringName.Renderer);
            Console.WriteLine(version);
            Console.WriteLine(shadingLang);

            this.KeyDown += (object sender, KeyboardKeyEventArgs args) => {
                if(PKeyDown != null) {
                    PKeyDown(this, args);
                }
            };
            this.KeyUp += (object sender, KeyboardKeyEventArgs args) => {
                if(PKeyUp != null) {
                    PKeyUp(this, args);
                }
            };
            this.KeyPress += (object sender, KeyPressEventArgs args) => {
                if(PKeyPress != null) {
                    PKeyPress(this, args);
                }
            };

            this.MouseDown += (object sender, MouseButtonEventArgs args) => {
                if(mouseDown != null) {
                    mouseDown(this, args);
                }
            };
            this.MouseUp += (object sender, MouseButtonEventArgs args) => {
                if(mouseUp != null) {
                    mouseUp(this, args);
                }
            };
            this.MouseMove += (object sender, MouseMoveEventArgs args) => {
                if(mouseMove != null) {
                    mouseMove(this, args);
                }
            };
            this.MouseWheel += (object sender, MouseWheelEventArgs args) => {
                if(mouseScroll != null) {
                    mouseScroll(this, args);
                }
            };            
            if(OnLoaded != null) {
                OnLoaded(this, null);
            }

            base.OnLoad(e);
        }

        public void LoadTexture(Texture2D tex2d) {
            PixelFormat fmt = default;
            PixelType typ = default;
            switch(tex2d.Format) {
                case Texture2D.TextureFormat.R8:
                    fmt = PixelFormat.Red;
                    typ = PixelType.UnsignedByte;
                    break;
                case Texture2D.TextureFormat.RGB8:
                    fmt = PixelFormat.Rgb;
                    typ = PixelType.UnsignedByte;
                    break;
                case Texture2D.TextureFormat.RGBA8:
                    fmt = PixelFormat.Rgba;
                    typ = PixelType.UnsignedByte;
                    break;
                case Texture2D.TextureFormat.ARGB8:
                    fmt = PixelFormat.Bgra;
                    typ = PixelType.UnsignedInt8888Reversed;
                    break;
                case Texture2D.TextureFormat.BGR8:
                    fmt = PixelFormat.Bgr;
                    typ = PixelType.UnsignedByte;
                    break;
                default:
                    throw new NotImplementedException();
            }
            var tex = GL.GenTexture();
            tex2d.GLHandle = tex;

            if(tex2d.ownerGuid != "") {
                AllocatedResources res;
                if(!allocatedResources.TryGetValue(tex2d.ownerGuid, out res)) {
                    res = new AllocatedResources();
                    allocatedResources.Add(tex2d.ownerGuid, res);
                }
                res.textures.Add(tex);
            }

            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, tex2d.Alignment);
            GL.TexImage2D(TextureTarget.Texture2D, 
                0, PixelInternalFormat.Rgba, 
                tex2d.Width, tex2d.Height, 0, 
                fmt, typ, 
                ref tex2d.RawData[0]
            );
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ArbTextureFilterAnisotropic.TextureMaxAnisotropy, 4.0f);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
        }
        
        protected override void OnRenderFrame(FrameEventArgs e) {
            // destory resources that are no longer needed
            if(destroyedOwners.Count > 0) {
                string owner;
                while(destroyedOwners.TryTake(out owner)) {
                    AllocatedResources res;
                    if(!allocatedResources.TryGetValue(owner, out res)) {
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
            
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }

        public void RenderImGui(ImDrawDataPtr data, Texture2D atlas, IList<SceneView> views, IRenderBuffer rb) {
            // create vertex and index buffers
            // each command list gets drawn on a seperate drawcall
            int vcount = 0;
            int icount = 0;
            for(int i = 0; i < data.CmdListsCount; i++) {
                int vertSize = Marshal.SizeOf(typeof(ImDrawVert));
                var cmdList = data.CmdListsRange[i];
                GL.BindBuffer(BufferTarget.ArrayBuffer, imguiVbo);
                GL.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(vcount*vertSize), cmdList.VtxBuffer.Size*vertSize, cmdList.VtxBuffer.Data);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, imguiEbo);
                GL.BufferSubData(BufferTarget.ElementArrayBuffer, new IntPtr(icount*sizeof(ushort)), cmdList.IdxBuffer.Size*sizeof(ushort), cmdList.IdxBuffer.Data);
                vcount += cmdList.VtxBuffer.Size;
                icount += cmdList.IdxBuffer.Size;
            }

            GL.BindVertexArray(imguiVao);

            uint vofst = 0;
            uint iofst = 0;

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
            if(atlas.GLHandle == -1) {
                LoadTexture(atlas);
                ImGui.GetIO().Fonts.SetTexID((IntPtr)atlas.GLHandle);
            }
            GL.Uniform1(texLoc, 0);

            for(int n = 0; n < data.CmdListsCount; n++) {
                ImDrawListPtr cmdList = data.CmdListsRange[n];
                for(int ci = 0; ci < cmdList.CmdBuffer.Size; ci++) {
                    var pcmd = cmdList.CmdBuffer[ci];
                    if(pcmd.UserCallback != IntPtr.Zero) {
                        throw new NotImplementedException();
                    } else {
                        GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                        if(views.Any(x => x.TextureHandle == (int)pcmd.TextureId)) {
                            GL.Uniform1(entIdLoc, -1);
                        } else {
                            GL.Uniform1(entIdLoc, 0xAAAAAAA);
                        }
                        GL.Scissor((int)pcmd.ClipRect.X, Height - (int)pcmd.ClipRect.W, (int)(pcmd.ClipRect.Z-pcmd.ClipRect.X), (int)(pcmd.ClipRect.W-pcmd.ClipRect.Y));
                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, new IntPtr(iofst*sizeof(ushort)), (int)vofst);
                    }
                    iofst += pcmd.ElemCount;
                }
                vofst += (uint)cmdList.VtxBuffer.Size;
            }

            GL.Disable(EnableCap.ScissorTest);
            //GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
        }

        public void RenderGUI((ImDrawDataPtr, Texture2D)? data, IList<SceneView> views, IRenderBuffer rb)
        {
            DepthPeelRenderBuffer pb;
            GL.Viewport(0, 0, rb.Size.Item1, rb.Size.Item2);
            pb = rb as DepthPeelRenderBuffer;

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

            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.Zero); 
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
            RenderImGui(data.Value.Item1, data.Value.Item2, views, rb);
            pb.NextLayer();
            if (data != null) pb.BlendToScreen(Width, Height, _blitProgram);
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

        public int AddShader(string v, string f, string g, string tcs = null, string tes = null) {
            Console.WriteLine("Compile shader!");
            int[] ps = new int[1];
            var vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, v);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, ps);
            if(ps[0] != 1) {
                int slen;
                string slog = new string('*', 256);
                GL.GetShaderInfoLog(vs, 256, out slen, out slog);
                Console.WriteLine("Failed to compile vertex shader!");
                Console.WriteLine("LOG: " + slog);
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
                Console.WriteLine("Failed to compile fragment shader!");
                Console.WriteLine("LOG: " + slog);
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
                    Console.WriteLine("Failed to compile geometry shader!");
                    Console.WriteLine(slog);
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
                Console.WriteLine("Failed to link program!");
                Console.WriteLine(slog);
            } else {
                Debug.Log("Program linked!");
            }
            _programs.Add(ret);
            // TODO: delete shaders?
            return ret;
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
        }
        
        private void CompileShaders() {
            _blitProgram = AddShader(blitVert, blitFrag, null);
            _defaultProgram = AddShader(vertShader, fragShader, null);
            _imguiProgram = AddShader(imguiVert, imguiFrag, null);
        }

        static void debugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
            if(type == DebugType.DebugTypeOther)
                return;
            Console.WriteLine($"OpenGL debug ({type}) src:{source} message: {Marshal.PtrToStringAnsi(message)}");
        }
    }

}
