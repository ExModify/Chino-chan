using System.Net;
using System.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    public class FFmpegReader : IDisposable
    {
        private int SampleRate { get; } = 48000;
        private int ChannelCount { get; } = 2;
        private int BitDepth { get; } = 16;

        private Process FFmpegProcess;
        private MemoryStream MemoryStream;
        private StreamReader FFMpegStreamReader { get => FFmpegProcess.StandardOutput; }

        public string URL { get; }
        public TimeSpan CurrentTime { get => ToTimeSpan(); }
        public TimeSpan TotalTime { get; }

        public bool CanRead { get => FFmpegProcess.StandardOutput.BaseStream.CanRead; }
        public bool FFMpegRunning { get => !FFmpegProcess.HasExited; }

        public bool Seeking = false;

        public FFmpegReader(string Url)
        {
            URL = Url;
            TotalTime = GetTotalTime();

            FFmpegProcess = CreateFFmpeg(Url);

            MemoryStream = new MemoryStream();
        }

        public int Read(byte[] Buffer, int Offset, int Count)
        {
            while (Seeking) Thread.Sleep(10);

            if (MemoryStream.Length - MemoryStream.Position < Buffer.Length + Offset)
            {
                int r = FFmpegProcess.StandardOutput.BaseStream.Read(Buffer, 0, Buffer.Length);
                if (r != 0)
                {
                    MemoryStream.Position = MemoryStream.Length;
                    MemoryStream.Write(Buffer, 0, r);
                    return r;
                }
            }

            return MemoryStream.ReadAsync(Buffer, Offset, Count).Result;
        }
        public int BufferSize(int Seconds)
        {
            return (int)(SampleRate * (BitDepth / 8) * ChannelCount * Seconds);
        }
        public void SetTime(TimeSpan Time)
        {
            Seeking = true;
            MemoryStream.Position = ReadUntil(Time);
            Seeking = false;
        }
        public long ReadUntil(TimeSpan Time)
        {
            byte[] b = new byte[BufferSize(5)];
            long l = ToLength(Time);
            while (MemoryStream.Length < l)
            {
                int r = FFmpegProcess.StandardOutput.BaseStream.Read(b, 0, b.Length);
                if (r != 0)
                {
                    MemoryStream.Position = MemoryStream.Length;
                    MemoryStream.Write(b, 0, r);
                }
                else
                {
                    return l;
                }
            }
            return l;
        }
        private Process CreateFFmpeg(string Url)
        {
            var proc = Process.Start(new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Arguments = $"-hide_banner -loglevel panic -i \"{ Url }\" -ac { ChannelCount } -f s16le -ar { SampleRate } pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            return proc;
        }
        private TimeSpan GetTotalTime()
        {
            Process p = Process.Start(new ProcessStartInfo()
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{ URL }\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            p.WaitForExit();
            string d = p.StandardOutput.ReadToEnd().Trim();
            double time;
            if (!double.TryParse(d, out time))
            {
                if (d.Contains('.'))
                    time = double.Parse(d.Replace('.', ','));
                else time = double.Parse(d.Replace(',', '.'));
            }
            return TimeSpan.FromSeconds(time);
        }
        private long ToLength(TimeSpan Time)
        {
            return (long)(SampleRate * (BitDepth / 8) * ChannelCount * Time.TotalSeconds);
        }
        private TimeSpan ToTimeSpan()
        {
            return TimeSpan.FromSeconds(MemoryStream.Position / (SampleRate *  (BitDepth / 8) * ChannelCount));
        }

        public void Dispose()
        {
            MemoryStream.Dispose();
            if (!FFmpegProcess.HasExited) try { FFmpegProcess.Kill(); } catch { }
            GC.Collect();
        }
    }
}