using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace AnimLib {

    public partial class ResourceManager {

        object rlock = new object();

        public delegate void AssemblyChanged(string newpath);
        public event AssemblyChanged OnAssemblyChanged;

        bool resourcesDirty = false;
        ZipArchive resourceArchive;
        MemoryStream ms;

        readonly string SCENE_FILE_NAME = "scene.json";
        readonly string SETTINGS_FILE_NAME = "settings.json";
        readonly string PROPERTIES_FILE_NAME = "properties.json";
        readonly string RESOURCES_DIR = ".resources";
        readonly string SETTINGS_DIR = ".animlib";
        public readonly string PROJECT_EXTENSION = ".animproj";

        string currentProjectPath;

        public ResourceManager() {
            ms = new MemoryStream();
        }

        protected string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        private ZipArchiveEntry CreateProjectTextFile(string filename, string text)
        {
            var scene = resourceArchive.CreateEntry($"{SETTINGS_DIR}/{filename}");
            scene.ExternalAttributes |= Convert.ToInt32("664", 8) << 16;
            using(var scenestream = scene.Open())
            {
                scenestream.Write(System.Text.UTF8Encoding.UTF8.GetBytes(text));
            }
            return scene;
        }

        private string GetAssemblyPath(string projectDirectory, string projectname)
        {
            return Path.Join(projectDirectory, $"src/bin/Debug/netcoreapp3.1/{projectname}.dll");
        }

        public bool haveProject {
            get {
                lock(rlock) {
                    return currentProjectPath != null;
                }
            }
        }

        public bool CreateProject(string directoryPath)
        {
            lock(rlock) {
                ms = new MemoryStream();
                // create zip archive
                resourceArchive = new ZipArchive(ms, ZipArchiveMode.Create, true);
                var name = Path.GetFileName(directoryPath);
                currentProjectPath = Path.Join(directoryPath, name+PROJECT_EXTENSION);
                // create scene.json
                CreateProjectTextFile(SCENE_FILE_NAME, "{}");
                // create settings.json
                var pset = new ProjectSettings() {
                    Name = name,
                };
                string json = JsonSerializer.Serialize(pset);
                var settings = CreateProjectTextFile(SETTINGS_FILE_NAME, json);
                // create src directory
                var srcPath = Path.Join(directoryPath, "src");
                Directory.CreateDirectory(srcPath);
                using(var csproj = File.CreateText(Path.Join(srcPath, $"{name}.csproj")))
                {
                    csproj.Write(CreateCsProj());
                }
                using(var maincs = File.CreateText(Path.Join(srcPath, $"{name}.cs")))
                {
                    var classname = FirstLetterToUpper(name);
                    maincs.Write(CreateMain(classname));
                }
                resourcesDirty = true;
                var ret = Save();
                if(OnAssemblyChanged != null) {
                    OnAssemblyChanged(GetAssemblyPath(directoryPath, name));
                }
                return ret;
            }
        }

        public bool SetProject(string projectFile)
        {
            lock(rlock) {
                var projectDir = Path.GetDirectoryName(projectFile);
                currentProjectPath = projectFile;
                try {
                    using(var fs = new FileStream(currentProjectPath, FileMode.Open, FileAccess.Read)) {
                        fs.CopyTo(ms);
                        currentProjectPath = projectFile;
                    }
                } catch (FileNotFoundException) {
                    System.Console.WriteLine($"Project file not found {projectFile}");
                    return false;
                }
                resourceArchive = new ZipArchive(ms, ZipArchiveMode.Update, true);
                var settings = resourceArchive.GetEntry($"{SETTINGS_DIR}/{SETTINGS_FILE_NAME}");
                if (settings == null) {
                    throw new Exception("Project file did not contain project settings");
                }
                using(var setstream = settings.Open())
                {
                    var json = new StreamReader(setstream).ReadToEnd();
                    var pset = JsonSerializer.Deserialize<ProjectSettings>(json);
                    if (pset == null)
                        throw new Exception("Failed to parse project settings");
                    var assembly = GetAssemblyPath(projectDir, pset.Name);
                    if(OnAssemblyChanged != null) {
                        OnAssemblyChanged(assembly);
                    }
                }
                System.Console.WriteLine($"Project loaded {projectFile}");
                return true;
            }
        }

        public void AddResource(string filename) {
            lock(rlock) {
                if(!haveProject)
                    return;
                string name = Path.GetFileName(filename);
                var entry = resourceArchive.Entries.Where(x => x.Name == name).FirstOrDefault();
                if(entry != null) {
                    entry.Delete();
                }
                resourceArchive.CreateEntryFromFile(filename, $"{RESOURCES_DIR}/{name}", CompressionLevel.NoCompression);
                Console.WriteLine($"Added {Path.GetFileName(filename)} as {RESOURCES_DIR}/{name} to project archive");
                resourcesDirty = true;
                Save();
            }
        }

        public void AddResource(string resource, byte[] file)
        {
            lock(rlock) {
                if(!haveProject)
                    return;
                var entry = resourceArchive.CreateEntry($"{RESOURCES_DIR}/{resource}");
                using(var es = entry.Open())
                {
                    es.Write(file, 0, file.Length);
                }
                resourcesDirty = true;
                Save();
            }
        }

        public void DeleteResource(string name) {
            lock(rlock) {
                if(!haveProject)
                    return;
                var entry = resourceArchive.Entries.Where(x => x.FullName == $"{RESOURCES_DIR}/{name}").FirstOrDefault();
                if(entry != null) {
                    entry.Delete();
                }
                resourcesDirty = true;
                Save();
            }
        }

        public Stream GetResource(string name, out string fileName) {
            lock(rlock) {
                if(!haveProject) {
                    fileName = "";
                    return null;
                }
                var resname = $"{RESOURCES_DIR}/{name}";
                var entry = resourceArchive.Entries.Where(x => 
                        x.FullName.StartsWith(resname)).FirstOrDefault();
                fileName = Path.GetFileName(entry?.Name);
                return entry?.Open();
            }
        }

        public struct StoredResource {
            public string name;
        }

        public StoredResource[] GetStoredResources() {
            lock(rlock) {
                if(!haveProject) {
                    return new StoredResource[0];
                }
                return resourceArchive.Entries.Where(x => x.FullName.StartsWith($"{RESOURCES_DIR}/"))
                    .Select(x => new StoredResource {
                    name = Path.GetFileName(x.Name),
                }).ToArray();
            }
        }

        internal AnimationPlayer.PlayerProperties GetProperties() {
            lock(rlock) {
                if(!haveProject)
                    return null;
                var fn = $"{SETTINGS_DIR}/{PROPERTIES_FILE_NAME}";
                using(Stream s = resourceArchive.GetEntry(fn)?.Open()) {
                    if (s != null) {
                        StreamReader sr = new StreamReader(s);
                        var str = sr.ReadToEnd();
                        return JsonSerializer.Deserialize<AnimationPlayer.PlayerProperties>(str) ?? new AnimationPlayer.PlayerProperties();
                    } else {
                        return new AnimationPlayer.PlayerProperties();
                    }
                }
            }
        }

        private void SaveJsonToFile(object sdata, string filename) {
            var entry = resourceArchive.GetEntry(filename);
            JsonSerializerOptions opt = new JsonSerializerOptions();
            opt.MaxDepth = 10;
            opt.WriteIndented = true;
            //opt.Converters.Add(new JsonColorConverter());
            var str = JsonSerializer.Serialize(sdata, opt);
            var data = System.Text.UTF8Encoding.UTF8.GetBytes(str);
            if (entry != null) {
                entry.Delete();
            }
            var ne = CreateProjectTextFile(Path.GetFileName(filename), "{}");
            using(var ns = ne.Open())
            {
                ns.Write(data);
            }
        }

        internal void SaveProperties(AnimationPlayer.PlayerProperties props) {
            lock(rlock) {
                if(!haveProject)
                    return;
                var fn = $"{SETTINGS_DIR}/{PROPERTIES_FILE_NAME}";
                SaveJsonToFile(props, fn);
                resourcesDirty = true;
                Save();
            }
        }

        public PlayerScene GetScene() {
            lock(rlock) {
                if(!haveProject)
                    return null;
                var fn = $"{SETTINGS_DIR}/{SCENE_FILE_NAME}";
                using(Stream s = resourceArchive.GetEntry(fn)?.Open())
                {
                    if (s != null)
                    {
                        StreamReader sr = new StreamReader(s);
                        var str = sr.ReadToEnd();
                        return JsonSerializer.Deserialize<PlayerScene>(str) ?? new PlayerScene();
                    }
                    else
                    {
                        return new PlayerScene();
                    }
                }
            }
        }

        public void SaveScene(PlayerScene scene) {
            lock(rlock) {
                if(!haveProject)
                    return;
                var fn = $"{SETTINGS_DIR}/{SCENE_FILE_NAME}";
                SaveJsonToFile(scene, fn);
                resourcesDirty = true;
                Save();
            }
        }

        public bool Save() {
            lock(rlock) {
                if(!haveProject)
                    return false;
                if(!resourcesDirty)
                    return true;
                try {
                    var fs = new FileStream(currentProjectPath, FileMode.Create, FileAccess.Write, FileShare.Write);
                    resourceArchive.Dispose();
                    ms.WriteTo(fs);
                    fs.Dispose();
                    resourceArchive = new ZipArchive(ms, ZipArchiveMode.Update, true);
                    resourcesDirty = false;
                } catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
