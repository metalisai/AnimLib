using System;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib;

/// <summary>
/// Incomplete attempt at a tessallation renderer. Not used.
/// </summary>
partial class TessallationRenderer : IRenderer, IDisposable {

    class RenderContext {
        public Vector3 camPosWorld;
        public M4x4 worldToClip;
        public M4x4 screenToClip;
    }

    private EntityStateResolver entRes;
    private OpenTKPlatform platform;
    private RenderState rs;

    private int _circleVao = -1;
    private int _cubeVao = -1;

    int _standardProgram = -1;
    int _circleProgram = -1;

    const int circleSegments = 360;

    RenderContext ctx = new RenderContext();

    public TessallationRenderer(OpenTKPlatform platform, RenderState rs) {
        this.platform = platform;
        this.rs = rs;
        _standardProgram = platform.AddShader(vertShader, fragShader, null);
        _circleProgram = platform.AddShader(tessVS, fragShader, null, tessTCS, tessTES);
        CreateMeshes();
    }

    private void CreateMeshes() {
        var initial = new Vector2(1.0f, 0.0f);
        var circleVertices = new float[(circleSegments + 2)*2];
        circleVertices[0] = 0.0f;
        circleVertices[1] = 0.0f;
        // Note: this should be rendered as triangle fan
        for(int i = 0; i <= circleSegments; i++) {
            float angle = (float)i * (MathF.PI/180.0f);
            var pos = initial.Rotated(angle);
            circleVertices[(i+1)*2] = pos.x;
            circleVertices[(i+1)*2 + 1] = pos.y;
        }
        _circleVao = GL.GenVertexArray();
        GL.BindVertexArray(_circleVao);
        var vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, circleVertices.Length*sizeof(float)*2, circleVertices, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 0, 0);

        var cubeV = new Vector3[] {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
        };
        var cubeI = new uint[] {
            0,1,2, 1,3,2, 0,4,1, 1,4,5, 2,7,6, 2,3,7, 1,7,3, 1,5,7, 4,2,6, 4,0,2, 5,6,7, 5,4,6
        };
        _cubeVao = GL.GenVertexArray();
        GL.BindVertexArray(_cubeVao);
        var cvbo = GL.GenBuffer();
        var cebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, cvbo);
        GL.BufferData(BufferTarget.ArrayBuffer, cubeV.Length*sizeof(float)*3, ref cubeV[0].x, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, cebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, cubeI.Length*sizeof(int), ref cubeI[0], BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

        GL.BindVertexArray(0);
    }

    /*public void RenderCircles(CircleState[] circles)
    {
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        if(circles.Length > 0) {
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.UseProgram(_circleProgram);
            GL.PatchParameter(PatchParameterInt.PatchVertices, 1);
            var m2wL = GL.GetUniformLocation(_circleProgram, "_ModelToWorld");
            var w2cL = GL.GetUniformLocation(_circleProgram, "_WorldToClip");
            var cwpL = GL.GetUniformLocation(_circleProgram, "_CamPosWorld");
            var rL = GL.GetUniformLocation(_circleProgram, "_Radius");
            //var loc = GL.GetUniformLocation(_standardProgram, "_ModelToClip");
            var colLoc = GL.GetUniformLocation(_circleProgram, "_Color");
            var entLoc = GL.GetUniformLocation(_circleProgram, "_EntityId");
            GL.VertexAttrib4(1, 1.0f, 1.0f, 1.0f, 1.0f);
            GL.BindVertexArray(_circleVao);
            foreach(var c in circles) {
                M4x4 modelToWorld;
                modelToWorld = c.ModelToWorld(entRes) * M4x4.Scale(new Vector3(c.radius, c.radius, c.radius));
                //modelToClip = (c.is2d ? orthoMat : mat)*modelToWorld;
                GL.UniformMatrix4(m2wL, 1, false, ref modelToWorld.m11);
                GL.UniformMatrix4(w2cL, 1, false, ref ctx.worldToClip.m11);
                GL.Uniform3(cwpL, ctx.camPosWorld.x, ctx.camPosWorld.y, ctx.camPosWorld.z);
                GL.Uniform1(rL, c.radius);
                var col4 = Vector4.FromInt32(c.color.ToU32());
                GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                GL.Uniform1(entLoc, c.entityId);
                //GL.DrawArrays(PrimitiveType.TriangleFan, 0, circleSegments+2);
                GL.DrawArrays(PrimitiveType.Patches, 0, 1); 
            }
        }
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
    }*/

    public void RenderCubes(CubeState[] cubes, M4x4 mat)
    {
        if(cubes.Length > 0) {
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.UseProgram(_standardProgram);
            var loc = GL.GetUniformLocation(_standardProgram, "_ModelToClip");
            var colLoc = GL.GetUniformLocation(_standardProgram, "_Color");
            var entLoc = GL.GetUniformLocation(_standardProgram, "_EntityId");
            GL.VertexAttrib4(1, 1.0f, 1.0f, 1.0f, 1.0f);
            GL.BindVertexArray(_cubeVao);
            foreach(var c in cubes) {
                M4x4 modelToWorld, modelToClip;
                modelToWorld = c.ModelToWorld(entRes);
                modelToClip = mat*modelToWorld;
                GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                var col4 = Vector4.FromInt32(c.color.ToU32());
                GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                GL.Uniform1(entLoc, c.entityId);
                GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
            }
        }
    }

    public void RenderRectangles(RectangleState[] rectangles, M4x4 mat, M4x4 orthoMat)
    {
        if(rectangles.Length > 0) {
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.UseProgram(_standardProgram);
            var loc = GL.GetUniformLocation(_standardProgram, "_ModelToClip");
            var colLoc = GL.GetUniformLocation(_standardProgram, "_Color");
            var entLoc = GL.GetUniformLocation(_standardProgram, "_EntityId");
            GL.VertexAttrib4(1, 1.0f, 1.0f, 1.0f, 1.0f);
            GL.BindVertexArray(platform.rectVao);
            foreach(var r in rectangles) {
                M4x4 modelToWorld, modelToClip;
                modelToWorld = r.ModelToWorld(entRes) * M4x4.Scale(new Vector3(r.width, r.height, 1.0f));
                modelToClip = mat*modelToWorld;
                GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                var col4 = Vector4.FromInt32(r.color.ToU32());
                GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                GL.Uniform1(entLoc, r.entityId);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }
        }
    }
    public void RenderBeziers(BezierState[] beziers, M4x4 mat, M4x4 orthoMat, IRenderBuffer rb)
    {
    }
    /*public void RenderTextureRectangles(TexRectState[] rectangles, M4x4 mat)
    {
    }*/
    public void RenderMeshes(ColoredTriangleMesh[] meshes, M4x4 camMat, M4x4 orthoMat)
    {
    }
    public void RenderScene(WorldSnapshot ss, SceneView sv, CameraState cam, bool gizmo)
    {
        entRes = ss.resolver;

        MultisampleRenderBuffer pb; 
        var w = sv.BufferWidth;
        var h = sv.BufferHeight;
        if(!(sv.Buffer is MultisampleRenderBuffer) || sv.Buffer == null) {
            Debug.Warning("Using triangle rendering, but renderbuffer is not multisampled, allocating new buffer");
            var buf = new MultisampleRenderBuffer(platform);
            buf.Resize(w, h);
            sv.Buffer = buf;
        }
        pb = sv.Buffer as MultisampleRenderBuffer;
        GL.Viewport(0, 0, pb.Size.Item1, pb.Size.Item2);
        if (pb == null) {
            Debug.Error("TessallationRenderer needs MultisampleRenderBuffer, but it wasn't created.");
            return;
        }

        /*pb.Bind();
        GL.DepthMask(true);
        GL.ClearDepth(1.0f);
        GL.Clear(ClearBufferMask.DepthBufferBit);*/
        pb.Bind();

        var bcol = ss.Camera.clearColor;
        GL.ClearColor((float)bcol.r/255.0f, (float)bcol.g/255.0f, (float)bcol.b/255.0f, (float)bcol.a/255.0f);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        var bufs = new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1};
        GL.DrawBuffers(2, bufs);

        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);

        var smat = M4x4.Ortho(0.0f, pb.Size.Item1, 0.0f, pb.Size.Item2, -1.0f, 1.0f);
        var _programs = platform.Programs;

        // begin rendering
        GL.ColorMask(true, true, true, true);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        GL.Clear(ClearBufferMask.DepthBufferBit);

        // TODO: reuse this between renderers
        var pbSize = pb.Size;

        if(cam is OrthoCameraState) {
            var ocam = cam as OrthoCameraState;
            ocam.width = pbSize.Item1;
            ocam.height = pbSize.Item2;
        }

        M4x4 worldToClip = cam.CreateWorldToClipMatrix((float)pbSize.Item1/(float)pbSize.Item2);

        RectTransform.RootTransform = new RectTransform(new Dummy());
        RectTransform.RootTransform.Size = new Vector2(pbSize.Item1, pbSize.Item2);

        ctx.worldToClip = worldToClip;
        ctx.screenToClip = smat;
        ctx.camPosWorld = cam.position;

        // render rectangles
        if(ss.Rectangles != null) {
            RenderRectangles(ss.Rectangles, worldToClip, smat);
        }
        // render meshbackedgeometries
        // render cubes
        if(ss.Cubes != null) {
            RenderCubes(ss.Cubes, worldToClip);
        }
        // render circles
        /*if(ss.Circles != null) {
            RenderCircles(ss.Circles);
        }*/
        // render meshes
        // render texrects
        // render glyphs
        // render labels
        // render beziers

    }

    public bool BufferValid(IRenderBuffer buf) {
        return buf is MultisampleRenderBuffer;
    }

    public IRenderBuffer CreateBuffer(int w, int h) {
        var buf = new MultisampleRenderBuffer(platform);
        buf.Resize(w, h);
        // TODO: this is wrong!
        platform.Skia.SetBuffer(buf);
        return buf;
    }

    public void Dispose() {
        // TODO; delete shaders
        // TODO: delete circle vao and vbo
    }
}
