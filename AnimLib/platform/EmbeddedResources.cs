using System.IO;
using AnimLib.Resources;

namespace AnimLib {
    internal static class EmbeddedResources {
        public static Stream GetResource(string folder, string file) {
            return AnimLibAssembly.Value.GetManifestResourceStream($"AnimLib.Resources.{folder}.{file}") ?? throw new FileNotFoundException($"Resource {folder}/{file} not found");
        }

        public static byte[] GetResourceBytes(string folder, string file) {
            var stream = GetResource(folder, file);
            if(stream == null) {
                throw new FileNotFoundException($"Resource {folder}/{file} not found");
            }
            using (var ms = new MemoryStream()) {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

    }
}
