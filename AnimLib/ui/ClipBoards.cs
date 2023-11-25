namespace AnimLib;

internal static class Clipboard {
    private static object data = null;

    public static object Object {
        get {
            return data;
        }
        set {
            data = value;
        }
    }
}
