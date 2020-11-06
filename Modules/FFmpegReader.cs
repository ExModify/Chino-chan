using System.Net;
using System.Threading;
using System;
using System.Diagnostics;
using System.IO;

namespace Chino_chan.Modules
{
    public class FFmpegReader : IDisposable
    {
        private int SampleRate { get; } = 48000;
        private int ChannelCount { get; } = 2;
        private int BitDepth { get; } = 16;

        private Process FFmpegProcess;
        private StreamReader Reader { get => FFmpegProcess.StandardOutput; }

        public string URL { get; }
        //public TimeSpan CurrentTime { get => ToTimeSpan(); }
        public TimeSpan TotalTime { get; }

        public bool CanRead { get => Reader.BaseStream.CanRead; }

        public FFmpegReader(string Url)
        {
            URL = Url;
            TotalTime = GetTotalTime();

            FFmpegProcess = CreateFFmpeg(Url);

            Console.WriteLine(FFmpegProcess.StartInfo.Arguments);
        }

        public int Read(byte[] Buffer, int Offset, int Count)
        {
            return Reader.BaseStream.ReadAsync(Buffer, Offset, Count).Result;
        }
        public int BufferSize(int Seconds)
        {
            return (int)(SampleRate * (BitDepth / 8) * ChannelCount * Seconds);
        }
        public void SetTime(TimeSpan Time)
        {
            long length = ToLength(Time);
            while (Reader.BaseStream.Length < length)
            {
                Thread.Sleep(10);
            }

            Reader.BaseStream.Position = length;
        }
        public void ReadUntil(TimeSpan Time)
        {

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
            return TimeSpan.FromSeconds(Reader.BaseStream.Position / (SampleRate *  (BitDepth / 8) * ChannelCount));
        }

        public void Dispose()
        {
            Reader.Dispose();
            if (!FFmpegProcess.HasExited) try { FFmpegProcess.Kill(); } catch { }
            GC.Collect();
        }
    }
}