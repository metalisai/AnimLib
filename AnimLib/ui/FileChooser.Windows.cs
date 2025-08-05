#if Windows
using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

namespace AnimLib {
    class FileChooser {
        static public Task<string?> ChooseFile(string title, string path, string[] filters)
        {
            using(var ofd = new OpenFileDialog())
            {
                ofd.Title = title;
                ofd.InitialDirectory = path;
                var sb = new StringBuilder();
                sb.Append("Files ");
                sb.Append("(");
                foreach(var filter in filters) {
                    sb.Append(filter);
                    if(filter != filters[filters.Length-1]) {
                        sb.Append(",");
                    }
                }
                sb.Append(") | ");
                foreach(var filter in filters) {
                    sb.Append(filter);
                    if(filter != filters[filters.Length-1]) {
                        sb.Append(";");
                    }
                }
                ofd.ShowHelp = true;
                ofd.Filter = sb.ToString();
                ofd.FilterIndex = 0;
                ofd.RestoreDirectory = true;

                if(ofd.ShowDialog() == DialogResult.OK) {
                    return Task.FromResult(ofd.FileName);
                }
                return null;
            }
        }

        static public Task<string?> ChooseDirectory(string title, string path)
        {
            using(var fbd = new FolderBrowserDialog())
            {
                fbd.InitialDirectory = path;
                fbd.Description = title;
                if(fbd.ShowDialog() == DialogResult.OK) {
                    return Task.FromResult(fbd.SelectedPath);
                }
                return Task.FromResult<string?>(null);
            }
        }
    }
}
#endif
