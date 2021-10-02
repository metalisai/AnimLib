using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using ImGuiNET;
using System.Collections.Concurrent;

namespace AnimLib
{
    public partial class RenderState : GameWindow
    {
        public enum BuiltinShader {
            None, 
            LineShader,
            ArrowShader,
            CubeShader,
            MeshShader,
        }

        class AllocatedResources {
            public List<int> vaos = new List<int>();
            public List<int> buffers = new List<int>();
            public List<int> textures = new List<int>();
        }

        public static ConcurrentBag<string> destroyedOwners = new ConcurrentBag<string>();

        Dictionary<string, AllocatedResources> allocatedResources = new Dictionary<string, AllocatedResources>();

        string guid = Guid.NewGuid().ToString();

        public delegate void OnUpdateDelegate();
        public OnUpdateDelegate OnUpdate;
        public delegate WorldSnapshot OnRenderSceneDelegate();
        public delegate void OnEndRenderSceneDelegate();

        public delegate void OnDisplayChangeD(int w, int h, double rate);
        public OnDisplayChangeD OnDisplayChange;

        public OnRenderSceneDelegate OnBeginRenderScene;
        public OnEndRenderSceneDelegate OnEndRenderScene;
        private int _circleProgram, _rectangleProgram, _defaultProgram, _textProgram, _staticLineProgram;
        private int _texRectProgram, _cubeProgram, _blitProgram, _arrowProgram, _imguiProgram;
        private int _meshProgram;
        private int rectVao;

        private EntityStateResolver entRes;

        private int imguiVao, imguiVbo, imguiEbo;

        private List<SceneView> views = new List<SceneView>();

        ColoredTriangleMeshGeometry cubeGeometry;

        RenderBuffer uiRenderBuffer = new DepthPeelRenderBuffer();

        FontCache _fr;
        TypeSetting ts = new TypeSetting();

        int drawId = 0;

        bool mouseLeft;
        bool mouseRight;
        Vector2 mousePos;
        float scrollValue;

        bool overrideCamera = false;
        PerspectiveCameraState debugCamera;
        Vector2 debugCamRot;

        private (int,int)? _sizeOverride = null;
        public (int,int)? SizeOverride {
            get {
                return _sizeOverride;
            }
            set {
                _sizeOverride = value;
                UpdateSize();
            }
        }

        public void UpdateSize() {
            int width = _sizeOverride == null ? this.Width : _sizeOverride.Value.Item1;
            int height = _sizeOverride == null ? this.Height : _sizeOverride.Value.Item2;
            if(uiRenderBuffer.Width != width || uiRenderBuffer.Height != height) {
                //Console.WriteLine($"Resize to {width}x{height}");
                uiRenderBuffer.Resize(width, height);
                RectTransform.RootTransform.Size = new Vector2(uiRenderBuffer.Width, uiRenderBuffer.Height);
            }
        }

        public void AddSceneView(SceneView view) {
            views.Add(view);
        }

        public void RemoveSceneView(SceneView view) {
            views.Remove(view);
        }

        private void mouseDown(object sender, MouseButtonEventArgs args) {
            if(args.Button == MouseButton.Left) {
                mouseLeft = true;
            }
            if(args.Button == MouseButton.Right) {
                mouseRight = true;
            }
        }

        private void mouseUp(object sender, MouseButtonEventArgs args) {
            if(args.Button == MouseButton.Left) {
                mouseLeft = false;
            }
            if(args.Button == MouseButton.Right) {
                mouseRight = false;
            }
        }

        private void mouseMove(object sender, MouseMoveEventArgs args) {
            mousePos = new Vector2(args.Position.X, args.Position.Y);
        }

        private void mouseScroll(object sender, MouseWheelEventArgs args) {
            scrollValue = args.ValuePrecise;
        }

        public void RenderCircles(CircleState[] circles, M4x4 mat, M4x4 orthoMat) {
            if(circles.Length > 0) {
                GL.PolygonOffset(0.4f, 1.0f);
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);
                GL.UseProgram(_circleProgram);
                var loc = GL.GetUniformLocation(_circleProgram, "_ModelToClip");
                var colLoc = GL.GetUniformLocation(_circleProgram, "_Color");
                var outlineLoc = GL.GetUniformLocation(_circleProgram, "_Outline");
                var idLoc = GL.GetUniformLocation(_circleProgram, "_EntityId");
                GL.BindVertexArray(rectVao);
                GL.VertexAttrib4(1, 1.0f, 1.0f, 1.0f, 1.0f);
                foreach(var c in circles) {
                    M4x4 modelToWorld, modelToClip;
                    // handle anchors etc for 2d canvas objects
                    /*if(c.is2d) {
                        float x,y;
                        x = RectTransform.RootTransform.Size.x*c.anchor.x;
                        y = RectTransform.RootTransform.Size.y*c.anchor.y;
                        var p = new Vector3(x, y, 0.0f);
                        pos += p;
                    }*/

                    modelToWorld = c.ModelToWorld(entRes) * M4x4.Scale(new Vector3(c.radius*2.0f, c.radius*2.0f, c.radius*2.0f));
                    modelToClip = c.is2d ? orthoMat * modelToWorld : mat * modelToWorld;
                    GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                    var col4 = Vector4.FromInt32(c.color.ToU32());
                    GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                    var b = new Vector4(c.outline.r/255.0f, c.outline.g/255.0f, c.outline.b/255.0f, c.outlineWidth);
                    GL.Uniform4(outlineLoc, b.x, b.y, b.z, b.w);
                    GL.Uniform1(idLoc, c.entityId);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                    drawId++;
                }
            }        
        }

        public void RenderRectangles(RectangleState[] rectangles, M4x4 mat, M4x4 smat) {
            if(rectangles.Length > 0) {
                GL.PolygonOffset(0.4f, 1.0f);
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);
                GL.UseProgram(_rectangleProgram);
                var loc = GL.GetUniformLocation(_rectangleProgram, "_ModelToClip");
                var outlineLoc = GL.GetUniformLocation(_rectangleProgram, "_Outline");
                var colLoc = GL.GetUniformLocation(_rectangleProgram, "_Color");
                var entLoc = GL.GetUniformLocation(_rectangleProgram, "_EntityId");
                GL.BindVertexArray(rectVao);
                GL.VertexAttrib4(1, 1.0f, 1.0f, 1.0f, 1.0f);
                foreach(var r in rectangles) {
                    M4x4 modelToWorld, modelToClip;
                    /*if(r.is2d) {
                        float x,y;
                        x = RectTransform.RootTransform.Size.x*r.anchor.x;
                        y = RectTransform.RootTransform.Size.y*r.anchor.y;
                        var p = new Vector3(x, y, 0.0f);
                        pos += p;
                    }*/

                    modelToWorld = r.ModelToWorld(entRes) * M4x4.Scale(new Vector3(r.width, r.height, 1.0f));
                    modelToClip = (r.is2d ? smat : mat)*modelToWorld;
                    GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                    var bb = new Vector4(r.outline.r/255.0f, r.outline.g/255.0f, r.outline.b/255.0f, r.outlineWidth);
                    GL.Uniform4(outlineLoc, bb.x, bb.y, bb.z, bb.w);
                    var col4 = Vector4.FromInt32(r.color.ToU32());
                    GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                    GL.Uniform1(entLoc, r.entityId);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                    drawId++;
                }
            }
        }

        private void LoadTexture(Texture2D tex2d) {
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

        public void RenderTextureRectangles(TexRectState[] rectangles, M4x4 mat) {
            if(rectangles.Length > 0) {
                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);
                GL.UseProgram(_texRectProgram);
                var loc = GL.GetUniformLocation(_texRectProgram, "_ModelToClip");
                var outlineLoc = GL.GetUniformLocation(_texRectProgram, "_Outline");
                var colLoc = GL.GetUniformLocation(_texRectProgram, "_Color");
                var texLoc = GL.GetUniformLocation(_texRectProgram, "_MainTex");
                GL.BindVertexArray(rectVao);
                GL.VertexAttrib4(1, 1.0f, 1.0f, 1.0f, 1.0f);
                foreach(var r in rectangles) {
                    if(r.texture.GLHandle == -1) {
                        LoadTexture(r.texture);
                    }
                    GL.BindTextureUnit(0, r.texture.GLHandle);
                    GL.Uniform1(texLoc, 0);
                    M4x4 modelToWorld, modelToClip;

                    modelToWorld = r.ModelToWorld(entRes) * M4x4.Scale(new Vector3(r.width, r.height, 1.0f));
                    modelToClip = mat*modelToWorld;
                    GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                    var bb = new Vector4(r.outline.r/255.0f, r.outline.g/255.0f, r.outline.b/255.0f, r.outlineWidth);
                    GL.Uniform4(outlineLoc, bb.x, bb.y, bb.z, bb.w);
                    var col4 = Vector4.FromInt32(r.color.ToU32());
                    GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                    drawId++;
                }
            }
        }

        public void RenderLabels(Vector2 screenSize, (LabelState, EntityState)[] labels, CameraState gcam) {
            var mat = M4x4.Ortho(0.0f, screenSize.x, 0.0f, screenSize.y, -1.0f, 1.0f);
            var cam = ((PerspectiveCameraState)gcam);
            foreach(var label in labels){
                var l = label.Item1;
                Rect size;
                Color c = l.color;
                var sss = ts.GetSize(l.text, l.size);
                size = new Rect(0.0f, 0.0f, sss.x, sss.y);
                var pos3 = l.target.GetLabelWorldCoordinate(l.style, label.Item2);
                var pos2 = cam.WorldToScreenPos(pos3.Value, screenSize);
                var pos = pos2 + l.target.GetLabelOffset(cam, size, l.style, label.Item2, screenSize);
                float z = l.position.z;
                if(pos != null) {
                    if(l.anim != null) {
                        c.a = (byte)(255 - (byte)(((MathF.Max(0.9f,l.anim.progress)-0.9f)/0.1f)*255));
                        Vector2 absorbpos;
                        if(l.anim.point != null) {
                            absorbpos = cam.WorldToScreenPos(l.anim.point.Value, screenSize);
                        } else {
                            absorbpos = l.anim.screenPoint.Value;
                            z += l.anim.screenPoint.Value.z;
                        }
                        pos = Vector2.Berp(pos.Value, absorbpos, l.anim.progress);
                    }
                    Vector3 p = new Vector3(pos.Value.x, pos.Value.y, z);
                    _fr.PushString(l.text, p, l.size, c, l.entityId, TextHorizontalAlignment.Center, TextVerticalAlignment.Center);
                }
            }
            _fr.RenderTest(_textProgram, mat);
        }

        public void RenderGlyphs(Vector2 screenSize, GlyphState[] gs, PerspectiveCameraState worldCamera) {
            var mat = M4x4.Ortho(0.0f, screenSize.x, 0.0f, screenSize.y, -1.0f, 1.0f);
            var cam = worldCamera;
            foreach(var ch in gs) {
                var modelToWorld = ch.ModelToWorld(entRes);
                Vector3 pos = new Vector3(modelToWorld.m14, modelToWorld.m24, modelToWorld.m34);
                _fr.PushCharacter(ch, pos);
            }
            _fr.RenderTest(_textProgram, mat);
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

        public void RenderMeshes(ColoredTriangleMesh[] meshes, M4x4 camMat, M4x4 screenMat) {
            if(meshes.Length > 0) {
                // TODO: winding order is wrong?
                GL.Disable(EnableCap.CullFace);
                GL.FrontFace(FrontFaceDirection.Ccw);
                //GL.DepthFunc(DepthFunction.Lequal);
                GL.Enable(EnableCap.DepthTest);

                //GL.VertexAttrib4(1, 1.0f, 1.0f, 1.0f, 1.0f);
                foreach(var m in meshes) {
                    var program = GetProgram(m.Shader);
                    GL.UseProgram(program);
                    var loc = GL.GetUniformLocation(program, "_ModelToClip");
                    var colLoc = GL.GetUniformLocation(program, "_Color");
                    //var outlineLoc = GL.GetUniformLocation(program, "_Outline");
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
                            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12, 0);
                            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, 4, new IntPtr(vertCount*12));
                            if(m.Geometry.edgeCoordinates != null) {
                                GL.EnableVertexAttribArray(2);
                                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, new IntPtr(vertCount*(12+4)));
                            }
                            m.Geometry.VAOHandle = vao;
                            m.Geometry.VBOHandle = vbo;
                            m.Geometry.EBOHandle = ebo;
                            // register owner for deletion (if owner gets destroyed)
                            if(m.Geometry.GetOwnerGuid() != "") {
                                AllocatedResources res;
                                if(!allocatedResources.TryGetValue(m.Geometry.GetOwnerGuid(), out res)) {
                                    res = new AllocatedResources();
                                    allocatedResources.Add(m.Geometry.GetOwnerGuid(), res);
                                }
                                res.buffers.Add(vbo); res.buffers.Add(ebo);
                                res.vaos.Add(vao);
                            }
                        } else {
                            vao = m.Geometry.VAOHandle;    
                            GL.BindVertexArray(vao);
                        }
                        GL.BindBuffer(BufferTarget.ArrayBuffer, m.Geometry.VBOHandle);
                        GL.BufferData(BufferTarget.ArrayBuffer, vertCount*(12+4+8), IntPtr.Zero, BufferUsageHint.DynamicDraw);
                        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertCount*12, ref m.Geometry.vertices[0].x);
                        GL.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(vertCount*12), vertCount*4, ref m.Geometry.colors[0]);
                        if(m.Geometry.edgeCoordinates != null) {
                            GL.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(vertCount*(12+4)), vertCount*8, ref m.Geometry.edgeCoordinates[0]);
                        }
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, m.Geometry.EBOHandle);
                        GL.BufferData(BufferTarget.ElementArrayBuffer, m.Geometry.indices.Length*4, ref m.Geometry.indices[0], BufferUsageHint.StaticDraw);
                        GL.BindVertexArray(0);
                        m.Geometry.Dirty = false;
                    }
                    GL.BindVertexArray(m.Geometry.VAOHandle);
                    M4x4 modelToClip;
                    modelToClip = !m.is2d ? camMat*m.modelToWorld : screenMat;
                    GL.UniformMatrix4(loc, 1, false, ref modelToClip.m11);
                    var col4 = Vector4.FromInt32(m.Tint.ToU32());
                    GL.Uniform4(colLoc, col4.x, col4.y, col4.z, col4.w);
                    //GL.Uniform4(outlineLoc, m.Outline.r/255.0f, m.Outline.g/255.0f, m.Outline.b/255.0f, m.OutlineWidth);
                    GL.Uniform1(entLoc, m.entityId);
                    GL.DrawElements(PrimitiveType.Triangles, m.Geometry.indices.Length, DrawElementsType.UnsignedInt, 0);
                    drawId++;
                }
            }
        }

        public void RenderImGui(ImDrawDataPtr data, Texture2D atlas) {
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
            var mat = M4x4.Ortho(0.0f, uiRenderBuffer.Width, 0.0f, uiRenderBuffer.Height, -1.0f, 1.0f);
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

        public void RenderGUI((ImDrawDataPtr, Texture2D)? data)
        {
            DepthPeelRenderBuffer pb;
            GL.Viewport(0, 0, uiRenderBuffer.Width, uiRenderBuffer.Height);
            pb = uiRenderBuffer as DepthPeelRenderBuffer;

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

            var smat = M4x4.Ortho(0.0f, pb.Width, 0.0f, pb.Height, -1.0f, 1.0f);
            drawId = 0;
            GL.ColorMask(true, true, true, true);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.DepthBufferBit/* | ClearBufferMask.ColorBufferBit*/);
            foreach(var prog in _programs) {
                var loc = GL.GetUniformLocation(prog, "_depthPeelTex");
                GL.UseProgram(prog);
                GL.ProgramUniform1(prog, loc, 1);
                GL.BindTextureUnit(1, pb.PeelTex);
            }
            RenderImGui(data.Value.Item1, data.Value.Item2);
            pb.NextLayer();
            if (data != null) pb.BlendToScreen(this.Width, this.Height, _blitProgram);
        }

        public void RenderScene(WorldSnapshot ss, SceneView sv) {
            entRes = ss.resolver;

            DepthPeelRenderBuffer pb;
            pb = sv.Buffer as DepthPeelRenderBuffer;
            GL.Viewport(0, 0, pb.Width, pb.Height);
            if (pb == null) {
                Console.WriteLine("Can't render scene because renderbuffer isn't DepthPeelRenderBuffer");
                return;
            }

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

            var background = ss.Camera.clearColor;
            GL.ClearColor((float)background.r/255.0f, (float)background.g/255.0f, (float)background.b/255.0f, (float)background.a/255.0f);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            var bufs = new DrawBuffersEnum[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1};
            GL.DrawBuffers(2, bufs);

            GL.BlendEquation(BlendEquationMode.FuncAdd);
            //GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.Zero); 
            GL.Enable(EnableCap.Blend);

            var smat = M4x4.Ortho(0.0f, pb.Width, 0.0f, pb.Height, -1.0f, 1.0f);
            var query = GL.GenQuery();

            for(int p = 0; p < 16; p++) {
                drawId = 0;
                GL.ColorMask(true, true, true, true);
                GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                GL.Clear(ClearBufferMask.DepthBufferBit/* | ClearBufferMask.ColorBufferBit*/);
                foreach(var prog in _programs) {
                    var loc = GL.GetUniformLocation(prog, "_depthPeelTex");
                    GL.UseProgram(prog);
                    GL.ProgramUniform1(prog, loc, 1);
                    GL.BindTextureUnit(1, pb.PeelTex);
                }
                GL.BeginQuery(QueryTarget.SamplesPassed, query);
                //GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

                // TODO: bind framebuffer when we have render targets
                var sceneCamera = ss.Camera;

                if(sceneCamera is OrthoCameraState) {
                    var cam = sceneCamera as OrthoCameraState;
                    cam.width = pb.Width;
                    cam.height = pb.Height;
                }
                M4x4 worldToClip = sceneCamera.CreateWorldToClipMatrix((float)pb.Width/(float)pb.Height);
                if(overrideCamera && sceneCamera is PerspectiveCameraState) {
                    worldToClip = debugCamera.CreateWorldToClipMatrix((float)pb.Width/(float)pb.Height);
                }

                RectTransform.RootTransform = new RectTransform(new Dummy());
                RectTransform.RootTransform.Size = new Vector2(pb.Width, pb.Height);

                if(ss.MeshBackedGeometries != null) {
                    int i = 0;
                    ColoredTriangleMesh[] meshes = new ColoredTriangleMesh[ss.MeshBackedGeometries.Length];
                    var throwawayGeometries = new List<ColoredTriangleMeshGeometry>();
                    foreach(var mbg in ss.MeshBackedGeometries) {
                        ColoredTriangleMeshGeometry geom = null;
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
                            modelToWorld = mbg.ModelToWorld(entRes),
                            Geometry = geom, 
                            /*Outline = mbg.Outline,
                            OutlineWidth = mbg.OutlineWidth,*/
                            Shader = mbg.Shader,
                            shaderProperties = mbg.shaderProperties,
                            entityId = mbg.entityId
                        };
                        i++;
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
                            modelToWorld = cube.ModelToWorld(entRes),
                            Geometry = cubeGeometry,
                            Tint = cube.color,
                            Shader = BuiltinShader.CubeShader,
                            entityId = cube.entityId,
                        };
                        i++;
                    }
                    RenderMeshes(meshes, worldToClip, smat);
                }

                if(ss.Circles != null)
                    RenderCircles(ss.Circles, worldToClip, smat);

                if(ss.Rectangles != null)
                    RenderRectangles(ss.Rectangles, worldToClip, smat);
                if(ss.Meshes != null) {
                    RenderMeshes(ss.Meshes, worldToClip, smat);
                }
                if(ss.TexRects != null) {
                    RenderTextureRectangles(ss.TexRects, worldToClip);
                }
                /*if(ss.Texts != null)
                    Render2DTexts(ss.Texts);*/
                /*if(ss.Texts != null) {
                    Render2DTexts2(new Vector2(pb.Width, pb.Height), ss.Texts, sceneCamera as PerspectiveCameraState);
                }*/
                if(ss.Glyphs != null) {
                    RenderGlyphs(new Vector2(pb.Width, pb.Height), ss.Glyphs, sceneCamera as PerspectiveCameraState);
                }
                if(ss.Labels != null) {
                    RenderLabels(new Vector2(pb.Width, pb.Height), ss.Labels, UserInterface.WorldCamera);
                }

                //TODO:
                //RenderTriangleMeshes();
                //RenderLinestrips();

                // TODO: OnRender delegate!

                GL.EndQuery(QueryTarget.SamplesPassed);
                long passedcount = 0;
                GL.GetQueryObject(query, GetQueryObjectParam.QueryResult, out passedcount);
                if(passedcount <= 0) {
                    break;
                }

                pb.NextLayer();
            }
            GL.DeleteQuery(query);
        }

        public int GetSceneEntityAtPixel(SceneView sv, Vector2 pixel) {
            var area = sv.LastArea;
            if (area == null) return -2;
            var a = area.Value;
            var offset = pixel - new Vector2(a.Item1, a.Item2);
            var normalizedInViewport = offset / new Vector2(a.Item3, a.Item4);
            var bufW = sv.Buffer.Width;
            var bufH = sv.Buffer.Height;
            pixel = normalizedInViewport * new Vector2(bufW, bufH);
            sv.Buffer.Bind();
            GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
            var pixs = new int[1];
            if(pixel.x >= 0.0f && pixel.x < bufW && pixel.y >= 0.0f && pixel.y < bufH) {
                GL.ReadPixels((int)pixel.x, bufH-(int)pixel.y-1, 1, 1, PixelFormat.RedInteger, PixelType.Int, pixs);
                return pixs[0];
            } else {
                return -2;
            }
        }

        public int GetGuiEntityAtPixel(RenderBuffer pb, Vector2 pixel) {
            pb.Bind();
            GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
            var pixs = new int[1];
            //System.Console.WriteLine(pixs[0]);
            if(pixel.x >= 0.0f && pixel.x < pb.Width && pixel.y >= 0.0f && pixel.y < pb.Height) {
                GL.ReadPixels((int)pixel.x, pb.Height-(int)pixel.y-1, 1, 1, PixelFormat.RedInteger, PixelType.Int, pixs);
                return pixs[0];
            } else {
                return -2;
            }
        }

        private int AddShader(string v, string f, string g) {
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
            GL.LinkProgram(ret);
            GL.GetProgram(ret, GetProgramParameterName.LinkStatus, ps);
            if(ps[0] != 1) {
                int slen;
                string slog = "";
                GL.GetProgramInfoLog(ret, 256, out slen, out slog);
                Console.WriteLine("Failed to link program!");
                Console.WriteLine(slog);
            }
            _programs.Add(ret);
            // TODO: delete shaders?
            return ret;
        }

        private void CompileShaders() {
            _circleProgram = AddShader(circleVert, circleFrag, null);
            _rectangleProgram = AddShader(rectangleVert, rectangleFrag, null);
            _texRectProgram = AddShader(rectangleVert, texRectFrag, null);
            _defaultProgram = AddShader(vertShader, fragShader, null);
            _imguiProgram = AddShader(imguiVert, imguiFrag, null);
            _arrowProgram = AddShader(rectangleVert, arrowFrag, null);
            _staticLineProgram = AddShader(staticLineVert, staticLineFrag, null);
            _cubeProgram = AddShader(staticLineVert, cubeFrag, null);
            _meshProgram = AddShader(vertShader, meshFrag, meshGeom);
            //_wireTriangleProgram = AddShader(wireTriangleVert, wireTriangleFrag, null);
            //_lineProgram = AddShader(lineVert, lineFrag, lineGeom);
            _textProgram = AddShader(textVert, textFrag, null);
            _blitProgram = AddShader(blitVert, blitFrag, null);
        }
        
        List<int> _programs = new List<int>();

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

            cubeGeometry = new ColoredTriangleMeshGeometry(guid);
            cubeGeometry.vertices = new Vector3[] {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
            };
            cubeGeometry.indices = new uint[] {
                0,1,2, 1,3,2, 0,4,1, 1,4,5, 2,7,6, 2,3,7, 1,7,3, 1,5,7, 4,2,6, 4,0,2, 5,6,7, 5,4,6
            };
            cubeGeometry.colors = new Color[] {
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
                Color.WHITE,
            };
            cubeGeometry.Dirty = true;
            cubeGeometry.edgeCoordinates = new Vector2[] {
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
                Vector2.ZERO,
            };
        }

        static void debugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
            if(type == DebugType.DebugTypeOther)
                return;
            Console.WriteLine($"OpenGL debug ({type}): {Marshal.PtrToStringAnsi(message)}");
        }

        public RenderState(int width, int height) : base(width, height, new OpenTK.Graphics.GraphicsMode(), "Test", 0 , DisplayDevice.Default, 3, 3, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible) {
            Width = width;
            Height = height;
        }

        static DebugProc proc;

        protected override void OnLoad(EventArgs e) {
            CompileShaders();
            CreateMeshes();

            //renderBuffer.Resize(this.Width, this.Height);
            uiRenderBuffer.Resize(1024, 1024);

            proc = new DebugProc(debugCallback);
            GL.DebugMessageCallback(proc, IntPtr.Zero);
            GL.Disable(EnableCap.Dither);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.Blend);
            //GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.FuncAdd);
            //GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrc1Alpha, BlendingFactorSrc.One, BlendingFactorDest.Zero);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Front);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.DepthTest);

            string version = GL.GetString(StringName.Version);
            string shadingLang = GL.GetString(StringName.ShadingLanguageVersion);
            string renderer = GL.GetString(StringName.Renderer);
            Console.WriteLine(version);
            Console.WriteLine(shadingLang);
            Console.WriteLine(renderer);
            _fr = new FontCache(ts);

            this.MouseDown += mouseDown;
            this.MouseUp += mouseUp;
            this.MouseMove += mouseMove;
            this.MouseWheel += mouseScroll;
            
            this.KeyDown += (object sender, KeyboardKeyEventArgs args) => {
                if(args.Key == Key.F && !args.IsRepeat) {
                    overrideCamera = !overrideCamera;
                    if(overrideCamera) {
                        debugCamera = new PerspectiveCameraState();
                        debugCamera.position.z = -13.0f;
                        UserInterface.DebugCamera = debugCamera;
                        UserInterface.UseDebugCamera = true;
                    } else {
                        UserInterface.UseDebugCamera = false;
                        debugCamera.position = Vector3.ZERO;
                        this.debugCamRot = Vector2.ZERO;
                    }
                }
                if(ImGui.GetCurrentContext() != null) {
                    var io = ImGui.GetIO();
                    io.KeysDown[(int)args.Key] = true;
                }
            };
            this.KeyUp += (object sender, KeyboardKeyEventArgs args) => {
                if(ImGui.GetCurrentContext() != null) {
                    var io = ImGui.GetIO();
                    io.KeysDown[(int)args.Key] = false;
                }
            };
            this.KeyPress += (object sender, KeyPressEventArgs args) => {
                if(ImGui.GetCurrentContext() != null) {
                    var io = ImGui.GetIO();
                    io.AddInputCharacter(args.KeyChar);
                }
            };

            this.OnUpdate += () => {
                var kstate = Keyboard.GetState();
                if(overrideCamera) {
                    //UIOldCam = UserInterface.WorldCamera;
                    float dt = (float)this.UpdatePeriod;
                    float s = 5.0f;
                    if(kstate.IsKeyDown(Key.ShiftLeft)) {
                        s = 0.01f;
                    }
                    if(kstate.IsKeyDown(Key.W)) {
                        debugCamera.position += debugCamera.rotation*Vector3.FORWARD*dt*s;
                    }
                    if(kstate.IsKeyDown(Key.S)) {
                        debugCamera.position -= debugCamera.rotation*Vector3.FORWARD*dt*s;
                    }
                    if(kstate.IsKeyDown(Key.A)) {
                        debugCamera.position -= debugCamera.rotation*Vector3.RIGHT*dt*s;
                    }
                    if(kstate.IsKeyDown(Key.D)) {
                        debugCamera.position += debugCamera.rotation*Vector3.RIGHT*dt*s;
                    }
                }
            };

            this.MouseMove += (object s, MouseMoveEventArgs args) => {
                var state = OpenTK.Input.Keyboard.GetState();
                if(overrideCamera && !state[OpenTK.Input.Key.ControlLeft]) {
                    this.debugCamRot.x += args.XDelta*0.01f;
                    this.debugCamRot.y += args.YDelta*0.01f;
                    this.debugCamRot.x %= 2*MathF.PI;
                    this.debugCamRot.y %= 2*MathF.PI;
                    var qx = Quaternion.AngleAxis(debugCamRot.x, Vector3.UP);
                    var qy = Quaternion.AngleAxis(debugCamRot.y, Vector3.RIGHT);
                    debugCamera.rotation = qy * qx;
                }
            };

            base.OnLoad(e);
        }

        protected override void OnResize(EventArgs e) {
            UpdateSize();
            if(OnDisplayChange != null) {
                double maxrate = 0.0;
                foreach (DisplayIndex index in Enum.GetValues(typeof(DisplayIndex))) { 
                    var dsp = DisplayDevice.GetDisplay(index);
                    if (dsp != null) {
                        maxrate = Math.Max(dsp.RefreshRate, maxrate);
                    }
                }
                if (maxrate == 0.0) maxrate = 60.0;
                OnDisplayChange(this.Width, this.Height, maxrate);
            }
            base.OnResize(e);
        }

        //static CameraState UIOldCam;

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

            UserInterface.MouseState mouseState = new UserInterface.MouseState {
                position = mousePos,
                left = mouseLeft,
                right = mouseRight,
                scroll = scrollValue,
            };

            foreach(var view in views) view.Buffer.Clear();
            uiRenderBuffer.Clear();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DrawBuffer(DrawBufferMode.Back);
            GL.Viewport(0, 0, uiRenderBuffer.Width, uiRenderBuffer.Height);
            GL.ColorMask(true, true, true, true);
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            UserInterface.Size = new Vector2(uiRenderBuffer.Width, uiRenderBuffer.Height);
            UserInterface.BeginFrame(mouseState, uiRenderBuffer.Width, uiRenderBuffer.Height);
            foreach(var view in views) {
                view.BeginFrame();
            }
            if(OnUpdate != null) {
                OnUpdate();
            }
            
            WorldSnapshot worldSnapshot = null;
            if(OnBeginRenderScene != null) {
                worldSnapshot = OnBeginRenderScene();
            }
            // Render scene
            if(worldSnapshot != null)
            {
                foreach(var sv in views) {
                    //RenderScene(worldSnapshot, sv, new Vector4(0.5f, 0.3f, 0.7f, 1.0f));
                    RenderScene(worldSnapshot, sv);
                }
            }

            if(OnEndRenderScene != null) {
                OnEndRenderScene();
            }

            // Render UI
            var uiData = UserInterface.EndFrame();
            RenderGUI(uiData.Item2);

            int sceneEntity = -2;
            if (views.Count > 0) {
                sceneEntity = GetSceneEntityAtPixel(views[0], UserInterface.mousePosition);
            }
            var guiEntity = GetGuiEntityAtPixel(uiRenderBuffer, UserInterface.mousePosition);
            if(sceneEntity == -2) { // out of scene viewport
                UserInterface.MouseEntityId = guiEntity;
            } else {
                if (guiEntity == -1)
                    UserInterface.MouseEntityId = sceneEntity;
                else
                    UserInterface.MouseEntityId = guiEntity;
            }
            
            //Console.WriteLine(UserInterface.MouseEntityId);

            //renderBuffer.BlendToScreen(this.Width, this.Height, _blitProgram);

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }
    }
}
