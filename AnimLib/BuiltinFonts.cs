using System.IO;

namespace AnimLib;

public static class BuiltinFont {

    private static Stream GetFont(string path) {
        var ret = EmbeddedResources.GetResource("font", path);
        if(ret == null) {
            Debug.Error($"Failed to get builtin font {path}");
        }
        return ret;
    }

    public static Stream Dejavu_Monospace {
        get {
            return GetFont("DejaVuSansMono.ttf");
        }
    }

    public static Stream Computer_Modern_SansSerif {
        get {
            return GetFont("cmunss.ttf");
        }
    }

    public static Stream Computer_Modern_Serif {
        get {
            return GetFont("cmunrm.ttf");
        }
    }
}
