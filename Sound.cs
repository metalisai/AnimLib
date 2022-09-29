using ManagedBass;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;

namespace AnimLib {

    public class BakedSound {
        public SoundTrack[] tracks;
    }

    public class SoundTrack {
        public double length;
        public int sampleRate;
        public int Channels {
            get { return samples.Length; }
        }
        public int SamplesPerChannel {
            get { return samples[0].Length; }
        }
        public double Duration {
            get { return (double)samples[0].Length / (double)sampleRate; }
        }
        public short[][] samples;

        public SoundTrack(int sampleRate, int channels, double length) {
            int samplesPerChannel = (int)Math.Round((double)sampleRate * length);
            this.samples = new short[channels][];
            for(int i = 0 ; i < channels; i++) {
                this.samples[i] = new short[samplesPerChannel];
            }
            this.length = length; 
            this.sampleRate = sampleRate;
        }

        public void PushSound(short[,] samples, double startTime, float volume = 1.0f) {
            if(samples.GetLength(0) != Channels)
                throw new Exception("Pushing sound with different channel count");
            int start = (int)Math.Round(startTime * sampleRate);
            // startTime past end of track
            if(start >= SamplesPerChannel)
                return;
            // last sample + 1
            int end = start + Math.Min(SamplesPerChannel, samples.GetLength(1));
            for(int ch = 0; ch < Channels; ch++) {
                for(int i = start; i < end; i++) {
                    int data = (int)(volume*(samples[ch, i-start] + this.samples[ch][i]));
                    this.samples[ch][i] += (short)Math.Clamp(data, short.MinValue, short.MaxValue);
                }
            }
        }

        public void PushSoundMono(short[] samples, double startTime, float volume = 1.0f) {
            int start = (int)Math.Round(startTime * sampleRate);
            // startTime past end of track
            if(start >= SamplesPerChannel)
                return;
            // last sample + 1
            int end = start + Math.Min(SamplesPerChannel, samples.Length);
            end = Math.Min(end, this.samples[0].Length);
            for(int ch = 0; ch < Channels; ch++) {
                for(int i = start; i < end; i++) {
                    var data = (int)(volume*(samples[i-start] + this.samples[ch][i]));
                    this.samples[ch][i] += (short)Math.Clamp(data, short.MinValue, short.MaxValue);
                }
            }
        }

        public void PushSoundMono(Span<short> samples, double startTime, float volume = 1.0f) {
            int start = (int)Math.Round(startTime * sampleRate);
            // startTime past end of track
            if(start >= SamplesPerChannel)
                return;
            // last sample + 1
            int end = start + Math.Min(SamplesPerChannel, samples.Length);
            for(int ch = 0; ch < Channels; ch++) {
                for(int i = start; i < end; i++) {
                    var data = (int)(volume*(samples[i-start] + this.samples[ch][i]));
                    this.samples[ch][i] += (short)Math.Clamp(data, short.MinValue, short.MaxValue);
                }
            }
        }

        public void PushSample(SoundSample sample, double startTime, float volume = 1.0f) {
            if(sample.sampleRate != 44100) {
                Debug.Warning($"Sample rate {sample.sampleRate} played as 44100");
            }
            PushSoundMono(sample.samples[0], startTime, volume);
        }
    }

    public class TrackPlayer : IDisposable {
        int sampleIndex;

        int SampleIndex {
            get { return sampleIndex; }
            set {
                int safeUp = Math.Max(value, 0);
                int safe = Math.Min(safeUp, currentTrack.SamplesPerChannel);
                sampleIndex = safe;
            }
        }

        SoundTrack currentTrack;
        bool _playing = false;

        int bassStream;

        public SoundTrack Track {
            get {
                return currentTrack;
            } set {
                sampleIndex = 0;
                currentTrack = value;
            }
        }

        public TrackPlayer() {
            bassStream = Bass.CreateStream(44100, 1, BassFlags.Default, StreamProc);
        }

        public void Play() {
            Debug.TLog("Animation track Play()");
            Bass.ChannelPlay(bassStream);
            _playing = true;
        }

        public void Pause() {
            Debug.TLog("Animation track Pause()");
            Bass.ChannelPause(bassStream);
            _playing = false;
        }

        public void Seek(double progress) {
            if(currentTrack == null) // no track, cant seek
                return;
            Bass.ChannelStop(bassStream);
            if(_playing)
                Bass.ChannelPlay(bassStream);
            var pos = Bass.ChannelGetPosition(bassStream, PositionFlags.Relative);
            Debug.TLog($"position {pos}");
            Debug.TLog($"Track seek {progress}");
            double seconds = progress * currentTrack.Duration;
            int sample = (int)Math.Round(seconds * currentTrack.sampleRate);
            SampleIndex = sample;
        }

        private int StreamProc(int handle, IntPtr buffer, int len, IntPtr usrPtr) {
            int samplesLeft;
            if(currentTrack != null) {
                samplesLeft = currentTrack.SamplesPerChannel - sampleIndex;
            } else {
                samplesLeft = 0;
            }
            int copyCount = Math.Min(samplesLeft, len/2);
            int zeroCount = len/2 - copyCount;
            if(copyCount > 0) {
                Marshal.Copy(currentTrack.samples[0], sampleIndex, buffer, copyCount);
                sampleIndex += copyCount;
            }
            if(zeroCount > 0) {
                unsafe {
                    short *data = (short*)buffer + copyCount;
                    for(int i = 0; i < zeroCount; i++) {
                        data[i] = 0;
                    }
                }
            }
            return len;
        }

        public void Dispose() {
            //if(_playing) {
                Bass.ChannelStop(bassStream);
                //_playing = false;
            //}
            Bass.StreamFree(bassStream);
        }

    }

    public enum BuiltinSound {
        Keyboard_Click1,
        Keyboard_Click2,
        Keyboard_Click3,
        Keyboard_Click4,
        Keyboard_Click5,
        Keyboard_Click6,
        Keyboard_Click7,
        Keyboard_Click8,
        Keyboard_Click9,
    }

    public struct SoundSample {

        private static Dictionary<BuiltinSound, (string, string)> builtins = new Dictionary<BuiltinSound, (string, string)>() {
            {BuiltinSound.Keyboard_Click1, ("sounds", "keyboard_click1.ogg")},
            {BuiltinSound.Keyboard_Click2, ("sounds", "keyboard_click2.ogg")},
            {BuiltinSound.Keyboard_Click3, ("sounds", "keyboard_click3.ogg")},
            {BuiltinSound.Keyboard_Click4, ("sounds", "keyboard_click4.ogg")},
            {BuiltinSound.Keyboard_Click5, ("sounds", "keyboard_click5.ogg")},
            {BuiltinSound.Keyboard_Click6, ("sounds", "keyboard_click6.ogg")},
            {BuiltinSound.Keyboard_Click7, ("sounds", "keyboard_click7.ogg")},
            {BuiltinSound.Keyboard_Click8, ("sounds", "keyboard_click8.ogg")},
            {BuiltinSound.Keyboard_Click9, ("sounds", "keyboard_click9.ogg")},
        };

        private static ConcurrentDictionary<BuiltinSound, SoundSample> cache = new ConcurrentDictionary<BuiltinSound, SoundSample>();

        public int sampleRate;
        public int channels;
        public short[][] samples;

        private static SoundSample SoundFileToSample (byte[] data) {
            var sample = Bass.SampleLoad(data, 0, data.Length, 1, BassFlags.Default);
            var sinfo = new SampleInfo();
            var info = Bass.SampleGetInfo(sample, sinfo);
            byte[] sampleData = new byte[sinfo.Length];
            Bass.SampleGetData(sample, sampleData);
            var sampleData16 = MemoryMarshal.Cast<byte, short>(sampleData.AsSpan()).ToArray();
            return new SoundSample {
                sampleRate = sinfo.Frequency,
                channels = sinfo.Channels,
                samples = new short[1][] {sampleData16},
            };
        }

        public static SoundSample? GetFromStream(Stream file) {
            if(file == null) return null;
            var ms = new MemoryStream();
            file.CopyTo(ms);
            var data = ms.ToArray();
            return SoundFileToSample(data);
        }

        public static SoundSample GetBuiltin(BuiltinSound sound) {
            SoundSample ret;
            if(!cache.TryGetValue(sound, out ret)) {
                var bs = builtins[sound];
                var data = EmbeddedResources.GetResourceBytes(bs.Item1, bs.Item2);
                ret = SoundFileToSample(data);
            }
            cache[sound] = ret;
            return ret;
        }
    }

    public class Sound {

        BassInfo bassInfo;

        private void InitBass() {
            Bass.UpdatePeriod = 10;

            var res = EmbeddedResources.GetResource("txt", "test.txt");
            var text = new StreamReader(res).ReadToEnd();
            Debug.TLog($"File content: {text}");

            if(!Bass.Init(-1, 44100, DeviceInitFlags.Latency))
            {
                Debug.Error("Failed to initialize audio device");
            }
            Bass.GetInfo(out bassInfo);

            /*var data = EmbeddedResources.GetResourceBytes("sounds", "keyboard_click1.ogg");
            var sample = Bass.SampleLoad(data, 0, data.Length, 1, BassFlags.Default);
            var ch = Bass.SampleGetChannel(sample);
            var sinfo = new SampleInfo();
            var info = Bass.SampleGetInfo(sample, sinfo);
            Bass.ChannelPlay(ch);
            Debug.TLog($"sample {sinfo.Channels} channels {sinfo.Frequency} hz");
            byte[] sampleData = new byte[sinfo.Length];
            Bass.SampleGetData(sample, sampleData);*/

            /*var sampleData16 = MemoryMarshal.Cast<byte, short>(sampleData.AsSpan());
            var track = new SoundTrack(44100, 1, 60.0);
            track.PushSoundMono(sampleData16, 3.0);
            track.PushSoundMono(sampleData16, 4.0);
            track.PushSoundMono(sampleData16, 5.0);
            track.PushSoundMono(sampleData16, 5.5);
            track.PushSoundMono(sampleData16, 6.0);
            //var stream = Bass.CreateStream(data, 0, data.Length, BassFlags.Default);
            //Bass.ChannelPlay(stream);
            //System.Threading.Thread.Sleep(100);
            
            var player = new TrackPlayer();
            player.Track = track;
            player.Play();*/
            
        }

        public Sound() {
            InitBass();
        }
    }
}
