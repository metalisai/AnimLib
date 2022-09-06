using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using System.Collections.Generic;

namespace AnimLib {

    public static class SkiaExtensions
    {
        public static SKPoint ToSKPoint(this Vector2 v) {
            return new SKPoint(v.x, v.y);
        }

        public static SKColor ToSKColor(this Color c) {
            return new SKColor(c.r, c.g, c.b, c.a);
        }

        public static SKMatrix ToSKMatrix(this M3x3 m) {
            return new SKMatrix(m.m11, m.m12, m.m13, m.m21, m.m22, m.m23, m.m31, m.m32, m.m33);
        }
    }

    public partial class SkiaRenderer
    {
        public enum RenderMode {
            None,
            OpenGL,
            Software,
        }

        public RenderMode Mode {
            get { return mode; }
        }

        int textureId = 0;
        Dictionary<int, SKBitmap> LoadedImages = new Dictionary<int, SKBitmap>();

        RenderMode mode;

        GRContext ctx;
        GRBackendRenderTarget renderTarget;
        SKSurface surface;
        SKCanvas canvas;
        IRenderBuffer glBuffer;
        GRGlInterface glInterface;

        Texture2D tex;

        IPlatform platform;

        public SkiaRenderer(IPlatform platform) {
            this.platform = platform;
        }

        /*public int TextureId {
            get {
                return glBuffer.Texture();
            }
        }*/

        private SKBitmap LoadTexture(Texture2D texture) {
            int handle = textureId++;
            SKImageInfo info;
            switch(texture.Format) {
                case Texture2D.TextureFormat.RGBA8:
                    info = new SKImageInfo(texture.Width, texture.Height, SKColorType.Rgba8888);
                    info.AlphaType = SKAlphaType.Unpremul;
                    break;
                case Texture2D.TextureFormat.BGRA8:
                    texture.ConvertColor(Texture2D.TextureFormat.RGBA8);
                    info = new SKImageInfo(texture.Width, texture.Height, SKColorType.Rgba8888);
                    info.AlphaType = SKAlphaType.Unpremul;
                    break;
                case Texture2D.TextureFormat.BGR8:
                    texture.ConvertColor(Texture2D.TextureFormat.RGBx8);
                    info = new SKImageInfo(texture.Width, texture.Height, SKColorType.Rgb888x);
                    info.AlphaType = SKAlphaType.Opaque;
                    break;
                default:
                    Debug.Error($"Can't load texture, unsupported format {texture.Format}");
                    return null;
            }
            SKBitmap bitmap = new();
            GCHandle gcHandle = GCHandle.Alloc(texture.RawData, GCHandleType.Pinned);
            var ptr = gcHandle.AddrOfPinnedObject();
            bitmap.InstallPixels(info, ptr);
            LoadedImages.Add(handle, bitmap);
            texture.GLHandle = handle;
            Debug.TLog($"Loaded texture {texture.Width}x{texture.Height} format {texture.Format}");
            return bitmap;
        }

        public void SetBuffer(IRenderBuffer buf) {
            if(mode == RenderMode.OpenGL) {
                // in OpenGL render directly to framebuffer
                ctx.ResetContext();
                var fbInfo = new GRGlFramebufferInfo((uint)buf.FBO, SKColorType.Rgba8888.ToGlSizedFormat());
                renderTarget = new GRBackendRenderTarget(buf.Size.Item1, buf.Size.Item2, 0, 8, fbInfo);
                surface = SKSurface.Create(ctx, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
                canvas = surface.Canvas;
                glBuffer = buf;
            } else if(mode == RenderMode.Software) {
                // in software mode render to image then blit to renderbuffer
                var imageInfo = new SKImageInfo(
                    width: buf.Size.Item1,
                    height: buf.Size.Item2,
                    colorType: SKColorType.Rgba8888,
                    alphaType: SKAlphaType.Premul);
                surface = SKSurface.Create(imageInfo);
                canvas = surface.Canvas;
                glBuffer = buf;
                renderTarget = null;
            }
        }

        // OpenGL rendering
        public void CreateGL() {
            if(mode != RenderMode.None) {
                Debug.Error($"CreateGL() called after already initialized, mode: {mode}");
                return;
            }
            mode = RenderMode.OpenGL;
            glInterface = GRGlInterface.Create();
            if(!glInterface.Validate()) {
                Debug.Error("Gl interface not valid for skia");
            }
            ctx = GRContext.CreateGl(glInterface);
            /*var fbInfo = new GRGlFramebufferInfo((uint)glBuffer.FBO, SKColorType.Rgba8888.ToGlSizedFormat());
            renderTarget = new GRBackendRenderTarget(1920, 1080, 0, 8, fbInfo);
            surface = SKSurface.Create(ctx, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
            canvas = surface.Canvas;*/
        }

        // Software rendering
        public void CreateSW() {
            if(mode != RenderMode.None) {
                Debug.Error($"CreateGL() called after already initialized, mode: {mode}");
                return;
            }
            mode = RenderMode.Software;
            glInterface = null;
            ctx = null;
        }

        public void Clear() {
            if(mode == RenderMode.OpenGL) {
                ctx.ResetContext();
            }
            canvas.Clear();
        }

        SKMatrix? GetCanvasMatrix2D(ref M4x4 canvasToClip, CanvasState canvas) {
            if(!canvas.is2d) {
                float bw = glBuffer.Size.Item1;
                float bh = glBuffer.Size.Item2;
                var bl = canvasToClip * new Vector4(-0.5f, -0.5f, 0.0f, 1.0f);
                bl.x /= bl.w; bl.y /= bl.w; bl.z /= bl.w;
                var br = canvasToClip * new Vector4(0.5f, -0.5f, 0.0f, 1.0f);
                br.x /= br.w; br.y /= br.w; br.z /= br.w;
                var tl = canvasToClip * new Vector4(-0.5f, 0.5f, 0.0f, 1.0f);
                tl.x /= tl.w; tl.y /= tl.w; tl.z /= tl.w;
                var tr = canvasToClip * new Vector4(0.5f, 0.5f, 0.0f, 1.0f);
                tr.x /= tr.w; tr.y /= tr.w; tr.z /= tr.w;
                var c = canvasToClip * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                c.x /= c.w; c.y /= c.w; c.z /= c.w;
                // NOTE: perspective clipping isn't handled, so we have to get rid of the image if the canvas is behind the camera
                // this solution is far from perfect but gets rid of ghost images for most part
                // perspective clipping is tough
                if(bl.w < 0 || br.w < 0 || tl.w < 0 || tr.w < 0)
                    return null;
                float x1 = (0.5f * tl.x + 0.5f) * bw;
                float y1 = (0.5f * tl.y + 0.5f) * bh;
                float x2 = (0.5f * tr.x + 0.5f) * bw;
                float y2 = (0.5f * tr.y + 0.5f) * bh;
                float x3 = (0.5f * br.x + 0.5f) * bw;
                float y3 = (0.5f * br.y + 0.5f) * bh;
                float x4 = (0.5f * bl.x + 0.5f) * bw;
                float y4 = (0.5f * bl.y + 0.5f) * bh;
                float w = canvas.width;
                float h = canvas.height;

                float persp0 = (x1*y3 - x3*y1 - x1*y4 - x2*y3 + x3*y2 + x4*y1 + x2*y4 - x4*y2)/(w*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float persp1 = -(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y4 - x4*y2 - x3*y4 + x4*y3)/(h*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float persp2 = 1.0f;
                float scaleX = (x1*x3*y2 - x2*x3*y1 - x1*x4*y2 + x2*x4*y1 - x1*x3*y4 + x1*x4*y3 + x2*x3*y4 - x2*x4*y3)/(w*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float scaleY = (x2*y1*y3 - x3*y1*y2 - x1*y2*y4 + x4*y1*y2 + x1*y3*y4 - x4*y1*y3 - x2*y3*y4 + x3*y2*y4)/(h*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float skewX = (x1*x2*y3 - x1*x3*y2 - x1*x2*y4 + x2*x4*y1 + x1*x3*y4 - x3*x4*y1 - x2*x4*y3 + x3*x4*y2)/(h*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float skewY = (x1*y2*y3 - x2*y1*y3 - x1*y2*y4 + x2*y1*y4 - x3*y1*y4 + x4*y1*y3 + x3*y2*y4 - x4*y2*y3)/(w*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float transX = x4;
                float transY = y4;

                // TODO: something more readable?
                // top left
                /*float scaleX = (y1 * x2 * x4 - x1 * y2 * x4 + x1 * y3 * x4 - x2 * y3 * x4 - y1 * x2 * x3 + x1 * y2 * x3 - x1 * y4 * x3 + x2 * y4 * x3) / (x2 * y3 * w + y2 * x4 * w - y3 * x4 * w - x2 * y4 * w - y2 * w * x3 + y4 * w * x3);
        float skewX = (-x1 * x2 * y3 - y1 * x2 * x4 + x2 * y3 * x4 + x1 * x2 * y4 + x1 * y2 * x3 + y1 * x4 * x3 - y2 * x4 * x3 - x1 * y4 * x3) / (x2 * y3 * h + y2 * x4 * h - y3 * x4 * h - x2 * y4 * h - y2 * h * x3 + y4 * h * x3);
                float transX = x1;
                float skewY = (-y1 * x2 * y3 + x1 * y2 * y3 + y1 * y3 * x4 - y2 * y3 * x4 + y1 * x2 * y4 - x1 * y2 * y4 - y1 * y4 * x3 + y2 * y4 * x3) / (x2 * y3 * w + y2 * x4 * w - y3 * x4 * w - x2 * y4 * w - y2 * w * x3 + y4 * w * x3);
                float scaleY = (-y1 * x2 * y3 - y1 * y2 * x4 + y1 * y3 * x4 + x1 * y2 * y4 - x1 * y3 * y4 + x2 * y3 * y4 + y1 * y2 * x3 - y2 * y4 * x3) / (x2 * y3 * h + y2 * x4 * h - y3 * x4 * h - x2 * y4 * h - y2 * h * x3 + y4 * h * x3);
                float transY = y1;
                float persp0 = (x1 * y3 - x2 * y3 + y1 * x4 - y2 * x4 - x1 * y4 + x2 * y4 - y1 * x3 + y2 * x3) / (x2 * y3 * w + y2 * x4 * w - y3 * x4 * w - x2 * y4 * w - y2 * w * x3 + y4 * w * x3);
                float persp1 = (-y1 * x2 + x1 * y2 - x1 * y3 - y2 * x4 + y3 * x4 + x2 * y4 + y1 * x3 - y4 * x3) / (x2 * y3 * h + y2 * x4 * h - y3 * x4 * h - x2 * y4 * h - y2 * h * x3 + y4 * h * x3);
                float persp2 = 1.0f;*/


                var mat = new SKMatrix(scaleX, skewX, transX, skewY, scaleY, transY, persp0, persp1, persp2);
                return mat;
            } else {
                var mat2d = new SKMatrix(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
                return mat2d;
            }
        }

        SKMatrix GetLocalTransform(EntityState2D ent, CanvasState rc, Rect aabb) {
            var origin = (new Vector2(0.5f, 0.5f)+ent.anchor)*new Vector2(rc.width, rc.height);
            var translation = origin + ent.position;

            // TODO: AABB center and (0,0) might be misaligned?
            var changePivot = M3x3.Translate_2D(-(new Vector2(aabb.width, aabb.height)*ent.pivot));

            var trs = M3x3.TRS_2D(translation, ent.rot, ent.scale);
            var lt = trs * changePivot;
            //lt.m22 *= -1.0f;
            return lt.ToSKMatrix();
        }

        public void RenderCanvas(CanvasSnapshot css, ref M4x4 worldToClip, bool gizmo) {
            var rc = css.Canvas;
            var canvasToClip = worldToClip * rc.NormalizedCanvasToWorld;
            var mat = GetCanvasMatrix2D(ref canvasToClip, rc);
            // can't create transform (canvas off screen, clipping with near plane etc)
            if(mat == null)
                return;

            var bottomLeft = new Vector4(-0.5f, -0.5f, 0.0f, 1.0f);
            var topRight = new Vector4(0.5f, 0.5f, 0.0f, 1.0f);
            var bottomLeftClip = canvasToClip * bottomLeft;
            bottomLeftClip /= bottomLeftClip.w;
            var topRightClip = canvasToClip * topRight;
            topRightClip /= topRightClip.w;

            if(MathF.Abs(topRightClip.x - bottomLeftClip.x) < 0.001f
                    || MathF.Abs(topRightClip.y - bottomLeftClip.y) < 0.001f)
                return;

            Clear();

            canvas.SetMatrix(mat.Value);
            // gizmo
            if(gizmo && mat != null && !rc.is2d) {
                // draw canvas outline
                using(SKPaint paint = new SKPaint()) {
                    var path = new SKPath();
                    path.MoveTo(new SKPoint(0.0f, 0.0f));
                    path.LineTo(new SKPoint(rc.width, 0.0f));
                    path.LineTo(new SKPoint(rc.width, rc.height));
                    path.LineTo(new SKPoint(0.0f, rc.height));
                    path.Close();
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = 0.0f;
                    paint.Color = SKColors.Black;
                    canvas.DrawPath(path, paint);
                }
            }
            
            foreach(var entitiy in css.Entities) {
                switch(entitiy) {
                case ShapeState shape:
                    if(mode == RenderMode.OpenGL) {
                        ctx.ResetContext();
                    }
                    float bw = glBuffer.Size.Item1;
                    float bh = glBuffer.Size.Item2;
                    
                    using(SKPaint paint = new SKPaint()) {
                        var path = new SKPath();
                        foreach(var verb in shape.path.path) {
                            switch(verb.Item1) {
                                case PathVerb.Move:
                                    path.MoveTo(verb.Item2.points[0].ToSKPoint());
                                    break;
                                case PathVerb.Line:
                                    path.LineTo(verb.Item2.points[1].ToSKPoint());
                                    break;
                                case PathVerb.Quad:
                                    path.QuadTo(verb.Item2.points[1].ToSKPoint(), verb.Item2.points[2].ToSKPoint());
                                    break;
                                case PathVerb.Cubic:
                                    path.CubicTo(verb.Item2.points[1].ToSKPoint(), verb.Item2.points[2].ToSKPoint(), verb.Item2.points[3].ToSKPoint());
                                    break;
                                case PathVerb.Conic:
                                    path.ConicTo(verb.Item2.points[1].ToSKPoint(), verb.Item2.points[2].ToSKPoint(), verb.Item2.conicWeight);
                                    break;
                                case PathVerb.Close:
                                    path.Close();
                                    break;
                                default:
                                    break;
                            }
                        }
                        var bounds = path.TightBounds;
                        var pathSize = new Vector2(bounds.Width, bounds.Height);

                        // calculate transform
                        SKMatrix localTransform = GetLocalTransform(shape, rc, new Rect(0.0f, 0.0f, bounds.Width, bounds.Height));

                        path.Transform(localTransform);

                        // draw fill
                        if(shape.mode == ShapeMode.Filled || shape.mode == ShapeMode.FilledContour) {
                            var c = shape.color.ToSKColor();
                            paint.Color = c;
                            paint.Style = SKPaintStyle.Fill;
                            paint.IsAntialias = true;
                            canvas.DrawPath(path, paint);
                        }
                        // draw contour
                        if(shape.mode == ShapeMode.Contour || shape.mode == ShapeMode.FilledContour) {
                            var c = shape.contourColor.ToSKColor();
                            paint.Color = c;
                            paint.StrokeWidth = shape.contourSize;
                            paint.Style = SKPaintStyle.Stroke;
                            paint.IsAntialias = true;
                            canvas.DrawPath(path, paint);
                        }
                        path.Dispose();


                        /*paint.Typeface = SKTypeface.Default;
                        paint.Style = SKPaintStyle.Fill;
                        paint.StrokeWidth = 0.01f;
                        paint.TextSize = 0.5f;
                        path = paint.GetTextPath("HELLO", 0, 0.0f);
                        path.Transform(localMat);
                        canvas.DrawPath(path, paint);
                        path.Dispose();*/
                    }
                    break;
                case SpriteState sprite:
                    SKBitmap bitmap = null;
                    if(sprite.texture.GLHandle > 0) {
                        bitmap = LoadedImages[sprite.texture.GLHandle];
                    } else {
                        bitmap = LoadTexture(sprite.texture);
                    }
                    if(bitmap != null && mat != null) {

                        //SKMatrix GetLocalTransform(EntityState2D ent, CanvasState rc, Rect aabb) {
                        var curMat = canvas.TotalMatrix;
                        var local = GetLocalTransform(sprite, rc, new Rect(0.0f, 0.0f, sprite.width, sprite.height)).PreConcat(new SKMatrix(1.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f)).PostConcat(curMat);
                        canvas.SetMatrix(local);
                        var rect = new SKRect(-sprite.width/2.0f, -sprite.height/2.0f, sprite.width/2.0f, sprite.height/2.0f);
                        using(var paint = new SKPaint()) {
                            paint.FilterQuality = SKFilterQuality.High;
                            paint.BlendMode = SKBlendMode.SrcOver;
                            canvas.DrawBitmap(bitmap, rect, paint);
                        }
                        canvas.SetMatrix(curMat);
                    }
                    break;
                }
            }
            Flush(css.Canvas.entityId);
        }

        public void Flush(int entityId) {
            if(mode == RenderMode.OpenGL) {
                ctx.ResetContext();
            }
            canvas.Flush();
            // blit software buffer to scren
            if(mode == RenderMode.Software) {
                using(var img = surface.Snapshot()) {
                    if(tex == null) {
                        tex = new Texture2D("SkiaRenderer"); 
                    }
                    tex.Width = img.Width;
                    tex.Height = img.Height;
                    tex.Format = Texture2D.TextureFormat.RGBA8;
                    // allocate new buffer if needed
                    if(tex.RawData == null || tex.RawData.Length != tex.Width * tex.Height * 4) {
                        tex.RawData = new byte[tex.Width * tex.Height * 4];
                    }
                    var pinned = GCHandle.Alloc(tex.RawData, GCHandleType.Pinned);
                    System.IntPtr dst = pinned.AddrOfPinnedObject();
                    if(img.ReadPixels(img.Info, dst)) {
                        var dprb = glBuffer as DepthPeelRenderBuffer;
                        //dprb.BlitTexture(tex);
                        dprb.BlitTextureWithEntityId(tex, entityId);
                    } else {
                        Debug.Error("Failed to read Skia surface pixels");
                    }
                }
            }
            // TODO: blit entityId in OpenGL mode
        }

        void temp() {
            canvas.DrawColor(SKColors.Red);
            canvas.Clear(SKColors.Red);
            using(SKPaint paint = new SKPaint()) {
                paint.Color = SKColors.Blue;
                paint.IsAntialias = true;
                paint.StrokeWidth = 15;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawCircle(100.0f, 100.0f, 50.0f, paint);
            }
            var mat = new SKMatrix44();
            canvas.Flush();
        }
    }
}
