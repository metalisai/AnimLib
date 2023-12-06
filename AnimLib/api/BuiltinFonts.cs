using System.IO;

namespace AnimLib;

/// <summary>
/// A collection of fonts built into the executable.
/// </summary>
public static class BuiltinFont {

    /// <summary>
    /// Get a builtin font
    /// </summary>
    /// <param name="path"> The path to the font resource.</param>
    private static Stream GetFont(string path) {
        var ret = EmbeddedResources.GetResource("font", path);
        if(ret == null) {
            Debug.Error($"Failed to get builtin font {path}");
        }
        return ret;
    }

    /// <summary>
    /// DejaVu Sans font. A monospace font good for things like code.
    /// </summary>
    public static Stream Dejavu_Monospace {
        get {
            return GetFont("DejaVuSansMono.ttf");
        }
    }

    /// <summary>
    /// Computer Modern Sans Serif font.
    /// </summary>
    public static Stream Computer_Modern_SansSerif {
        get {
            return GetFont("cmunss.ttf");
        }
    }

    /// <summary>
    /// Computer Modern Serif font.
    /// </summary>
    public static Stream Computer_Modern_Serif {
        get {
            return GetFont("cmunrm.ttf");
        }
    }
}
