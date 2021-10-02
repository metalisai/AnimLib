using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace AnimLib {
    public class FfmpegExporter {

        Process ffmpegProcess;
        BinaryWriter ffmpegStdin;

        public FfmpegExporter() {

        }

        private void FfmpegOnExit(object sender, System.EventArgs e) { 
            Console.WriteLine("ffmpeg has exited!");
        }

        public void Start(string filename, int width, int height, int framerate, int crf = 15) {
            /*int width = 1920;
            int height = 1080;
            double framerate = 60;*/

            // string av1 options = "-c:v libaom-av1  -strict -2"

            string outputOpt = $"-c:v libx264 -crf {crf}";
            //string outputOpt = "-c:v libvpx-vp9 -lossless 1";

            var opt = $"-y -f rawvideo -vcodec rawvideo -pixel_format rgb24 -video_size {width}x{height} -r {framerate} -i - -vf vflip {outputOpt} \"{filename}\"";

            var info = new ProcessStartInfo("ffmpeg", opt);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            ffmpegProcess = Process.Start(info);
            ffmpegProcess.Exited += FfmpegOnExit;
            ffmpegStdin = new BinaryWriter(ffmpegProcess.StandardInput.BaseStream);
            
            ffmpegProcess.EnableRaisingEvents = true;
            ffmpegProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                if(e.Data != null) {
                    Console.WriteLine($"ffmpeg STDOUT: {e.Data}");
                }
            };
            ffmpegProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                if(e.Data != null) {
                    Console.WriteLine($"ffmpeg STDERR: {e.Data}");
                }  
            };
            ffmpegProcess.BeginOutputReadLine();
            ffmpegProcess.BeginErrorReadLine();
        }


        public void PushData(byte[] data) {
            if(ffmpegStdin != null) {
                ffmpegStdin.Write(data);
            }
        }

        public void TestStream() {
            Start("test"+Guid.NewGuid().ToString()+".mp4", 1920, 1080, 60);
            if(ffmpegProcess != null) {
                for(int k = 0; k < 60*10; k++) {
                    for(int i = 0; i < 1080; i++) {
                        for(int j = 0; j < 1920; j++) {
                            int col = j/480;
                            switch(col) {
                                case 0:
                                ffmpegStdin.Write((byte)0xff);
                                ffmpegStdin.Write((byte)0x00);
                                ffmpegStdin.Write((byte)0x00);
                                break;
                                case 1:
                                ffmpegStdin.Write((byte)0x00);
                                ffmpegStdin.Write((byte)0xff);
                                ffmpegStdin.Write((byte)0x00);
                                break;
                                case 2:
                                ffmpegStdin.Write((byte)0x00);
                                ffmpegStdin.Write((byte)0x00);
                                ffmpegStdin.Write((byte)0xff);
                                break;
                                case 3:
                                ffmpegStdin.Write((byte)0xff);
                                ffmpegStdin.Write((byte)0xff);
                                ffmpegStdin.Write((byte)0xff);
                                break;
                            }
                        }
                    }
                }
            }
            Stop();
        }

        public void Stop() {
            if(ffmpegProcess != null) {
                ffmpegProcess.StandardInput.Close();
                ffmpegProcess.WaitForExit();
                ffmpegProcess.CancelOutputRead();
                ffmpegProcess.CancelErrorRead();
                Console.WriteLine("ffmpeg stream ended!");
                ffmpegProcess.Close();
                ffmpegProcess.Dispose();
                ffmpegProcess = null;
                ffmpegStdin = null;
            }
        }

        public void AddAudio(string videofile, short[] samples, int sampleRate) {
            var opt = $"-y -i {videofile} -f s16le -i - -c:v copy {videofile + "-audio.mp4"}";
            Debug.Log($"Audio command: {"ffmpeg "+ opt}");

            var info = new ProcessStartInfo("ffmpeg", opt);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            ffmpegProcess = Process.Start(info);
            ffmpegProcess.Exited += FfmpegOnExit;
            ffmpegStdin = new BinaryWriter(ffmpegProcess.StandardInput.BaseStream);
            
            ffmpegProcess.EnableRaisingEvents = true;
            ffmpegProcess.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                if(e.Data != null) {
                    Console.WriteLine($"ffmpeg STDOUT: {e.Data}");
                }
            };
            ffmpegProcess.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                if(e.Data != null) {
                    Console.WriteLine($"ffmpeg STDERR: {e.Data}");
                }  
            };
            ffmpegProcess.BeginOutputReadLine();
            ffmpegProcess.BeginErrorReadLine();

            PushData(MemoryMarshal.Cast<short, byte>(samples.AsSpan()).ToArray());
            Stop();
            Console.WriteLine("Done adding audio");
        }
    }
}
