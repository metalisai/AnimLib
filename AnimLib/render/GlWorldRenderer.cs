using System;
using System.Runtime.InteropServices;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// <c>IRenderer</c> implementation using OpenGL and SkiaSharp. A renderer is responsible for rendering <c>WorldSnapshot</c> for a specified <c>SceneView</c> using appropriate coordinate transformations.
/// </summary>
internal partial class GlWorldRenderer : IRenderer {
    
    int _circleProgram;
    int _rectangleProgram;
    int _bezierProgram;
    int _texRectProgram;
    int _staticLineProgram;
    int _arrowProgram;
    int _cubeProgram;
    int _meshProgram;
    int _textProgram;

    int drawId = 0;

    private OpenTKPlatform platform;
    private RenderState rs;

    private Dictionary<int, IBackendRenderBuffer> renderBuffers = new();

    bool gizmo = true;

    System.Diagnostics.Stopwatch sw;
    EffectBuffer effectBuffer;
    GlKawaseBlur kawaseBlur;

    private struct FrameContext {
        public required EntityStateResolver entRes;
    }

    public GlWorldRenderer(OpenTKPlatform platform, RenderState rs) {
        this.platform = platform;
        this.rs = rs;
        _circleProgram = platform.AddShader(circleVert, circleFrag, null);
        _bezierProgram = platform.AddShader(quadBezierVert, quadBezierFrag, quadBezierGeo);
        _rectangleProgram = platform.AddShader(rectangleVert, rectangleFrag, null);
        _texRectProgram = platform.AddShader(rectangleVert, texRectFrag, null);
        _arrowProgram = platform.AddShader(rectangleVert, arrowFrag, null);
        _staticLineProgram = platform.AddShader(staticLineVert, staticLineFrag, null);
        _cubeProgram = platform.AddShader(staticLineVert, cubeFrag, null);
        _meshProgram = platform.AddShader(vertShader, meshFrag, meshGeom);
        //_wireTriangleProgram = AddShader(wireTriangleVert, wireTriangleFrag, null);
        //_lineProgram = AddShader(lineVert, lineFrag, lineGeom);
        _textProgram = platform.AddShader(textVert, textFrag, null);

        // TODO: dispose
        effectBuffer = new EffectBuffer(platform);
        kawaseBlur = new GlKawaseBlur(platform);

        sw = new System.Diagnostics.Stopwatch();
    }

    public void RenderCanvases(IBackendRenderBuffer buffer, CanvasSnapshot[] canvases, M4x4 mat) {
        using var _ = new Performance.Call("WorldRenderer.RenderCanvases");
        if (platform.Skia == null) {
            return;
        }
        // save OpenGL state (Skia might modify it in HW rendering mode)
        PushState();
        // draw 2D last so that they are always in front
        var sorted = canvases.OrderBy(x => x.Canvas.is2d ? 1 : 0);
        foreach(var canvas in sorted) {
            var buf = (DepthPeelRenderBuffer)buffer;
            platform.Skia.RenderCanvas(canvas, ref mat, this.gizmo, buf);
            RestoreState();
            {
                using var __ = new Performance.Call("WorldRenderer buf.BlitTextureWithEntityId");
                buf.BlitTextureWithEntityId(platform.Skia.Texture, canvas.Canvas.entityId);
            }
        }
    }

    private void RenderBeziers(BezierState[] beziers, M4x4 mat, M4x4 orthoMat, IBackendRenderBuffer buf, in FrameContext ctx) {
        if(beziers.Length > 0) {
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.UseProgram(_bezierProgram);
            var loc = GL.GetUniformLocation(_bezierProgram, "_ModelToClip");
            var colLoc = GL.GetUniformLocation(_bezierProgram, "_Color");
            var idLoc = GL.GetUniformLocation(_bezierProgram, "_EntityId");
            var p1Loc = GL.GetUniformLocation(_bezierProgram, "_Point1");
            var p2Loc = GL.GetUniformLocation(_bezierProgram, "_Point2");
            var p3Loc = GL.GetUniformLocation(_bezierProgram, "_Point3");
            var ssLoc = GL.GetUniformLocation(_bezierProgram, "_ScreenSize");
            var wLog = GL.GetUniformLocation(_bezierProgram, "_Width");
            GL.BindVertexArray(platform.rectVao);
            foreach(BezierState bz in beziers) {
                // NOTE: bz.points causes nullable warning in sdk 8.0.100, seems like a compiler bug?
                Vector3[] points = bz.points!;
                int count = 1 + (bz.points!.Length-3)/2;
                M4x4 modelToWorld, modelToClip;
                if(bz.points == null || bz.points.Length < 3)
                    continue;
                modelToWorld = bz.ModelToWorld(ctx.entRes);
                modelToClip = mat * modelToWorld;
                GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                var col4 = Vector4.FromInt32(bz.color.ToU32());
                GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                GL.Uniform1(idLoc, bz.entityId);
                GL.Uniform1(wLog, bz.width);
                for(int i = 0; i < count; i++) {
                    int idx = i*2;
                    GL.Uniform4(p1Loc, bz.points[idx].x, bz.points[idx].y, bz.points[idx].z, 1.0f);
                    GL.Uniform4(p2Loc, bz.points[idx+1].x, bz.points[idx+1].y, bz.points[idx+1].z, 1.0f);
                    GL.Uniform4(p3Loc, bz.points[idx+2].x, bz.points[idx+2].y, bz.points[idx+2].z, 1.0f);
                    var size = buf.Size;
                    GL.Uniform2(ssLoc, size.Item1, size.Item2);
                    GL.DrawArrays(PrimitiveType.Points, 0, 1);
                    drawId++;
                }
            }
        }
    }

    public void RenderMeshes(ColoredTriangleMesh[] meshes, M4x4 camMat, M4x4 screenMat) {
        var colorSize = Marshal.SizeOf(typeof(Color));
        var vertSize = Marshal.SizeOf(typeof(Vector3));
        var edgeSize = Marshal.SizeOf(typeof(Vector2));
        using var _ = new Performance.Call("WorldRenderer.RenderMeshes");
        if(meshes.Length > 0) {
            // TODO: winding order is wrong?
            GL.Disable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.Enable(EnableCap.DepthTest);

            foreach(var m in meshes) {
                var program = GetProgram(m.Shader);
                GL.UseProgram(program);
                var loc = GL.GetUniformLocation(program, "_ModelToClip");
                var colLoc = GL.GetUniformLocation(program, "_Color");
                var outlineLoc = GL.GetUniformLocation(program, "_Outline");
                var entLoc = GL.GetUniformLocation(program, "_EntityId");

                foreach(var prop in m.shaderProperties) {
                    var sloc = GL.GetUniformLocation(program, prop.Item1);
                    switch(prop.Item2){
                        case float props:
                            GL.Uniform1(sloc, props);
                        break;
                        case Func<float> propsf:
                            GL.Uniform1(sloc, propsf());
                        break;
                        case Func<Vector4> propsv4:
                            var v = propsv4();
                            GL.Uniform4(sloc, v.x, v.y, v.z, v.w);
                        break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                if(m.Geometry.indices.Length == 0)
                    continue;
                if(m.Geometry.Dirty) {
                    int vao;
                    int vertCount = m.Geometry.vertices.Length;
                    if(m.Geometry.VAOHandle < 0) {
                        vao = GL.GenVertexArray();
                        int vbo = GL.GenBuffer();
                        int ebo = GL.GenBuffer();
                        GL.BindVertexArray(vao);
                        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
                        GL.EnableVertexAttribArray(0);
                        GL.EnableVertexAttribArray(1);
                        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertSize, 0);
                        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, colorSize, new IntPtr(vertCount*vertSize));
#if DEBUG
                        if ((new Color()).r.GetType() != typeof(float)) {
                            throw new Exception("Color.r is not float, VertexAttribPointer expects float!");
                        }
#endif
                        if(m.Geometry.edgeCoordinates != null && m.Geometry.edgeCoordinates.Length > 0) {
                            GL.EnableVertexAttribArray(2);
                            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, new IntPtr(vertCount*(vertSize+colorSize)));
                        }
                        m.Geometry.VAOHandle = vao;
                        m.Geometry.VBOHandle = vbo;
                        m.Geometry.EBOHandle = ebo;
                        // register owner for deletion (if owner gets destroyed)
                        // (we created a new buffer, but we don't know the lifetime of it)
                        var irr = (IRendererResource)m.Geometry;
                        var ar = RenderState.currentPlatform as OpenTKPlatform;
                        if(irr.GetOwnerGuid() != "" && ar != null) {
                            if(!ar.allocatedResources.TryGetValue(irr.GetOwnerGuid(), out var res)) {
                                res = new OpenTKPlatform.AllocatedResources();
                                ar.allocatedResources.Add(irr.GetOwnerGuid(), res);
                            }
                            res.buffers.Add(vbo); res.buffers.Add(ebo);
                            res.vaos.Add(vao);
                        }
                    } else {
                        vao = m.Geometry.VAOHandle;    
                        GL.BindVertexArray(vao);
                    }

                    IntPtr colorOffset = new IntPtr(vertCount*vertSize);
                    IntPtr edgeOffset = new IntPtr(vertCount*(vertSize+colorSize));

                    GL.BindBuffer(BufferTarget.ArrayBuffer, m.Geometry.VBOHandle);
                    GL.BufferData(BufferTarget.ArrayBuffer, vertCount*(vertSize+colorSize+edgeSize), IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertCount*vertSize, ref m.Geometry.vertices[0].x);
                    GL.BufferSubData(BufferTarget.ArrayBuffer, colorOffset, vertCount*colorSize, ref m.Geometry.colors[0]);
                    if(m.Geometry.edgeCoordinates != null && m.Geometry.edgeCoordinates.Length > 0) {
                        GL.BufferSubData(BufferTarget.ArrayBuffer, edgeOffset, vertCount*edgeSize, ref m.Geometry.edgeCoordinates[0]);
                    }
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, m.Geometry.EBOHandle);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, m.Geometry.indices.Length*4, ref m.Geometry.indices[0], BufferUsageHint.DynamicDraw);
                    GL.BindVertexArray(0);
                    m.Geometry.Dirty = false;
                }
                GL.BindVertexArray(m.Geometry.VAOHandle);
                M4x4 modelToClip;
                modelToClip = !m.is2d ? camMat*m.modelToWorld : screenMat;
                GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                var col4 = m.Tint.ToVector4();
                GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                var outline4 = m.Outline.ToVector4();
                GL.Uniform4(outlineLoc, outline4.x, outline4.y, outline4.z, outline4.w);
                GL.Uniform1(entLoc, m.entityId);
                GL.DrawElements(PrimitiveType.Triangles, m.Geometry.indices.Length, DrawElementsType.UnsignedInt, 0);
                drawId++;
            }
        }
    }

    private void RenderGlyphs(Vector2 screenSize, GlyphState[] gs, CameraState worldCamera, in FrameContext ctx) {
        var mat = M4x4.Ortho(0.0f, screenSize.x, 0.0f, screenSize.y, -1.0f, 1.0f);
        foreach(var ch in gs) {
            var modelToWorld = ch.ModelToWorld(ctx.entRes);
            //Vector2 anchorPos = ch.anchor * screenSize;
            Vector3 pos = new Vector3(modelToWorld.m14, modelToWorld.m24, modelToWorld.m34);
            //pos += (Vector3)anchorPos;
            rs.FontCache.PushCharacter(ch, pos);
        }
        rs.FontCache.RenderTest(_textProgram, mat);
        drawId++;
    }

    public int GetProgram(BuiltinShader shader) {
        switch(shader) {
            case BuiltinShader.LineShader:
                return _staticLineProgram;
            case BuiltinShader.ArrowShader:
                return _arrowProgram;
            case BuiltinShader.CubeShader:
                return _cubeProgram;
            case BuiltinShader.MeshShader:
                return _meshProgram;
            default:
                return 0;
        }
    }

    struct SavedState {
        public bool blend;
        public bool programPointSize;
        public int boundVao;
        public bool cullFace;
        public int cullMode;
        public bool fbSrgb;
        public int textureUnit;
        public int unpackAlignment;
        public int boundFb;
        public int boundReadFb;
        public int boundDrawFb;
        public int drawBuffer;
        public bool dither;
        public bool depthMask;
        public bool multisample;
        public bool depthTest;
        public bool stencilTest;
        public int vpX;
        public int vpY;
        public int vpW;
        public int vpH;
    }

    SavedState state;

    public void PushState() {
        var state = new SavedState();
        state.blend = GL.GetBoolean(GetPName.Blend); // GL.Enable(EnableCap.Blend);
        state.programPointSize = GL.GetBoolean(GetPName.ProgramPointSize); // GL.Enable(EnableCap.ProgramPointSize)
        state.boundVao = GL.GetInteger(GetPName.VertexArrayBinding); // GL.BindVertexArray
        state.cullFace = GL.GetBoolean(GetPName.CullFace); // GL.Enable
        state.cullMode = GL.GetInteger(GetPName.CullFaceMode); // GL.CullFace
        state.fbSrgb = GL.GetBoolean(GetPName.FramebufferSrgb); // GL.Enable
        state.textureUnit = GL.GetInteger(GetPName.ActiveTexture); // GL.ActiveTexture
        state.unpackAlignment = GL.GetInteger(GetPName.UnpackAlignment); // GL.PixelStore
        state.boundReadFb = GL.GetInteger(GetPName.ReadFramebufferBinding); // GL.BindFramebuffer
        state.boundDrawFb = GL.GetInteger(GetPName.DrawFramebufferBinding);
        state.boundFb = GL.GetInteger(GetPName.FramebufferBinding); // GL.BindFramebuffer
        state.drawBuffer = GL.GetInteger(GetPName.DrawBuffer); // GL.DrawBuffer
        state.dither = GL.GetBoolean(GetPName.Dither); // GL.Enable
        state.depthMask = GL.GetBoolean(GetPName.DepthWritemask); // GL.DepthMask
        state.multisample = GL.GetBoolean(GetPName.Multisample);
        state.depthTest = GL.GetBoolean(GetPName.DepthTest);
        state.stencilTest = GL.GetBoolean(GetPName.StencilTest);
        var data = new int[4];
        GL.GetInteger(GetPName.Viewport, data);
        state.vpX = data[0]; state.vpY = data[1]; state.vpW = data[2]; state.vpH = data[3];
        GL.BindVertexArray(0);
        this.state = state;
    }

    public void RestoreState() {
        if(state.blend) {
            GL.Enable(EnableCap.Blend);
        } else {
            GL.Disable(EnableCap.Blend);
        }
        if(state.programPointSize) {
            GL.Enable(EnableCap.ProgramPointSize);
        } else {
            GL.Disable(EnableCap.ProgramPointSize);
        }
        GL.BindVertexArray(state.boundVao);
        if(state.cullFace) {
            GL.Enable(EnableCap.CullFace);
        } else {
            GL.Disable(EnableCap.CullFace);
        }
        GL.CullFace((CullFaceMode)state.cullMode);
        if(state.fbSrgb) {
            GL.Enable(EnableCap.FramebufferSrgb);
        } else {
            GL.Disable(EnableCap.FramebufferSrgb);
        }
        if(state.stencilTest) {
            GL.Enable(EnableCap.StencilTest);
        } else {
            GL.Disable(EnableCap.StencilTest);
        }
        GL.ActiveTexture((TextureUnit)state.textureUnit);
        //GL.PixelStore(PixelStoreParameter.UnpackAlignment, state.unpackAlignment);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, state.boundFb);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, state.boundReadFb);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, state.boundDrawFb);
        GL.DrawBuffer((DrawBufferMode)state.drawBuffer);
        if(state.dither) {
            GL.Enable(EnableCap.Dither);
        } else {
            GL.Disable(EnableCap.Dither);
        }
        GL.DepthMask(state.depthMask);
        if(state.multisample) {
            GL.Enable(EnableCap.Multisample);
        } else {
            GL.Disable(EnableCap.Multisample);
        }
        if(state.depthTest) {
            GL.Enable(EnableCap.DepthTest);
        } else {
            GL.Disable(EnableCap.DepthTest);
        }

        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.UseProgram(0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.Disable(EnableCap.ScissorTest);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.PolygonOffsetFill);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
        GL.PixelStore(PixelStoreParameter.PackAlignment, 4);
        GL.PixelStore(PixelStoreParameter.PackRowLength, 0);
        GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
        GL.Viewport(state.vpX, state.vpY, state.vpW, state.vpH);
    }

    public bool BufferValid(IBackendRenderBuffer buf) {
        return buf is DepthPeelRenderBuffer;
    }

    public IBackendRenderBuffer CreateBuffer(int w, int h, int id) {
        var buf = new DepthPeelRenderBuffer(platform, platform.PresentedColorSpace, true);
        buf.Resize(w, h);
        effectBuffer.Resize(w, h);
        // TODO: this is wrong!
        platform.Skia?.SetBuffer(buf);
        Debug.TLog($"Created new DepthPeelRenderBuffer with size {w}x{h}");

        renderBuffers.Add(id, buf);
        return buf;
    }

    public void RenderScene(WorldSnapshot ss, CameraState cam, bool gizmo, out IBackendRenderBuffer mainBuffer) {
        using var _ = new Performance.Call("WorldRenderer.RenderScene");

        this.gizmo = gizmo;
        long passedcount = 0;

        var ctx = new FrameContext {
            entRes = ss.resolver
        };

        var rb0 = ss.RenderBuffers[0];
        var w = rb0.Width;
        var h = rb0.Height;

        {
            using var ___ = new Performance.Call("Manage renderer buffers");
            foreach (var rb in ss.RenderBuffers) {
                if (!this.renderBuffers.TryGetValue(rb.BackendHandle, out var createdRb)
                    || createdRb.Size.w != rb.Width
                    || createdRb.Size.h != rb.Height)
                {
                    if (createdRb != null)
                    {
                        createdRb.Dispose();
                    }
                    createdRb = CreateBuffer(rb.Width, rb.Height, rb.BackendHandle);
                    this.renderBuffers[rb.BackendHandle] = createdRb;
                    Debug.Log($"Created new renderbuffer. Id: {rb.BackendHandle}, Size: {rb.Width}x{rb.Height}, Type: {createdRb.GetType()}, texId {createdRb.Texture()}");
                }
                this.renderBuffers[rb.BackendHandle].Clear();
                this.renderBuffers[rb.BackendHandle].OnPreRender();
            }
        }

        mainBuffer = this.renderBuffers[rb0.BackendHandle];
        var pb = mainBuffer as DepthPeelRenderBuffer;
        if (pb == null) {
            Console.WriteLine("Can't render scene because renderbuffer isn't DepthPeelRenderBuffer");
            return;
        }
        GL.Viewport(0, 0, mainBuffer.Size.w, mainBuffer.Size.h);
        GL.Enable(EnableCap.PolygonOffsetFill);

        // combination of depth peeling and multisampling requires sample shading
        if (pb.IsMultisampled) {
            GL.Enable(EnableCap.SampleShading);
            GL.MinSampleShading(1.0f);
        } else {
            GL.Disable(EnableCap.SampleShading);
        }

        mainBuffer.BindForRender();
        GL.DepthMask(true);
        GL.ClearDepth(1.0f);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        // this makes sure the other buffer is clear! (for first pass)
        pb.NextLayer();
        GL.ClearDepth(0.0f);
        GL.DepthFunc(DepthFunction.Greater);
        GL.Enable(EnableCap.DepthTest);

        var background = ss.Camera?.clearColor ?? Color.WHITE;
        var col = background.ToVector4();
        GL.ClearColor(col.x, col.y, col.z, col.w);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

        var bufs = new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2};
        GL.DrawBuffers(2, bufs);

        GL.BlendEquation(BlendEquationMode.FuncAdd);
        //GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        //GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.Zero); 
        GL.Enable(EnableCap.Blend);

        var smat = M4x4.Ortho(0.0f, mainBuffer.Size.w, 0.0f, mainBuffer.Size.h, -1.0f, 1.0f);
        var query = GL.GenQuery();

        var _programs = platform.Programs;

        // TODO: bind framebuffer when we have render targets
        var pbSize = mainBuffer.Size;
        if(cam is OrthoCameraState ocam) {
            ocam.width = pbSize.w;
            ocam.height = pbSize.h;
        }

        M4x4 worldToClip = cam.CreateWorldToClipMatrix((float)pbSize.w/(float)pbSize.h);
        int p = 0;
        for(p = 0; p < 16; p++) {
            drawId = 0;
            GL.ColorMask(true, true, true, true);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.DepthBufferBit/* | ClearBufferMask.ColorBufferBit*/);
            foreach(var prog in _programs) {
                var loc = GL.GetUniformLocation(prog, "_depthPeelTex");
                GL.UseProgram(prog);
                GL.ProgramUniform1(prog, loc, 1);
                GL.BindTextureUnit(1, pb.PeelTex);
                GL.BindSampler(1, platform.GetSampler(PlatformTextureSampler.Blit));
            }
            GL.BeginQuery(QueryTarget.SamplesPassed, query);
            //GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            if(ss.MeshBackedGeometries != null && ss.MeshBackedGeometries.Length > 0) {
                int i = 0;
                ColoredTriangleMesh[] meshes = new ColoredTriangleMesh[ss.MeshBackedGeometries.Length];
                var throwawayGeometries = new List<ColoredTriangleMeshGeometry>();
                foreach(var mbg in ss.MeshBackedGeometries) {
                    ColoredTriangleMeshGeometry? geom = null;
                    if(mbg.RendererHandle == null) {
                        // TODO: find a way to reuse these, this is nasty
                        geom = new ColoredTriangleMeshGeometry("");
                        throwawayGeometries.Add(geom);
                    }
                    else if(mbg.RendererHandle.Handle == null) {
                        mbg.RendererHandle.Handle = new ColoredTriangleMeshGeometry("");
                        geom = mbg.RendererHandle.Handle;
                    } else {
                        geom = mbg.RendererHandle.Handle;
                    }
                    mbg.UpdateMesh(geom);
                    meshes[i] = new ColoredTriangleMesh {
                        modelToWorld = mbg.ModelToWorld(ctx.entRes),
                        Geometry = geom, 
                        Outline = mbg.Outline,
                        /*Outline = mbg.Outline,
                        OutlineWidth = mbg.OutlineWidth,*/
                        Shader = mbg.Shader,
                        shaderProperties = mbg.shaderProperties,
                        entityId = mbg.entityId
                    };
                    i++;
                }
                if (!pb.IsMultisampled) {
                    throw new Exception("need to use sampler2D instead of sampler2DMS");
                }
                RenderMeshes(meshes, worldToClip, smat);
                foreach(var geom in throwawayGeometries) {
                    GL.DeleteBuffer(geom.EBOHandle);
                    GL.DeleteBuffer(geom.VBOHandle);
                    GL.DeleteVertexArray(geom.VAOHandle);
                }
            }

            if(ss.Cubes != null) {
                int i = 0;
                ColoredTriangleMesh[] meshes= new ColoredTriangleMesh[ss.Cubes.Length];
                foreach(var cube in ss.Cubes) {
                    meshes[i] = new ColoredTriangleMesh {
                        //Transform = cube.Transform,
                        modelToWorld = cube.ModelToWorld(ctx.entRes),
                        Geometry = rs.cubeGeometry!,
                        Tint = cube.color,
                        Outline = cube.outline,
                        Shader = BuiltinShader.CubeShader,
                        entityId = cube.entityId,
                    };
                    i++;
                }
                RenderMeshes(meshes, worldToClip, smat);
            }
            if(ss.Meshes != null) {
                RenderMeshes(ss.Meshes, worldToClip, smat);
            }
            if(ss.Beziers != null) {
                RenderBeziers(ss.Beziers, worldToClip, smat, mainBuffer, in ctx);
            }

            GL.EndQuery(QueryTarget.SamplesPassed);

            //TODO:
            //RenderTriangleMeshes();
            //RenderLinestrips();

            // TODO: OnRender delegate!

            passedcount = 0;
            GL.GetQueryObject(query, GetQueryObjectParam.QueryResult, out passedcount);
            if(passedcount <= 0) {
                break;
            }

            pb.NextLayer();
        }

        if(ss.Glyphs != null) {
            RenderGlyphs(new Vector2(pbSize.w, pbSize.h), ss.Glyphs, cam, in ctx);
        }

        GL.DeleteQuery(query);
        if(p >= 10) {
            Debug.Warning($"Rendering frame took {p} depth peels. Samples passed: {passedcount}");
        }

        if (pb.IsMultisampled) {
            GL.Disable(EnableCap.SampleShading);
        }

        // skia

        // render skia (all skia GL commands get executed here)
        //platform.Skia.Clear();
        sw.Restart();
        if(ss.Canvases != null)
            RenderCanvases(mainBuffer, ss.Canvases, worldToClip);

        pb.MakePresentable(); // make presentable so blur can read it
        kawaseBlur.ApplyBlur(pb);
        //pb.MakePresentable(); // make presentable so color map can read it
        effectBuffer.ApplyAcesColorMap(pb);
        //pb.MakePresentable(); // make presentable with blur applied

        sw.Stop();
        Performance.TimeToRenderCanvases = sw.Elapsed.TotalSeconds;
        // render skia (all skia GL commands get executed here)
        //platform.Skia.Flush();
        // render skia (all skia GL commands get executed here)

        foreach (var buf in this.renderBuffers.Values) {
            buf.OnPostRender();
        }
    }
}
