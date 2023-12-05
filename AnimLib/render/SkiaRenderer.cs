using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Svg.Skia;
using SkiaSharp;
using System.IO;
using System.Xml;
using System.Xml.Linq;

using CanvasProperties = (string Name, (string Name, object Value)[] Properties);

namespace AnimLib;

/// <summary>
/// A canvas renderer using SkiaSharp.
/// </summary>
internal partial class SkiaRenderer
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
    int svgId = 0;
    Dictionary<int, SKImage> LoadedImages = new ();
    Dictionary<int, SKSvg> LoadedSvgs = new ();

    RenderMode mode;
    bool hdr;

    int width;
    int height;

    GRContext? ctx;
    GRBackendRenderTarget? renderTarget;
    SKSurface? surface;
    SKCanvas? canvas;
    IBackendRenderBuffer? glBuffer; // intermediate buffer for hw rendering
    GRGlInterface? glInterface;

    Texture2D? tex;

    IPlatform platform;

    TextPlacement textPlacement;

    public SkiaRenderer(IPlatform platform) {
        this.platform = platform;
        textPlacement = new TextPlacement("/usr/share/fonts/truetype/ubuntu/Ubuntu-M.ttf", "Ubuntu");
    }

    private SKImage? LoadTexture(Texture2D texture) {
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
        SKImage image = SKImage.FromPixelCopy(info, texture.RawData);
        LoadedImages.Add(handle, image);
        texture.GLHandle = handle;
        Debug.Log($"Loaded texture {texture.Width}x{texture.Height} format {texture.Format}");
        return image;
    }

    private SKSvg LoadSvg(SvgData svg) {
        int handle = svg.GetHashCode();
        SKSvg newSvg = new SKSvg();
        var data = System.Text.Encoding.UTF8.GetBytes(svg.svg);
        var stream = new MemoryStream();
        stream.Write(data);
        stream.Flush();
        stream.Position = 0;
        newSvg.Load(stream);
        svg.handle = handle;
        LoadedSvgs.Add(handle, newSvg);
        Debug.Log($"Loaded SVG with length {svg.svg.Length}.");
        return newSvg;
    }

    public void SetBuffer(IBackendRenderBuffer buf) {
        width = buf.Size.Item1;
        height = buf.Size.Item2;
        if(mode == RenderMode.OpenGL) {
            var colorType = this.hdr ? SKColorType.RgbaF16 : SKColorType.Rgba8888;
            // TODO: don't need a buffer this complex (only need color and stencil)
            if(glBuffer == null) {
                glBuffer = new DepthPeelRenderBuffer(platform, platform.PresentedColorSpace);
            }
            if(buf.Size.Item1 != glBuffer.Size.Item1 || buf.Size.Item2 != glBuffer.Size.Item2) {
                glBuffer.Resize(buf.Size.Item1, buf.Size.Item2);
                Debug.Log($"Resize Skia GL buffer to {buf.Size.Item1}x{buf.Size.Item2}");
            }
            // tell skia that gl context was modified
            ctx?.ResetContext();
            var fbInfo = new GRGlFramebufferInfo((uint)glBuffer.FBO, colorType.ToGlSizedFormat());
            renderTarget = new GRBackendRenderTarget(buf.Size.Item1, buf.Size.Item2, 0, 8, fbInfo);
            surface = SKSurface.Create(ctx, renderTarget, GRSurfaceOrigin.TopLeft, colorType);
            canvas = surface.Canvas;
        } else if(mode == RenderMode.Software) {
            // NOTE: F16 is extremely slow in software mode
            var colorType = this.hdr ? SKColorType.RgbaF16 : SKColorType.Rgba8888;
            // in software mode render to image then blit to renderbuffer
            var imageInfo = new SKImageInfo(
                width: buf.Size.Item1,
                height: buf.Size.Item2,
                colorType: colorType,
                alphaType: SKAlphaType.Premul);
            surface = SKSurface.Create(imageInfo);
            canvas = surface.Canvas;
            renderTarget = null;
        } else {
            Debug.Error($"Unknown mode {mode}");
        }
    }

    // OpenGL rendering
    public void CreateGL(bool hdr) {
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
        this.hdr = hdr;
        /*var fbInfo = new GRGlFramebufferInfo((uint)glBuffer.FBO, SKColorType.Rgba8888.ToGlSizedFormat());
        renderTarget = new GRBackendRenderTarget(1920, 1080, 0, 8, fbInfo);
        surface = SKSurface.Create(ctx, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
        canvas = surface.Canvas;*/
    }

    // Software rendering
    public void CreateSW(bool hdr) {
        if(mode != RenderMode.None) {
            Debug.Error($"CreateSW() called after already initialized, mode: {mode}");
            return;
        }
        mode = RenderMode.Software;
        glInterface = null;
        ctx = null;
        this.hdr = hdr;
    }

    public void Clear() {
        ctx?.ResetContext();
        canvas?.Clear(SKColors.Transparent);
    }

    SKMatrix? GetCanvasMatrix2D(ref M4x4 canvasToClip, out SKRect clipRegion, CanvasState canvas, (float w, float h) bufferSize) {
        if(!canvas.is2d) {
            float bw = this.width;
            float bh = this.height;
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
            {
                clipRegion = new SKRect(0.0f, 0.0f, bufferSize.w, bufferSize.h);
                return null;
            }
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

            // Skia checks if the matrix has perspective transform and if it does, it will make rendering very slow
            // Due to rounding errors it might think that the matrix has perspective transform even if it doesn't
            // So we round the values to 0.0f or 1.0f if they are close enough
            const float epsilon = 1e-8f;
            bool isPersp0 = MathF.Abs(persp0 - 0.0f) > epsilon;
            bool isPersp1 = MathF.Abs(persp1 - 0.0f) > epsilon;
            bool isPersp2 = MathF.Abs(persp2 - 1.0f) > epsilon;
            if (!isPersp0 && !isPersp1 && !isPersp2)
            {
                persp0 = 0.0f;
                persp1 = 0.0f;
                persp2 = 1.0f;
            }

            var mat = new SKMatrix(scaleX, skewX, transX, skewY, scaleY, transY, persp0, persp1, persp2);
            // TODO: 3D clip region is a polygon, not a rectangle
            clipRegion = new SKRect(0, 0, bufferSize.w, bufferSize.h);
            return mat;
        } else {
            float tX = canvas.center.x;
            float tY = canvas.center.y;
            var mat2d = new SKMatrix(1.0f, 0.0f, tX, 0.0f, 1.0f, tY, 0.0f, 0.0f, 1.0f);
            float oX = bufferSize.w/2.0f + canvas.center.x;
            float oY = bufferSize.h/2.0f + canvas.center.y;
            clipRegion = new SKRect(oX - canvas.width/2.0f, oY - canvas.height/2.0f, oX + canvas.width/2.0f, oY + canvas.height/2.0f);
            return mat2d;
        }
    }

    M3x3 getModelMatrix(EntityState2D ent, CanvasState rc, EntityState2D[] entities, (float w, float h) bufferSize) {
        M3x3? parentMat = null;
        if(ent.parentId > 0) {
            // TODO: optimize
            var parent = entities.Where(x => x.entityId == ent.parentId).FirstOrDefault();
            if(parent != null) {
                parentMat = getModelMatrix(parent, rc, entities, bufferSize);
            } else {
                Debug.Warning($"Parent set ({ent.parentId}) but it was not part of the canvas");
            }
        }

        Vector2 origin;
        if(ent.parentId <= 0) { // has no parent
            float w = bufferSize.w;
            float h = bufferSize.h;
            origin = (new Vector2(0.5f, 0.5f)+ent.anchor)*new Vector2(w, h);
        } else {
            origin = Vector2.ZERO;
        }
        var translation = origin + ent.position;
        var trs = M3x3.TRS_2D(translation, ent.rot, ent.scale);
        if(parentMat != null) {
            trs =parentMat.Value * trs;
        }
        return trs;
    }

    SKMatrix GetLocalTransform(EntityState2D ent, CanvasState rc, Rect aabb, EntityState2D[] entities, (float, float) bufferSize) {
        // TODO: AABB center and (0,0) might be misaligned?
        var changePivot = M3x3.Translate_2D(-(new Vector2(aabb.width, aabb.height)*ent.pivot));
        var trs = getModelMatrix(ent, rc, entities, bufferSize);
        var lt = trs * changePivot;
        //lt.m22 *= -1.0f;
        var ret = lt.ToSKMatrix();
        if (ent.homography != null)
        {
            ret = ret.PreConcat(ent.homography.Value.ToSKMatrix());
        }
        return ret;
    }

    public void RenderCanvas(CanvasSnapshot css, ref M4x4 worldToClip, bool gizmo, IBackendRenderBuffer rb) {
        using var _ = new Performance.Call("SkiaRenderer.RenderCanvas");
        var rc = css.Canvas;
        var canvasToClip = worldToClip * rc.NormalizedCanvasToWorld;
        var mat = GetCanvasMatrix2D(ref canvasToClip, out var clipRegion, rc, rb.Size);
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

        // NOTE: ctx.ResetContext() will be called here
        Clear();

        canvas?.Save();
        int restoreCount = 1;

        T getValue<T>(CanvasProperties test, string name) {
            return (T)test.Properties.First(x => x.Name == name).Value;
        }

        foreach(var eff in css.Effects) {
            // NOTE: using typeof to make sure that if someone changes the name of the effect, it can be caught at compile time
            if (eff.Name == typeof (CanvasBlurEffect).Name) {
                using var _x = new Performance.Call("SkiaRenderer.RenderCanvas.Blur");
                float radiusX = getValue<float>(eff, "radiusX");
                float radiusY = getValue<float>(eff, "radiusY");
                using var filter = SKImageFilter.CreateBlur(radiusY, radiusY);
                using var paint = new SKPaint() { ImageFilter = filter };
                canvas?.SaveLayer(paint);
                restoreCount++;
            }
            else if (eff.Name == typeof (CanvasDilateEffect).Name) {
                using var _x = new Performance.Call("SkiaRenderer.RenderCanvas.Dilate");
                float radiusX = getValue<float>(eff, "radiusX");
                float radiusY = getValue<float>(eff, "radiusY");
                using var filter = SKImageFilter.CreateDilate(radiusX, radiusY);
                using var paint = new SKPaint() { ImageFilter = filter };
                canvas?.SaveLayer(paint);
                restoreCount++;
            }
        }

        canvas?.ClipRect(clipRegion, SKClipOperation.Intersect);

        canvas?.SetMatrix(mat.Value);
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
                canvas?.DrawPath(path, paint);
                path.Dispose();
            }
        }

        var confPaint = (SKPaint paint) => {
            paint.IsAntialias = true;
            paint.SubpixelText = true;
            paint.IsAutohinted = true;
        };

        var renderShape = (SKPaint paint, SKPath path, EntityState2D shape, ShapeMode mode, Color color, Color contourColor, float contourSize) => {
            using var _ = new Performance.Call("SkiaRenderer.RenderShape");

            var bounds = path.TightBounds;
            SKMatrix localTransform = GetLocalTransform(shape, rc, new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height), css.Entities, rb.Size);

            var realShape = shape as ShapeState;
            if (realShape != null && realShape.trim != (0.0f, 1.0f))
            {
                paint.PathEffect = SKPathEffect.CreateTrim(realShape.trim.Item1, realShape.trim.Item2);
            }

            path.Transform(localTransform);

            // draw fill
            if(mode == ShapeMode.Filled || mode == ShapeMode.FilledContour) {
                using var _aa = new Performance.Call("SkiaRenderer.RenderShape.Fill");
                var c = color.ToSKColorF();
                paint.ColorF = c;
                paint.Style = SKPaintStyle.Fill;
                canvas?.DrawPath(path, paint);
            }
            // draw contour
            if(mode == ShapeMode.Contour || mode == ShapeMode.FilledContour) {
                using var _a = new Performance.Call("SkiaRenderer.RenderShape.Contour");
                var c = contourColor.ToSKColorF();
                paint.ColorF = c;
                paint.StrokeWidth = contourSize;
                paint.Style = SKPaintStyle.Stroke;
                canvas?.DrawPath(path, paint);
            }
            path.Dispose();
            if (paint.PathEffect != null)
            {
                paint.PathEffect.Dispose();
                paint.PathEffect = null;
            }
        };
        
        foreach(var entitiy in css.Entities) {
            float bw = this.width;
            float bh = this.height;
            switch(entitiy) {
            case MorphShapeState morphShape:
                using (SKPaint paint = new SKPaint()) {
                    var path1 = CubicSpline.FromShape(morphShape.shape1);
                    var path2 = CubicSpline.FromShape(morphShape.shape2);
                    //var morph = path1[0].MorphTo(path2[0], morphShape.progress);
                    var morphs = CubicSpline.MorphCollection(path1, path2, morphShape.progress);
                    //var path = morph.ToShapePath().ToSKPath();
                    var path = CubicSpline.CollectionToShapePath(morphs).ToSKPath();
                    confPaint(paint);
                    renderShape(paint, path, morphShape, morphShape.CurrentMode, morphShape.CurrentColor, morphShape.CurrentContourColor, morphShape.CurrentContourSize);
                }
                break;
            case ShapeState shape:
                using(SKPaint paint = new SKPaint()) {
                    var path = shape.path.ToSKPath();
                    confPaint(paint);
                    // calculate transform
                    // NOTE: Skia's top is bottom in our renderer
                    renderShape(paint, path, shape, shape.mode, shape.color, shape.contourColor, shape.contourSize);
                }
                break;
            case SpriteState sprite:
                SKImage? image = null;
                if(sprite.texture.GLHandle > 0) {
                    image = LoadedImages[sprite.texture.GLHandle];
                } else {
                    image = LoadTexture(sprite.texture);
                }
                if(image != null && mat != null) {

                    //SKMatrix GetLocalTransform(EntityState2D ent, CanvasState rc, Rect aabb) {
                    if (canvas is not SKCanvas c2) {
                        Debug.Error("SkiaRenderer: canvas matrix is null");
                        break;
                    }
                    var curMat = canvas.TotalMatrix;
                    var local = GetLocalTransform(sprite, rc, new Rect(0.0f, 0.0f, sprite.width, sprite.height), css.Entities, rb.Size).PreConcat(new SKMatrix(1.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f)).PostConcat(curMat);
                    canvas?.SetMatrix(local);
                    var rect = new SKRect(-sprite.width/2.0f, -sprite.height/2.0f, sprite.width/2.0f, sprite.height/2.0f);
                    using(var paint = new SKPaint()) {
                        paint.FilterQuality = SKFilterQuality.High;
                        paint.BlendMode = SKBlendMode.SrcOver;
                        paint.ColorF = sprite.color.ToSKColorF();
                        c2.DrawImage(image, rect, paint);
                    }
                    c2.SetMatrix(curMat);
                }
                break;
            case SvgSpriteState svgsprite:
                SKSvg? svg = null;
                if(svgsprite.svg.handle > 0) {
                    svg = LoadedSvgs[svgsprite.svg.handle];
                } else {
                    svg = LoadSvg(svgsprite.svg);
                }
                if(svg != null && mat != null && svg.Picture != null) {
                    var bounds = svg.Picture.CullRect;
                    var curMat = canvas.TotalMatrix;
                    var rect = new SKRect(-svgsprite.width/2.0f, -svgsprite.height/2.0f, svgsprite.width/2.0f, svgsprite.height/2.0f);
                    float scaleX = 1.0f, scaleY = 1.0f;
                    float aspect = bounds.Width / bounds.Height;
                    if (svgsprite.width > 0.0f) {
                        scaleX = svgsprite.width / bounds.Width;
                    }
                    if (svgsprite.height > 0.0f) {
                        scaleY = svgsprite.height / bounds.Height;
                    }

                    if (svgsprite.width > 0.0f && svgsprite.height <= 0.0f) {
                        scaleY = scaleX;
                    }
                    else if (svgsprite.height > 0.0f && svgsprite.width <= 0.0f) {
                        scaleX = scaleY;
                    }
                    float tX = -bounds.Width*scaleX/2.0f;
                    float tY = bounds.Height*scaleY/2.0f;

                    var preMat = new SKMatrix(scaleX, 0.0f, tX, 0.0f, -scaleY, tY, 0.0f, 0.0f, 1.0f);
                    var local = GetLocalTransform(svgsprite, rc, new Rect(0.0f, 0.0f, svgsprite.width, svgsprite.height), css.Entities, rb.Size).PreConcat(preMat).PostConcat(curMat);
                    canvas.SetMatrix(local);

                    // TODO: scale svg to fit rect
                    using(var paint = new SKPaint()) {
                        paint.BlendMode = SKBlendMode.SrcOver;
                        paint.ColorF = svgsprite.color.ToSKColorF();
                        canvas.DrawPicture(svg.Picture, paint);
                    }
                    canvas.SetMatrix(curMat);
                }
                break;
            }
        }

        for (int i = 0; i < restoreCount; i++)
        {
            canvas.Restore();
        }
        {
            using var aaa = new Performance.Call("SkiaRenderer.RenderText");
            Flush(css.Canvas.entityId);
        }
    }

    int _texture = -1;
    public int Texture{
        get {
            return _texture;
        }
        set {
            _texture = value;
        }
    }

    public void Flush(int entityId) {
        ctx?.ResetContext();
        canvas.Flush();

        // blit software buffer to scren
        if(mode == RenderMode.Software) {
            using(var img = surface.Snapshot()) {
                if(tex == null) {
                    tex = new Texture2D("SkiaRenderer"); 
                }
                var requiredSize = img.Info.BytesSize;
                tex.GenerateMipmap = false;
                tex.Width = img.Width;
                tex.Height = img.Height;
                //tex.Format = Texture2D.TextureFormat.RGBA8;
                tex.Format = this.hdr ? Texture2D.TextureFormat.RGBA16F : Texture2D.TextureFormat.RGBA8;
                // allocate new buffer if needed
                if(tex.RawData == null || tex.RawData.Length != tex.Width * tex.Height * 4) {
                    tex.RawData = new byte[requiredSize];
                }
                if(tex.RawData.Length < requiredSize) {
                    Debug.Error($"Skia buffer too small, required {requiredSize} bytes, got {tex.RawData.Length}");
                    return;
                }
                var pinned = GCHandle.Alloc(tex.RawData, GCHandleType.Pinned);
                System.IntPtr dst = pinned.AddrOfPinnedObject();
                if(img.ReadPixels(img.Info, dst)) {
                    platform.LoadTexture(tex);
                    _texture = tex.GLHandle;
                } else {
                    Debug.Error("Failed to read Skia surface pixels");
                }
            }
        }
        // TODO: blit entityId in OpenGL mode
        else if(mode == RenderMode.OpenGL) {
            var src = glBuffer as DepthPeelRenderBuffer;
            if(src == null) {
                Debug.Error("SkiaRenderer: glBuffer is not DepthPeelRenderBuffer");
                return;
            }
            _texture = src.Texture();
        }
    }
}
