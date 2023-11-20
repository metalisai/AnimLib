using System.IO;
using AnimLib.Resources;

namespace AnimLib {
    internal static class EmbeddedResources {
        public static Stream GetResource(string folder, string file) {
            return AnimLibAssembly.Value.GetManifestResourceStream($"AnimLib.Resources.{folder}.{file}");
        }

        public static byte[] GetResourceBytes(string folder, string file) {
            var stream = GetResource(folder, file);
            if(stream == null)
                return null;
            using (var ms = new MemoryStream()) {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

    }
}
