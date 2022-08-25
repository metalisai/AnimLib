using SkiaSharp;
using OpenTK.Graphics.OpenGL4;

namespace AnimLib {

    public static class SkiaExtensions
    {
        public static SKPoint ToSKPoint(this Vector2 v) {
            return new SKPoint(v.x, v.y);
        }
    }

    public class SkiaRenderer
    {
        GRContext ctx;
        GRBackendRenderTarget renderTarget;
        SKSurface surface;
        SKCanvas canvas;
        IRenderBuffer glBuffer;
        GRGlInterface glInterface;

        /*public int TextureId {
            get {
                return glBuffer.Texture();
            }
        }*/

        public void SetBuffer(IRenderBuffer buf) {
            ctx.ResetContext();
            var fbInfo = new GRGlFramebufferInfo((uint)buf.FBO, SKColorType.Rgba8888.ToGlSizedFormat());
            renderTarget = new GRBackendRenderTarget(buf.Size.Item1, buf.Size.Item2, 0, 8, fbInfo);
            surface = SKSurface.Create(ctx, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
            canvas = surface.Canvas;
            glBuffer = buf;
        }

        public void Create() {
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

        public void Clear() {
            ctx.ResetContext();
            canvas.Clear();
        }

        public void RenderShape(ShapeState shape, ref M4x4 worldToClip) {
            ctx.ResetContext();
            var rc = shape.canvas;
            float bw = glBuffer.Size.Item1;
            float bh = glBuffer.Size.Item2;
            SKMatrix localTransform;
            if(!rc.is2d) {
                var canvasToClip = worldToClip * shape.NormalizedCanvasToWorld;
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
                if(bl.w < 0)
                    return;
                float x1 = (0.5f * tl.x + 0.5f) * bw;
                float y1 = (0.5f * tl.y + 0.5f) * bh;
                float x2 = (0.5f * tr.x + 0.5f) * bw;
                float y2 = (0.5f * tr.y + 0.5f) * bh;
                float x3 = (0.5f * br.x + 0.5f) * bw;
                float y3 = (0.5f * br.y + 0.5f) * bh;
                float x4 = (0.5f * bl.x + 0.5f) * bw;
                float y4 = (0.5f * bl.y + 0.5f) * bh;
                float w = rc.width;
                float h = rc.height;

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

                // bootom left
                float persp0 = (x1*y3 - x3*y1 - x1*y4 - x2*y3 + x3*y2 + x4*y1 + x2*y4 - x4*y2)/(w*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float persp1 = -(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y4 - x4*y2 - x3*y4 + x4*y3)/(h*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float persp2 = 1.0f;
                float scaleX = (x1*x3*y2 - x2*x3*y1 - x1*x4*y2 + x2*x4*y1 - x1*x3*y4 + x1*x4*y3 + x2*x3*y4 - x2*x4*y3)/(w*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float scaleY = (x2*y1*y3 - x3*y1*y2 - x1*y2*y4 + x4*y1*y2 + x1*y3*y4 - x4*y1*y3 - x2*y3*y4 + x3*y2*y4)/(h*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float skewX = (x1*x2*y3 - x1*x3*y2 - x1*x2*y4 + x2*x4*y1 + x1*x3*y4 - x3*x4*y1 - x2*x4*y3 + x3*x4*y2)/(h*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float skewY = (x1*y2*y3 - x2*y1*y3 - x1*y2*y4 + x2*y1*y4 - x3*y1*y4 + x4*y1*y3 + x3*y2*y4 - x4*y2*y3)/(w*(x1*y2 - x2*y1 - x1*y3 + x3*y1 + x2*y3 - x3*y2));
                float transX = x4;
                float transY = y4;

                var mat = new SKMatrix(scaleX, skewX, transX, skewY, scaleY, transY, persp0, persp1, persp2);
                canvas.SetMatrix(mat);
                localTransform = new SKMatrix(1.0f, 0.0f, 0.5f*w, 0.0f, -1.0f, 0.5f*h, 0.0f, 0.0f, 1.0f);
            } else {
                float tx = rc.width*(0.5f + shape.anchor.x);
                float ty = rc.height*(0.5f + shape.anchor.y);
                var mat = new SKMatrix(1.0f, 0.0f, tx, 0.0f, 1.0f, ty, 0.0f, 0.0f, 1.0f);
                canvas.SetMatrix(mat);
                localTransform = new SKMatrix(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
            }
            // TODO: acnhor
            
            using(SKPaint paint = new SKPaint()) {
                var c = shape.color;
                paint.Color = new SKColor(c.r, c.g, c.b, c.a);
                paint.StrokeWidth = 1.05f;
                paint.Style = SKPaintStyle.Stroke;
                paint.IsAntialias = true;
                var path = new SKPath();
                if(shape.path.Length > 0)
                    path.MoveTo(shape.path[0].ToSKPoint());
                foreach(var v in shape.path) {
                    path.LineTo(v.ToSKPoint());
                }
                path.Close();
                path.Transform(localTransform);
                canvas.DrawPath(path, paint);

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
        }

        public void Flush() {
            ctx.ResetContext();
            canvas.Flush();
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
