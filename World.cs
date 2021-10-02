using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnimLib
{
    public enum LabelStyle {
        None,
        Horizontal, // only horizontal orientation allowed
        Free, // can have any orientation
    }
    public interface Labelable {
        Vector2? GetLabelOffset(CameraState cam, Rect label, LabelStyle style, EntityState state, Vector2 screenSize);
        Vector3? GetLabelWorldCoordinate(LabelStyle style, EntityState state);
    }

    public class AbsorbDestruction {
        public int entityId;
        public Vector3? point;
        public Vector3? screenPoint;
        public float duration;
        public float progress;
    }

    public interface IColored {
        Color Color { get; set; }
    }

    public interface RendererResource {
        string GetOwnerGuid();
    }
    public class WorldResources : IDisposable {
        public WorldResources() {
            Debug.Log("New world resources " + GetGuid());
        }
        public List<ColoredTriangleMeshGeometry> MeshGeometries = new List<ColoredTriangleMeshGeometry>();
        public List<Texture2D> Textures = new List<Texture2D>();
        public List<MeshBackedGeometry> MeshBackedGeometries = new List<MeshBackedGeometry>();

        string hash = Guid.NewGuid().ToString();

        public string GetGuid() {
            return hash;
        }

        /*public void DestroyRendererResource(RendererResource resource) {
            resource.Destroy(renderState);
            if(resource is ColoredTriangleMeshGeometry) {
                MeshGeometries.Remove(resource as ColoredTriangleMeshGeometry);
            } else if(resource is Texture2D) {
                Textures.Remove(resource as Texture2D);
            } else if(resource is MeshBackedGeometry) {
                MeshBackedGeometries.Remove(resource as MeshBackedGeometry);
            }
        }*/

        public void Dispose()
        {
            MeshGeometries = null;
            Textures = null;
            MeshBackedGeometries = null;
            // make sure renderer knows that everything we allocated is no longer needed
            RenderState.destroyedOwners.Add(hash);
            Debug.Log("World resources destroyed " + GetGuid());
        }
    }
    public class Texture2D : RendererResource {
        public enum TextureFormat {
            None,
            RGBA8,
            RGB8,
            ARGB8,
            BGR8,
            R8,
        }
        public byte[] RawData;
        public TextureFormat Format;
        public int Width;
        public int Height;
        public int GLHandle = -1;
        public int Alignment = 4;
        public string ownerGuid = null;

        public Texture2D(string guid) {
            this.ownerGuid = guid;
        }

        public string GetOwnerGuid() {
            return ownerGuid;
        }

        public Color GetPixel(int x, int y) {
            byte blue, green, red, alpha;
            int offset;
            switch(this.Format) {
                case TextureFormat.BGR8:
                int alignmentBytes = (Alignment - (Width*3)%Alignment)%Alignment;
                offset = y*(Width*3+alignmentBytes) + x*3;
                blue = RawData[offset];
                green = RawData[offset+1];
                red = RawData[offset+2];
                alpha = 0xFF;
                break;
                case TextureFormat.ARGB8:
                // TODO: this is defnitely wrong!
                offset = y*Width*4 + (Alignment - ((y*Width*4)%Alignment)) + x*4;
                red = RawData[offset+1];
                green = RawData[offset+2];
                blue = RawData[offset+3];
                alpha = RawData[offset];
                break;
                default:
                throw new NotImplementedException();
            }
            return new Color(red, green, blue, alpha);
        }
    }

    public class CubeState : EntityState
    {
        public Color color = Color.WHITE;

        public CubeState() {
        }

        public CubeState(CubeState c) : base(c) {
            this.color = c.color;
        }

        public override object Clone()
        {
            return new CubeState(this);
        }
    }
    public class Cube : VisualEntity/*, ICloneable*/ {
        public Color Color { 
            get {
                return ((CubeState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((CubeState)state).color);
                ((CubeState)state).color = value;
            }
        }

        public Cube() {
            state = new CubeState();
        }

        public Cube(Cube cube) : base(cube) {
        }

        public override object Clone() {
            return new Cube(this);
        }
    }

    public class ColoredTriangleMeshGeometry : RendererResource {
        public Vector3[] vertices;
        public uint[] indices;
        public Color[] colors;
        public Vector2[] edgeCoordinates;
        public int VAOHandle = -1;
        public int VBOHandle = -1;
        public int EBOHandle = -1;
        public bool Dirty = true;
        // used by renderer to know who owns the resource (to know what can be deallocated)
        public string ownerGuid;

        public ColoredTriangleMeshGeometry(string ownerGuid) {
            this.ownerGuid  = ownerGuid;
        }

        public string GetOwnerGuid()
        {
            return ownerGuid;
        }
    }

    public class ColoredTriangleMesh/* : VisualEntity*/ {
        public RenderState.BuiltinShader Shader = RenderState.BuiltinShader.LineShader;
        public Color Tint = Color.WHITE;
        /*public Color Outline = Color.BLACK;
        public float OutlineWidth = 0.0f;*/
        public M4x4 modelToWorld;
        public ColoredTriangleMeshGeometry Geometry;
        public List<(string, object)> shaderProperties = new List<(string, object)>();
        public bool is2d = false;
        public int entityId = -1;
    }

    public class RendererHandle {
        public ColoredTriangleMeshGeometry Handle;
    }

    public abstract class MeshBackedGeometry : EntityState/*, ICloneable*/, RendererResource {
        //public Transform2 Transform;
        //public Color Outline = Color.BLACK;
        //public float OutlineWidth = 0.0f;
        public RenderState.BuiltinShader Shader = RenderState.BuiltinShader.LineShader;

        public readonly RendererHandle RendererHandle = new RendererHandle();
        /*public abstract object Clone();*/
        public abstract void UpdateMesh(ColoredTriangleMeshGeometry mesh);
        public List<(string, object)> shaderProperties = new List<(string, object)>();
        public string ownerGuid;

        public MeshBackedGeometry(string ownerGuid) {
            this.ownerGuid = ownerGuid;
        }

        public MeshBackedGeometry(MeshBackedGeometry mbg) : base(mbg) {
            this.ownerGuid = mbg.ownerGuid;
            this.Shader = mbg.Shader;
            this.shaderProperties = mbg.shaderProperties;
        }

        public string GetOwnerGuid() {
            return ownerGuid;
        }

        protected MeshBackedGeometry(RendererHandle handle, string ownerGuid) {
            RendererHandle = handle;
            this.ownerGuid = ownerGuid;
        }
    }

    public class MeshState : MeshBackedGeometry
    {
        bool dirty = true;
        Vector3[] _vertices;
        public Vector3[] vertices {
            get {
                return _vertices;
            }
            set {
                _vertices = value;
                dirty = true;
            }
        }
        uint[] _indices;
        public uint[] indices {
            get {
                return _indices;
            }
            set {
                _indices = value;
                dirty = true;
            }
        }
        Color _color;
        public Color color {
            get {
                return _color;
            } 
            set {
                _color = value;
                dirty = true;
            }
        }

        public MeshState(string owner) : base(owner) {
            this.Shader = RenderState.BuiltinShader.MeshShader;
        }

        public MeshState(MeshState ms) : base(ms) {
            this.vertices = ms.vertices.ToArray();
            this.indices = ms.indices.ToArray();
            this.color = ms.color;
        }

        public override object Clone() {
            return new MeshState(this);
        }

        public override void UpdateMesh(ColoredTriangleMeshGeometry mesh) {
            mesh.Dirty = dirty;
            if(dirty) {
                mesh.vertices = vertices;
                mesh.indices = indices;
                mesh.colors = vertices.Select(x => color).ToArray();
                dirty = false;
            }
        }
    }

    public class Mesh : VisualEntity
    {
        public Color Color {
            get {
                return ((MeshState)state).color;
            }
            set {
                World.current.SetProperty(this, "StartPoint", value, ((MeshState)state).color);
                ((MeshState)state).color = value;
            }
        }

        public Vector3[] Vertices {
            get {
                return ((MeshState)state).vertices;
            }
            set {
                World.current.SetProperty(this, "Vertices", value, ((MeshState)state).vertices);
                ((MeshState)state).vertices = value;
            }
        }

        public uint[] Indices {
            get {
                return ((MeshState)state).indices;
            }
            set {
                World.current.SetProperty(this, "Indices", value, ((MeshState)state).indices);
                ((MeshState)state).indices = value;
            }
        }

        public Mesh(string owner) : base() {
            this.state = new MeshState(owner);
        }

        public Mesh() : this(World.current.Resources.GetGuid()) {
        }

        public Mesh(Mesh mesh) : base(mesh) {
        }

        public override object Clone() {
            return new Mesh(this);
        }
    }


    public class ArrowState : MeshBackedGeometry
    {
        public Vector3 startPoint;
        public Vector3 endPoint;
        public Color startColor = Color.BLACK;
        public Color endColor = Color.BLACK;
        public float width = 1.0f;
        public float outlineWidth = 1.0f;
        public Color outline = Color.BLACK;

        public override object Clone()
        {
            return new ArrowState(this);
        }

        public ArrowState(ArrowState ars) : base(ars) {
            this.startPoint = ars.startPoint;
            this.endPoint = ars.endPoint;
            this.startColor = ars.startColor;
            this.endColor = ars.endColor;
            this.width = ars.width;
            this.outlineWidth = ars.outlineWidth;
            this.outline = ars.outline;
        }

        public ArrowState(string owner) : base(owner) {
            this.Shader = RenderState.BuiltinShader.ArrowShader;
            Func<float> len = () => {
                return (startPoint-endPoint).Length;
            };
            this.shaderProperties.Add(("Length", len));
            Func<float> width = () => {
                return this.width;
            };
            Func<Vector4> outlineColor = () => {
                Vector4 ret = new Vector4(outline.r/255.0f, outline.g/255.0f, outline.b/255.0f, outlineWidth);
                return ret;
            };
            this.shaderProperties.Add(("Width", width));
            this.shaderProperties.Add(("_Outline", outlineColor));
        }

        public ArrowState(RendererHandle h, string owner) : base(h, owner) {
            this.Shader = RenderState.BuiltinShader.ArrowShader;
            Func<float> len = () => {
                return (startPoint-endPoint).Length;
            };
            this.shaderProperties.Add(("Length", len));
            Func<float> width = () => {
                return this.width;
            };
            this.shaderProperties.Add(("Width", width));
        }

        public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
        {
            Vector2 dir = (endPoint - startPoint).Normalized;
            // We render 2d distance field (union of triangle and rectangle) within a rectangle
            float z = endPoint.z;
            var v1 = (Vector2)endPoint + dir.PerpCcw*width*1f;
            var v2 = (Vector2)startPoint + dir.PerpCcw*width*1f;
            var v3 = (Vector2)startPoint + dir.PerpCw*width*1f;
            var v4 = (Vector2)endPoint + dir.PerpCw*width*1f;
            mesh.vertices = new Vector3[] {
                new Vector3(v1, z),
                new Vector3(v2, z),
                new Vector3(v3, z),
                new Vector3(v4, z),
            };
            mesh.colors = new Color[] {endColor, startColor, startColor, endColor};
            mesh.indices = new uint[]{0, 3, 2, 0, 1, 2};
            mesh.edgeCoordinates = new Vector2[] {
                new Vector2(1.0f, 0.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
            };
            mesh.Dirty = true;
        }
    }

    public class Arrow2D : VisualEntity
    {
        public Vector3 StartPoint {
            get {
                return ((ArrowState)state).startPoint;
            }
            set {
                World.current.SetProperty(this, "StartPoint", value, ((ArrowState)state).startPoint);
                ((ArrowState)state).startPoint = value;
            }
        }
        public Vector3 EndPoint {
            get {
                return ((ArrowState)state).endPoint;
            }
            set {
                World.current.SetProperty(this, "EndPoint", value, ((ArrowState)state).endPoint);
                ((ArrowState)state).endPoint = value;
            }
        }
        public Color StartColor {
            get {
                return ((ArrowState)state).startColor;
            }
            set {
                World.current.SetProperty(this, "StartColor", value, ((ArrowState)state).startColor);
                ((ArrowState)state).startColor = value;
            }
        }
        public Color EndColor {
            get {
                return ((ArrowState)state).endColor;
            }
            set {
                World.current.SetProperty(this, "EndColor", value, ((ArrowState)state).endColor);
                ((ArrowState)state).endColor = value;
            }
        }
        public Color Color {
            set {
                StartColor = value;
                EndColor = value;
            }
            get {
                return StartColor;
            }
        }
        public float Width {
            get {
                return ((ArrowState)state).width;
            }
            set {
                World.current.SetProperty(this, "Width", value, ((ArrowState)state).width);
                ((ArrowState)state).width = value;
            }
        }

        public Arrow2D(string owner) : base() {
            this.state = new ArrowState(owner);
        }

        public Arrow2D() : this(World.current.Resources.GetGuid()) {
        }

        public Arrow2D(Arrow2D arrow) : base(arrow) {
        }

        public override object Clone() {
            return new Arrow2D(this);
        }
    }

    public class SolidFunctionCurveState : MeshBackedGeometry {
        public Vector2[] handles;
        public Color color;
        public float width;
        public float startX;
        public float endX;
        public Func<float,float> func;
        public int segments = 10;

        public SolidFunctionCurveState(Func<float,float> f, string owner) : base(owner) {
            func = f;
        }

        public SolidFunctionCurveState(Func<float,float> f, RendererHandle handle, string owner) : base(handle, owner) {
            func = f;
        }

        public SolidFunctionCurveState(RendererHandle handle, string owner) : base(handle, owner) { }

        public SolidFunctionCurveState(SolidFunctionCurveState sfcs) : base(sfcs) {
            this.handles = sfcs.handles.ToArray();
            this.color = sfcs.color;
            this.width = sfcs.width;
            this.startX = sfcs.startX;
            this.endX = sfcs.endX;
            this.func = sfcs.func;
            this.segments = sfcs.segments;
        }

        public override object Clone()
        {
            return new SolidFunctionCurveState(this);
        }

        public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
        {
            Vector3[] segs = new Vector3[segments+1];
            float start = startX;
            float step  = (endX-startX)/(float)segments;
            for(int i = 0; i < segs.Length; i++) {
                float x = start + i*step;
                segs[i] = new Vector3(x, func(x), 0.0f);
            }
            LineRenderer.UpdateLineMesh(mesh, segs, this.width, this.color, ownerGuid);
        }
    }

    public class SolidFunctionCurve : VisualEntity {
        public Vector2[] Handles {
            get {
                return ((SolidFunctionCurveState)state).handles;
            } set {
                World.current.SetProperty(this, "Handles", value, ((SolidFunctionCurveState)state).handles);
                ((SolidFunctionCurveState)state).handles = value;
            }
        }
        public Color Color {
            get {
                return ((SolidFunctionCurveState)state).color;
            } set {
                World.current.SetProperty(this, "Color", value, ((SolidFunctionCurveState)state).color);
                ((SolidFunctionCurveState)state).color = value;
            }
        }
        public float Width {
            get {
                return ((SolidFunctionCurveState)state).width;
            } set {
                World.current.SetProperty(this, "Width", value, ((SolidFunctionCurveState)state).width);
                ((SolidFunctionCurveState)state).width = value;
            }
        }
        public float StartX {
            get {
                return ((SolidFunctionCurveState)state).startX;
            } set {
                World.current.SetProperty(this, "StartX", value, ((SolidFunctionCurveState)state).startX);
                ((SolidFunctionCurveState)state).startX = value;
            }
        }
        public float EndX {
            get {
                return ((SolidFunctionCurveState)state).endX;
            } set {
                World.current.SetProperty(this, "EndX", value, ((SolidFunctionCurveState)state).endX);
                ((SolidFunctionCurveState)state).endX = value;
            }
        }
        public Func<float,float> Func {
            get {
                return ((SolidFunctionCurveState)state).func;
            } set {
                World.current.SetProperty(this, "Func", value, ((SolidFunctionCurveState)state).func);
                ((SolidFunctionCurveState)state).func = value;
            }
        }
        public int Segments {
            get {
                return ((SolidFunctionCurveState)state).segments;
            } set {
                World.current.SetProperty(this, "Segments", value, ((SolidFunctionCurveState)state).segments);
                ((SolidFunctionCurveState)state).segments = value;
            }
        }
        
        public SolidFunctionCurve(Func<float,float> f, string owner) {
            state = new SolidFunctionCurveState(f, owner);
        }

        public SolidFunctionCurve(SolidFunctionCurve sfc) : base(sfc) {
        }

        public override object Clone() {
            return new SolidFunctionCurve(this);
        }
    }

    public class SolidLineState : MeshBackedGeometry {
        public Vector3[] points;
        public Color color;
        public float width;
        // this is used to animate lines (from 0 to 1, how much of the line is visible)
        public float progression = 1.0f;

        public override object Clone()
        {
            return new SolidLineState(this);
        }

        public SolidLineState(string owner) : base(owner) {

        }

        public SolidLineState(RendererHandle h, string owner) : base(h, owner) {

        }

        public SolidLineState(SolidLineState sls) : base(sls) {
            this.color = sls.color;
            this.points = sls.points.ToArray();
            this.width = sls.width;
        }

        public override void UpdateMesh(ColoredTriangleMeshGeometry mesh)
        {
            if(points.Length == 2) {
                var ps = points.ToArray();
                ps[1] = points[0] + progression*(points[1]-points[0]);
                LineRenderer.UpdateLineMesh(mesh, ps, width, color, ownerGuid);
            } else {
                LineRenderer.UpdateLineMesh(mesh, points, width, color, ownerGuid);
            }
        }
    }

    public class SolidLine : VisualEntity, Labelable {
        public Vector3[] Points { 
            get {
                return ((SolidLineState)state).points;
            }
            set {
                World.current.SetProperty(this, "Points", value, ((SolidLineState)state).points);
                ((SolidLineState)state).points = value;
            }
        }
        public Color Color { 
            get {
                return ((SolidLineState)state).color;
            } set {
                World.current.SetProperty(this, "Color", value, ((SolidLineState)state).color);
                ((SolidLineState)state).color = value;
            }
        }
        public float Width {
            get {
                return ((SolidLineState)state).width;
            } set {
                World.current.SetProperty(this, "Width", value, ((SolidLineState)state).width);
                ((SolidLineState)state).width = value;
            }
        }

        public float Progression {
            get {
                return ((SolidLineState)state).progression;
            } set {
                World.current.SetProperty(this, "Progression", value, ((SolidLineState)state).progression);
                ((SolidLineState)state).progression = value;
            }
        }

        public SolidLine(string owner) {
            state = new SolidLineState(owner);
        }

        public SolidLine() : this(World.current.Resources.GetGuid()) {
        }

        public SolidLine(SolidLine sl) : base(sl) {
        }

        public Vector2? GetLabelOffset(CameraState cam, Rect label, LabelStyle style, EntityState state, Vector2 screenSize)
        {
            var mstate = (SolidLineState)state;
            if(cam is PerspectiveCameraState) {
                var pcam  = cam as PerspectiveCameraState;
                var startS = pcam.WorldToScreenPos(mstate.points[0], screenSize);
                var endS = pcam.WorldToScreenPos(mstate.points[1], screenSize);
                var dirS = (endS-startS).Normalized;
                var dotH = Vector2.Dot(dirS, Vector2.RIGHT);
                var dotV = Vector2.Dot(dirS, Vector2.UP);
                var alphaH = MathF.Acos(dotH);
                var linewhh = 0.5f*mstate.width/(MathF.Abs(MathF.Sin(0.5f*MathF.PI-alphaH)));
                var alphaV = MathF.Acos(dotV);
                var linewhv = 0.5f*mstate.width/(MathF.Abs(MathF.Sin(0.5f*MathF.PI-alphaV)));
                var centerS = 0.5f*(startS+endS);
            
                float x, y;
                if(MathF.Abs(dotH) >= MathF.Abs(dotV)) { // place up
                    var centerH = 0.5f*(mstate.points[0]+mstate.points[1]) + new Vector3(0.0f, linewhh, 0.0f);
                    var centerSH = pcam.WorldToScreenPos(centerH, screenSize);
                    var dif = centerSH-centerS;
                    x = 0.0f;
                    y = MathF.Abs(0.5f*label.width*MathF.Tan(alphaH))+0.5f*label.height;
                    return new Vector2(-x, -y);
                } else { // place left
                    var centerV = 0.5f*(mstate.points[0]+mstate.points[1]) + new Vector3(-linewhv, 0.0f, 0.0f);
                    var centerSV = pcam.WorldToScreenPos(centerV, screenSize);
                    var dif = centerSV-centerS;
                    x = MathF.Abs(0.5f*label.height*MathF.Tan(alphaV))+0.5f*label.width;
                    y = 0.0f;
                    return new Vector2(-x+dif.x, -y+dif.y);
                }
            } else {
                throw new NotImplementedException();
            }
        }

        public Vector3? GetLabelWorldCoordinate(LabelStyle style, EntityState state)
        {
            var ps = ((SolidLineState)state).points;
            return 0.5f*(ps[0]+ps[1]);
        }

        public override object Clone() {
            return new SolidLine(this);
        }
    }

    public class TexRectState : RectangleState {
        public Texture2D texture;

        public TexRectState() {
        }

        public TexRectState(TexRectState trs) : base(trs) {
            this.texture = trs.texture;
        }

        public override object Clone() {
            return new TexRectState(this);
        }
    }

    public class TexRect : Rectangle {
        public Texture2D Texture {
            get {
                return ((TexRectState)state).texture;
            }
            set {
                World.current.SetProperty(this, "Texture", value, ((TexRectState)state).texture);
                ((TexRectState)state).texture = value;
            }
        }
        public TexRect() {
            state = new TexRectState();
        }
    }

    public class RectangleState : EntityState {
        public float width, height;
        public Color color;
        public Color outline = Color.BLACK;
        public float outlineWidth = 1.0f;
        public Vector2 sizeRect;
        public bool is2d = false;

        public RectangleState() {
        }

        public RectangleState(RectangleState rs) : base(rs) {
            this.width = rs.width;
            this.height = rs.height;
            this.color = rs.color;
            this.outline = rs.outline;
            this.outlineWidth = rs.outlineWidth;
            this.sizeRect = rs.sizeRect;
            this.is2d = rs.is2d;
        }

        public override object Clone()
        {
            return new RectangleState(this);
        }
    }

    public class Dummy : VisualEntity {
        public Dummy() {}

        public Dummy(Dummy dummy) : base(dummy) {}

        public override object Clone() {
            return new Dummy(this);
        }
    }

    public class Rectangle : VisualEntity {
        public float Width {
            get {
                return ((RectangleState)state).width;
            }
            set {
                World.current.SetProperty(this, "Width", value, ((RectangleState)state).width);
                ((RectangleState)state).width = value;
            }
        }
        public float Height {
            get {
                return ((RectangleState)state).height;
            }
            set {
                World.current.SetProperty(this, "Height", value, ((RectangleState)state).height);
                ((RectangleState)state).height = value;
            }
        }
        public Color Color {
            get {
                return ((RectangleState)state).color;
            }
            set {
                World.current.SetProperty(this, "Color", value, ((RectangleState)state).color);
                ((RectangleState)state).color = value;
            }
        }
        public Color Outline {
            get {
                return ((RectangleState)state).outline;
            }
            set {
                World.current.SetProperty(this, "Outline", value, ((RectangleState)state).outline);
                ((RectangleState)state).outline = value;
            }
        }
        public float OutlineWidth {
            get {
                return ((RectangleState)state).outlineWidth;
            }
            set {
                World.current.SetProperty(this, "OutlineWidth", value, ((RectangleState)state).outlineWidth);
                ((RectangleState)state).outlineWidth = value;
            }
        }

        public Vector2 Anchor {
            get {
                return ((RectangleState)state).anchor;
            }
            set {
                World.current.SetProperty(this, "Anchor", value, ((RectangleState)state).anchor);
                ((RectangleState)state).anchor = value;
            }
        }

        public Vector2 SizeRect {
            get {
                return ((RectangleState)state).sizeRect;
            }
            set {
                World.current.SetProperty(this, "SizeRect", value, ((RectangleState)state).sizeRect);
                ((RectangleState)state).sizeRect = value;
            }
        }

        public Rectangle(bool is2d = false) {
            state = new RectangleState();
            if(is2d) {
                this.Transform = new RectTransform(this);
                ((RectangleState)state).is2d = true;
            }
        }

        public Rectangle(Rectangle r) : base(r) {
        }

        public Rectangle Pos(Vector3 pos) {
            Transform.Pos = pos;
            return this;
        }

        public Rectangle Rot(Quaternion rot) {
            Transform.Rot = rot;
            return this;
        }

        public Rectangle W(float width) {
            this.Width =width;
            return this;
        }

        public Rectangle H(float height) {
            this.Height = height;
            return this;
        }

        public Rectangle C(Color color) {
            this.Color = color;
            return this;
        }

        public override object Clone() {
            return new Rectangle(this);
        }
    };

    public class RendererAnimation {
        public Vector3? point;
        public Vector3? screenPoint;
        public float progress;
    }

    public class CircleState : EntityState {
        public float radius;
        public Color color;
        public Color outline = Color.BLACK;
        public float outlineWidth = 1.0f;
        public bool is2d = false;
        public Vector2 sizeRect;

        public CircleState() {}

        public CircleState(CircleState cs) : base(cs) {
            this.radius = cs.radius;
            this.color = cs.color;
            this.outline = cs.outline;
            this.outlineWidth = cs.outlineWidth;
            this.is2d = cs.is2d;
            this.sizeRect = cs.sizeRect;
        }

        public override object Clone() {
            return new CircleState(this);
        }
    }

    public class Circle : VisualEntity, IColored {
        public Circle() {
            state = new CircleState();
            Transform = new Transform(this);
        }
        public Circle(bool is2d) {
            state = new CircleState();
            if(is2d) {
                Transform = new RectTransform(this);
                ((CircleState)state).is2d = true;
            }
        }
        public Circle(Circle c) : base(c) {
        }
        public float Radius { 
            get {
                return ((CircleState)state).radius;
            }
            set {
                World.current.SetProperty(this, "Radius", value, ((CircleState)state).radius);
                ((CircleState)state).radius = value;
            }
        }
        public Color Color {
            get {
                return ((CircleState)state).color;
            } 
            set {
                World.current.SetProperty(this, "Color", value, ((CircleState)state).color);
                ((CircleState)state).color = value;
            }
        }
        public Color Outline {
            get {
                return ((CircleState)state).outline;
            }
            set {
                World.current.SetProperty(this, "Outline", value, ((CircleState)state).outline);
                ((CircleState)state).outline = value;
            }
        }

        public float OutlineWidth {
            get {
                return ((CircleState)state).outlineWidth;
            }
            set {
                World.current.SetProperty(this, "OutlineWidth", value, ((CircleState)state).outlineWidth);
                ((CircleState)state).outlineWidth = value;
            }
        }

        public Circle Pos(Vector3 p) {
            this.Transform.Pos = p;
            return this;
        }

        public Circle Rot(Quaternion quaternion) {
            Transform.Rot = quaternion;
            return this;
        }

        public Circle R(float r){
            this.Radius = r;
            return this;
        }

        public Circle C(Color color){ 
            this.Color = color;
            return this;
        }
        public override object Clone() {
            return new Circle(this);
        }
    }

    public class LabelState : EntityState
    {
        public string text;
        public float size;
        public LabelStyle style = LabelStyle.Free;
        public Labelable target;
        public Color color;

        public LabelState() {}

        public LabelState(LabelState ls) : base(ls) {
            this.text = ls.text;
            this.size = ls.size;
            this.style = ls.style;
            this.target = ls.target;
            this.color = ls.color;
        }

        public override object Clone()
        {
            return new LabelState(this);
        }
    }

    public class Label : VisualEntity {
        public string Text {
            get {
                return ((LabelState)state).text;
            } set {
                World.current.SetProperty(this, "Text", value, ((LabelState)state).text);
                ((LabelState)state).text = value;
            }
        }
        public float Size {
            get {
                return ((LabelState)state).size;
            } set {
                World.current.SetProperty(this, "Size", value, ((LabelState)state).size);
                ((LabelState)state).size = value;
            }
        }

        public Color Color {
            get {
                return ((LabelState)state).color;
            } set {
                World.current.SetProperty(this, "Color", value, ((LabelState)state).color);
                ((LabelState)state).color = value;
            }
        }

        public Label() {
            this.state = new LabelState();
        }

        public Label(Label l) : base(l) {
        }

        public override object Clone() {
            return new Label(this);
        }
    }

    public class WorldSnapshot {
        public EntityStateResolver resolver;
        public CameraState Camera;
        public CircleState[] Circles;
        public RectangleState[] Rectangles;
        public CubeState[] Cubes;
        public TexRectState[] TexRects;
        //public Text2DState[] Texts;
        public GlyphState[] Glyphs;
        public ColoredTriangleMesh[] Meshes;
        public MeshBackedGeometry[] MeshBackedGeometries;
        public (LabelState, EntityState)[] Labels;
        public double Time;
    }

    public class OrthoCameraState : CameraState {
        public float width;
        public float height;

        public OrthoCameraState() {}

        public OrthoCameraState(OrthoCameraState ocs) : base(ocs) {
            this.width = ocs.width;
            this.height = ocs.height;
        }

        public override object Clone()
        {
            return new OrthoCameraState(this);
        }

        public override M4x4 CreateWorldToClipMatrix(float aspect) {
            float hw = width/2.0f;
            float hh = height/2.0f;
            return M4x4.Ortho(0, width, 0, height, -1.0f, 1.0f);
        }
    }

    public class OrthoCamera : Camera {
        //public float /*left, right, bottom, top, front, back*/;
        
        public OrthoCamera() {}

        public OrthoCamera(OrthoCamera oc) : base(oc) {}

        public float Width {
            get {
                return ((OrthoCameraState)state).width;
            } set {
                World.current.SetProperty(this, "Width", value, ((OrthoCameraState)state).width);
                ((OrthoCameraState)state).width = value;
            }
        }
        public float Height {
            get {
                return ((OrthoCameraState)state).height;
            } set {
                World.current.SetProperty(this, "Height", value, ((OrthoCameraState)state).height);
                ((OrthoCameraState)state).height = value;
            }
        }
        
        public override object Clone() {
            return new OrthoCamera(this);
        }
    }

    public class PerspectiveCameraState : CameraState
    {
        public float fov = 60.0f;
        public float zNear = 0.1f;
        public float zFar = 1000.0f;

        public PerspectiveCameraState() {}

        public PerspectiveCameraState(PerspectiveCameraState pcs) : base(pcs) {
            this.fov = pcs.fov;
            this.zNear = pcs.zNear;
            this.zFar = pcs.zFar;
        }

        public override object Clone()
        {
            return new PerspectiveCameraState(this);
        }

        public M4x4 CreateWorldToViewMatrix() {
            var invRot = rotation;
            invRot.w *= -1.0f;
            var invPos = new Vector3(-position.x, -position.y, -position.z);
            M4x4 worldToView = M4x4.RT(invRot, invPos);
            return worldToView;
        }

        public M4x4 CreateViewToClipMatrix(float aspect) {
            var invRot = rotation;
            invRot.w *= -1.0f;
            var invPos = new Vector3(-position.x, -position.y, -position.z);
            M4x4 viewToClip = M4x4.Perspective(fov, aspect, this.zNear, this.zFar);
            return viewToClip;
        }

        public override M4x4 CreateWorldToClipMatrix(float aspect) {
            var invRot = rotation;
            invRot.w *= -1.0f;
            var invPos = new Vector3(-position.x, -position.y, -position.z);
            M4x4 worldToView = M4x4.RT(invRot, invPos);
            M4x4 worldToClip = M4x4.Perspective(fov, aspect, this.zNear, this.zFar) * worldToView;
            return worldToClip;
        }

        public Vector3 WorldToClipPos(float aspect, Vector3 pos) {
            var mat = CreateWorldToClipMatrix(aspect);
            Vector4 v4 = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            var ret = mat * v4;
            return new Vector3(ret.x/ret.w, ret.y/ret.w, ret.z/ret.w);
        }

        public Vector2 WorldToNormScreenPos(Vector3 pos, float aspect) {
            var clip = WorldToClipPos(aspect, pos);
            return new Vector2((clip.x+1.0f)*0.5f, 1.0f - (clip.y+1.0f)*0.5f);
        }

        public Vector2 WorldToScreenPos(Vector3 pos, Vector2 screenSize) {
            var clip = WorldToClipPos(screenSize.x/screenSize.y, pos);
            return new Vector2((clip.x+1.0f)*0.5f*screenSize.x, screenSize.y - (clip.y+1.0f)*0.5f*screenSize.y);
        }   

        public M4x4 CreateClipToWorldMatrix(float aspect) {
            M4x4 clipToView = M4x4.InvPerspective(fov, aspect, this.zNear, this.zFar);
            M4x4 viewToWorld = M4x4.TRS(position, rotation, Vector3.ONE);
            return viewToWorld * clipToView;
        }

        public Ray RayFromClip(Vector2 clipPos, float aspect) {
            M4x4 mat = CreateClipToWorldMatrix(aspect);
            Vector4 dirw = mat * new Vector4(clipPos.x, clipPos.y, 1.0f, 1.0f);
            Vector3 pos = new Vector3(dirw.x / dirw.w, dirw.y / dirw.w, dirw.z / dirw.w);
            return new Ray() {
                o = position,
                d = (pos - position).Normalized,
            };
        }
    }

    public class PerspectiveCamera : Camera {
        public PerspectiveCamera() {
            this.state = new PerspectiveCameraState();
        }
        public PerspectiveCamera(PerspectiveCamera pc) : base(pc) {}
        public float Fov {
            get {
                return ((PerspectiveCameraState)state).fov;
            } set {
                World.current.SetProperty(this, "Fov", value, ((PerspectiveCameraState)state).fov);
                ((PerspectiveCameraState)state).fov = value;
            }
        }
        public float ZNear {
            get {
                return ((PerspectiveCameraState)state).zNear;
            } set {
                World.current.SetProperty(this, "ZNear", value, ((PerspectiveCameraState)state).zNear);
                ((PerspectiveCameraState)state).zNear = value;
            }
        }
        public float ZFar {
            get {
                return ((PerspectiveCameraState)state).zFar;
            } set {
                World.current.SetProperty(this, "ZFar", value, ((PerspectiveCameraState)state).zFar);
                ((PerspectiveCameraState)state).zFar = value;
            }
        }

        public Vector3 WorldToClipPos(float aspect, Vector3 pos) {
            return ((PerspectiveCameraState)state).WorldToClipPos(aspect, pos);
        }   

        public M4x4 CreateClipToWorldMatrix(float aspect) {
            return ((PerspectiveCameraState)state).CreateClipToWorldMatrix(aspect);
        }

        public Ray RayFromClip(Vector2 clipPos, float aspect) {
            return ((PerspectiveCameraState)state).RayFromClip(clipPos, aspect);
        }

        public override object Clone() {
            return new PerspectiveCamera(this);
        }
    }

    public abstract class Camera : VisualEntity {
        public Camera() {}
        public Camera(Camera c) : base(c) {}

        public Color ClearColor {
            get {
                return ((CameraState)state).clearColor;
            }
            set {
                World.current.SetProperty(this, "clearColor", value, ((CameraState)state).clearColor);
                ((CameraState)state).clearColor = value;
            }
        }
    }

    public abstract class CameraState : EntityState {
        public Color clearColor = Color.WHITE;

        public CameraState() {}

        public CameraState(CameraState cs) : base(cs) {
        }

        public abstract M4x4 CreateWorldToClipMatrix(float aspect);
    }

    public enum WorldCommandType {
        Create_Entity,
        Destroy_Entity,
        Set_Property,
    }

    public class WorldCommand {
        public double time;
    }

    public class WorldSoundCommand : WorldCommand {
        public float volume;
    }

    public class WorldPlaySoundCommand : WorldSoundCommand {
        public SoundSample sound;
    }

    public class WorldPropertyCommand : WorldCommand {
        public int entityId;
        public string property;
        public object newvalue;
        public object oldvalue;
    }

    public class WorldCreateCommand : WorldCommand {
        public object entity;
    }

    public class WorldDestroyCommand : WorldCommand {
        public int entityId;
    }

    public class WorldAbsorbCommand : WorldCommand {
        public int entityId;
        public float progress;
        public float oldprogress;
        public Vector3? absorbPoint;
        public Vector3? absorbScreenPoint;
    }

    public class WorldSetActiveCameraCommand : WorldCommand {
        public int cameraEntId;
        public int oldCamEntId;
    }

    public class WorldEndCommand : WorldCommand {
        
    }

    public class EntityResolver {
        public Func<int, VisualEntity> GetEntity;
    }

    public class World
    {
        [ThreadStatic]
        static int entityId = 1;
        [ThreadStatic]
        public static World current;

        List<WorldCommand> _commands = new List<WorldCommand>();
        List<WorldSoundCommand> _soundCommands = new List<WorldSoundCommand>();
        List<Label> _labels = new List<Label>();

        private Dictionary<int, VisualEntity> _entities = new Dictionary<int, VisualEntity>();

        public TypeSetting ts = new TypeSetting();

        public EntityResolver EntityResolver;
        Color background = Color.WHITE;

        Camera _activeCamera;
        public Camera ActiveCamera {
            get {
                return _activeCamera;
            } set {
                var cmd = new WorldSetActiveCameraCommand() {
                    oldCamEntId = _activeCamera?.EntityId ?? 0,
                    cameraEntId = value?.EntityId ?? 0,
                    time = AnimationTime.Time,
                };
                _commands.Add(cmd);
                _activeCamera = value;
            }
        }

        public World() {
            current = this;
            EntityResolver = new EntityResolver {
                GetEntity = entid => {
                    return _entities[entid];
                }
            };
            //this._activeCamera.Position = new Vector3(0.0f, 0.0f, 13.0f);
            Reset();
        }
        List<AbsorbDestruction> removes = new List<AbsorbDestruction>();

        public int GetUniqueId() {
            return entityId++;
        }

        public void Update(double dt) {
            foreach(var label in _labels) {
                LabelState state = ((LabelState)label.state);
                var val = state.target.GetLabelWorldCoordinate(state.style, ((VisualEntity)state.target).state);
                if(val != null){
                    label.state.position = val.Value;
                }
            }
            
            removes.Clear();
        }

        public void PlaySound(SoundSample sound, float volume = 1.0f) {
            var command = new WorldPlaySoundCommand() {
                time = AnimationTime.Time,
                volume = volume,
                sound = sound,
            };
            _soundCommands.Add(command);
        }

        public void PlaySound(BuiltinSound sound, float volume = 1.0f) {
            PlaySound(SoundSample.GetBuiltin(sound), volume);
        }

        public void Reset() {
            Resources?.Dispose();
            Resources = new WorldResources();
            _commands.Clear();
            _labels.Clear();
            var cam = new PerspectiveCamera();
            cam.Fov = 60.0f;
            cam.ZNear = 0.1f;
            cam.ZFar = 1000.0f;
            cam.Transform.Pos = new Vector3(0.0f, 0.0f, -13.0f);
            CreateInstantly(cam);
            ActiveCamera = cam;
        }

        public void AddResource(ColoredTriangleMeshGeometry geometry) {
            Resources.MeshGeometries.Add(geometry);
        }

        public void AddResource(Texture2D texture) {
            Resources.Textures.Add(texture);
        }

        public void SetProperty<T>(VisualEntity entity, string propert, T value, T oldvalue) {
            if(value.Equals(oldvalue))
                return;
            if(entity.created) {
                var cmd = new WorldPropertyCommand {
                    entityId = entity.EntityId,
                    time = AnimationTime.Time,
                    property = propert,
                    newvalue = value,
                    oldvalue = oldvalue,
                };
                _commands.Add(cmd);
            }
        }

        private void EntityCreated(VisualEntity entity) {
            entity.state.entityId = GetUniqueId();
            var cmd = new WorldCreateCommand() {
                time = AnimationTime.Time,
                entity = entity.state.Clone(),
            };
            _commands.Add(cmd);
            entity.created = true;
            _entities.Add(entity.EntityId, entity);
            switch(entity) {
                case Label l1:
                _labels.Add(l1);
                break;
            }
            entity.EntityCreated();
        }

        public VisualEntity CreateInstantly(VisualEntity ent) {
            EntityCreated(ent);
            return ent;
        } 

        public Task CreateFadeIn<T>(T entity, float duration) where T : VisualEntity,IColored {
            CreateInstantly(entity);
            var c = entity.Color;
            return AnimationTransform.SmoothT<float>(x => {
                    c.a = (byte)Math.Round(x*255.0f);
                    entity.Color = c;
                }, 0.0f, 1.0f, duration);
        }

        public VisualEntity CloneInstantly(VisualEntity ent) {
            var ret = (VisualEntity)ent.Clone();
            CreateInstantly(ret);
            return ret;
        }

        public T CloneInstantly<T>(T e) where T : VisualEntity, new() {
            var ret = (T)e.Clone();
            CreateInstantly(ret);
            return ret;
        }

        public void Destroy(VisualEntity obj) {
            var cmd = new WorldDestroyCommand() {
                time = AnimationTime.Time,
                entityId = obj.EntityId,
            };
            _commands.Add(cmd);
        }

        public WorldCommand[] GetCommands() {
            return _commands.Concat(new WorldCommand[]{new WorldEndCommand{time = AnimationTime.Time}}).ToArray();
        }

        public WorldSoundCommand[] GetSoundCommands() {
            return _soundCommands.ToArray();
        }

        public WorldResources Resources;
    }
}
