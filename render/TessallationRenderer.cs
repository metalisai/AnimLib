using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace AnimLib {
    partial class TessallationRenderer : IRenderer {

        private EntityStateResolver entRes;
        private RenderState rs;

        int _standardProgram = -1;

        public TessallationRenderer(RenderState rs) {
            this.rs = rs;
            _standardProgram = rs.AddShader(vertShader, fragShader, null);
        }

        public void RenderCircles(CircleState[] circles, M4x4 mat, M4x4 orthoMat)
        {
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
                GL.BindVertexArray(rs.rectVao);
                foreach(var r in rectangles) {
                    M4x4 modelToWorld, modelToClip;
                    modelToWorld = r.ModelToWorld(entRes) * M4x4.Scale(new Vector3(r.width, r.height, 1.0f));
                    modelToClip = (r.is2d ? orthoMat : mat)*modelToWorld;
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
        public void RenderTextureRectangles(TexRectState[] rectangles, M4x4 mat)
        {
        }
        public void RenderMeshes(ColoredTriangleMesh[] meshes, M4x4 camMat, M4x4 orthoMat)
        {
        }
        public void RenderScene(WorldSnapshot ss, SceneView sv)
        {
            MultisampleRenderBuffer pb; 
            var w = sv.BufferWidth;
            var h = sv.BufferHeight;
            if(!(sv.Buffer is MultisampleRenderBuffer) || sv.Buffer == null) {
                var buf = new MultisampleRenderBuffer();
                buf.Resize(w, h);
                sv.Buffer = buf;
            }
            pb = sv.Buffer as MultisampleRenderBuffer;
            GL.Viewport(0, 0, pb.Size.Item1, pb.Size.Item2);
            if (pb == null) {
                Debug.Error("TessallationRenderer needs MultisampleRenderBuffer");
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
            var _programs = rs.Programs;

            // begin rendering
            GL.ColorMask(true, true, true, true);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // TODO: reuse this between renderers
            var sceneCamera = ss.Camera;
            var pbSize = pb.Size;

            if(sceneCamera is OrthoCameraState) {
                var cam = sceneCamera as OrthoCameraState;
                cam.width = pbSize.Item1;
                cam.height = pbSize.Item2;
            }

            M4x4 worldToClip = sceneCamera.CreateWorldToClipMatrix((float)pbSize.Item1/(float)pbSize.Item2);
            if(rs.overrideCamera && sceneCamera is PerspectiveCameraState) {
                worldToClip = rs.debugCamera.CreateWorldToClipMatrix((float)pbSize.Item2/(float)pbSize.Item2);
            }

            RectTransform.RootTransform = new RectTransform(new Dummy());
            RectTransform.RootTransform.Size = new Vector2(pbSize.Item1, pbSize.Item2);

            // render rectangles
            if(ss.Rectangles != null) {
                RenderRectangles(ss.Rectangles, worldToClip, smat);
            }
            // render meshbackedgeometries
            // render cubes
            // render circles
            // render meshes
            // render texrects
            // render glyphs
            // render labels
            // render beziers

        }
    }
}
