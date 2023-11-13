using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AnimLib;

/// <summary>
/// The animator API that's passed to the animation behaviour. Stores state related to the animating process.
/// </summary>
public class Animator {
    internal class AnimationHandle2D {
        public string Identifier;
        public double StartTime;
        public double EndTime;
        public Vector2 Position;
        public Vector2 Anchor;
    }

    internal class AnimationHandle3D {
        public string Identifier;
        public double StartTime;
        public double EndTime;
        public Vector3 Position;
    }

    internal ResourceManager resourceManager;
    internal World world;
    internal AnimationSettings settings;
    internal AnimationPlayer.PlayerProperties props;
    internal List<AnimationHandle2D> VectorHandles = new List<AnimationHandle2D>();
    internal List<AnimationHandle3D> VectorHandles3D = new List<AnimationHandle3D>();
    internal TextPlacement textPlacement;

    /// <summary>
    /// The descriptor for the main render target. This is the render target that's used for the final output.
    /// </summary>
    public RenderBuffer Backbuffer {
        get;
        private set;
    }

    /// <summary>
    /// The currently active animator.
    /// </summary>
    public static Animator Current { get; internal set; }

    /// <summary>
    /// The scene of the animation editor. Doesn't include programmatically created entities.
    /// </summary>
    public PlayerScene Scene;

    internal Animator(ResourceManager resourceManager, World world, PlayerScene scene, AnimationSettings settings, AnimationPlayer.PlayerProperties props, TextPlacement text) {
        this.resourceManager = resourceManager;
        this.world = world;
        this.settings = settings;
        this.Scene = scene;
        this.props = props;
        this.textPlacement = text;
        this.Backbuffer = new RenderBuffer(settings.Width, settings.Height, true);
    }

    internal void BeginAnimate() {
        if (Current != null) {
            throw new Exception("Animator already in use!");
        }
        Current = this;
    }

    internal void EndAnimate() {
        Current = null;
    }

    /// <summary>
    /// Get a named color from the Values panel.
    /// </summary>
    public Color GetColor(string name) {
        Color col;
        if(props.Values.ColorMap.TryGetValue(name, out col)) {
            return col;
        } else { 
            return default(Color);
        }
    }

    /// <summary>
    /// Load a font from a stream. The font name can then be used as an identifier for the font. For example Text2D() creation.
    /// </summary>
    public void LoadFont(Stream stream, string fontname) {
        textPlacement.LoadFont(stream, fontname);
    }

    /// <summary>
    /// Load a font from a file path. The font name can then be used as an identifier for the font. For example Text2D() creation.
    /// </summary>
    public void LoadFont(string filename, string fontname) {
        textPlacement.LoadFont(filename, fontname);
    }

    /// <summary>
    /// Shape a string with the given font and size. Returns a list of shapes and the characters.
    /// </summary>
    public List<(Shape s, char c)> ShapeText(string texts, Vector2 pos, int size, string font = null) {
        return textPlacement.PlaceTextAsShapes(texts, pos, size, font);
    }

    /// <summary>
    /// Create a 2D handle that can be moved in the animation editor.
    /// </summary>
    public Vector2 CreateHandle2D(string name, Vector2 pos, Vector2 anchor = default) {
        string key = settings.Name + "/" + name;
        Vector2 storedPos;
        if(props.VectorHandleMap.TryGetValue(key, out storedPos)) {
            pos = storedPos;
        }
        var handle = new AnimationHandle2D {
            Identifier = key,
            StartTime = Time.T,
            EndTime = Time.T + 1000.0,
            Position = pos,
            Anchor = anchor,
        };
        VectorHandles.Add(handle);
        return pos;
    }

    /// <summary>
    /// Create a 3D handle that can be moved in the animation editor.
    /// </summary>
    public Vector3 CreateHandle3D(string name, Vector3 pos) {
        string key = settings.Name + "/" + name;
        Vector3 storedPos;
        if(props.VectorHandleMap3D.TryGetValue(key, out storedPos)) {
            pos = storedPos;
        }
        var handle = new AnimationHandle3D {
            Identifier = key,
            StartTime = Time.T,
            EndTime = Time.T + 1000.0,
            Position = pos,
        };
        VectorHandles3D.Add(handle);
        return pos;
    }
    
    /// <summary>
    /// Get a sound sample stored within the project.
    /// </summary>
    public SoundSample? GetSoundResource(string name) {
        string fileName;
        try {
            using (var res = resourceManager.GetResource(name, out fileName)) {
                var sample = SoundSample.GetFromStream(res);
                return sample;
            }
        } catch (NullReferenceException) {
            Debug.Error($"Failed to load resource {name}");
        }
        return null;
    }

    /// <summary>
    /// Get a SVG resource stored within the project.
    /// </summary>
    public SvgData GetSvgResource(string name) {
        string fileName;
        try {
            using (var res = resourceManager.GetResource(name, out fileName)) {
                var reader = new StreamReader(res);
                var data = reader.ReadToEnd();
                return new SvgData() {
                    handle = -1,
                    svg = data,
                };
            }
        } catch (NullReferenceException) {
            Debug.Error($"Failed to load SVG resource {name}");
        }
        return null;
    }

    /// <summary>
    /// Get a texture resource stored within the project. Can be loaded from any common image format.
    /// </summary>
    public Texture2D GetTextureResource(string name) {
        string fileName;
        try {
            using (var res = resourceManager.GetResource(name, out fileName)) {
                if(res != null) {
                    var ext = Path.GetExtension(fileName);
                    switch(ext.ToLower()) {
                        case ".jpg":
                        case ".png":
                        case ".bmp":
                        case ".gif":
                        case ".exif":
                        case ".tiff":
                        var image = new Bitmap(res);
                        var pxsize = Image.GetPixelFormatSize(image.PixelFormat);
                        System.Drawing.Imaging.PixelFormat outFmt = default;
                        Texture2D.TextureFormat outFmtGl = default;
                        switch(pxsize) {
                            case 8:
                                outFmt = System.Drawing.Imaging.PixelFormat.Alpha;
                                outFmtGl = Texture2D.TextureFormat.R8;
                                break;
                            case 24:
                                outFmt = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
                                outFmtGl = Texture2D.TextureFormat.BGR8;
                                break;
                            case 32:
                                outFmt = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                                outFmtGl = Texture2D.TextureFormat.BGRA8;
                                break;
                            default:
                                System.Console.WriteLine("Unsupported pixel format " + image.PixelFormat);
                                return null;
                        }
                        var data = image.LockBits(
                            new System.Drawing.Rectangle(new Point(0, 0), new Size(image.Width, image.Height)), 
                            System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                            outFmt
                        );
                        var len = data.Stride * data.Height;

                        byte[] bytes = new byte[len];
                        Marshal.Copy(data.Scan0, bytes, 0, len);
                        image.UnlockBits(data);

                        // TODO: some image formats (and opengl by default) add padding for alignment
                        System.Diagnostics.Debug.Assert(bytes.Length >= image.Width * image.Height * (pxsize/8));

                        var ret = new Texture2D(world.Resources.GetGuid()) {
                            RawData = bytes,
                            Format = outFmtGl,
                            Width = image.Width,
                            Height = image.Height,
                        };
                        world.AddResource(ret);
                        return ret;
                    }
                }
                return null;
            }
        } catch (NullReferenceException)
        {
            // TODO: use fallback texture instead of failing?
            System.Diagnostics.Debug.Fail($"Resource {name} doesn't exist or no project loaded!");
            return null;
        }
    }

}
