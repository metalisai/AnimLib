using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;

namespace AnimLib {
#if Windows
    class FileChooser {
        static public string ChooseFile(string title, string path, string[] filters)
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
                    return ofd.FileName;
                }
                return null;
            }
        }

        static public string ChooseDirectory(string title, string path)
        {
            using(var fbd = new FolderBrowserDialog())
            {
                fbd.InitialDirectory = path;
                fbd.Description = title;
                if(fbd.ShowDialog() == DialogResult.OK) {
                    return fbd.SelectedPath;
                }
                return null;
            }
        }
    }
#endif
}
