using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace AnimLib;

internal class FfmpegExporter {

    Process? ffmpegProcess;
    BinaryWriter? ffmpegStdin;

    public FfmpegExporter() {
    }

    private void FfmpegOnExit(object? sender, System.EventArgs e) { 
        Console.WriteLine("ffmpeg has exited!");
    }

    public bool IsRunning { 
        get => ffmpegProcess != null;
    }

    public void Start(string filename, int width, int height, int framerate, int crf = 15, FrameColorSpace colorSpace = FrameColorSpace.sRGB, Texture2D.TextureFormat format = Texture2D.TextureFormat.RGB8) {
        // most players seem to choke on this :/
        //string outputOpt = "-c:v libvpx-vp9 -lossless 1";
        string outputOpt = "-c:v ffv1";
        string vfOpt = "vflip";
        string pixFmt = "";

        filename = filename.Replace(".mp4", ".mkv");

        if (format == Texture2D.TextureFormat.RGB8)
        {
            pixFmt = "rgb24";
            Debug.Log("Ffmpeg using 8 bit color depth.");
        }
        else if (format == Texture2D.TextureFormat.RGB16)
        {
            pixFmt = "rgb48le";
            Debug.Log("Ffmpeg using 16 bit color depth.");
        }
        else
        {
            throw new Exception("Only RGB8 or RGB16 supported");
        }

        // NOTE: this causes banding for darker colors if the input is only 8 bit
        if(colorSpace == FrameColorSpace.Linear) {
            vfOpt = "vflip, colorspace=all=bt709:iprimaries=1:itrc=8:ispace=1";
            Debug.Log("Ffmpeg applying conversion to sRGB color space.");
        }

        var opt = $"-y -f rawvideo -vcodec rawvideo  -pixel_format {pixFmt} -video_size {width}x{height} -r {framerate} -i - -vf \"{vfOpt}\" {outputOpt} \"{filename}\"";

        Debug.Log($"ffmpeg command: {"ffmpeg "+ opt}");
        Debug.Log($"Working directory: {Directory.GetCurrentDirectory()}");

        var info = new ProcessStartInfo("ffmpeg", opt);
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;

        ffmpegProcess = Process.Start(info);
        if (ffmpegProcess == null) {
            Debug.Error("Failed to start ffmpeg process!");
            return;
        }
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
        // anyone got an elevator?
        if(ffmpegProcess != null && ffmpegStdin != null) {
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

    public void Stop(bool cancelled = false) {
        if(ffmpegProcess != null) {
            if (cancelled) {
                Console.WriteLine("ffmpeg stream cancelled! Killed process.");
                ffmpegProcess.Kill();
            }
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
        videofile = videofile.Replace(".mp4", ".mkv");
        var opt = $"-y -i {videofile} -f s16le -i - -c:v copy {videofile + "-audio.mkv"}";
        //var opt = $"-y -i {videofile} -f s16le -i - -c:v copy {videofile + "-audio.mp4"}";
        Debug.Log($"Audio command: {"ffmpeg "+ opt}");

        var info = new ProcessStartInfo("ffmpeg", opt);
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;

        ffmpegProcess = Process.Start(info);
        if (ffmpegProcess == null) {
            Debug.Error("Failed to start ffmpeg process!");
            return;
        }
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
