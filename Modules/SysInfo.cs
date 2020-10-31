using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Chino_chan.Modules
{
    public class CPUInfo
    {
        public string Name { get; private set; }
        public int Speed { get; private set; }
        public int Threads { get; private set; }

        public CPUInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string data = File.ReadAllText("/proc/cpuinfo");
                string model = @"model name\s+:\s+(.+)\n";
                string speed = @"cpu MHz\s+:\s+(.+)\n";
                string threads = @"cpu cores\s+:\s+(.+)\n";

                Name = Regex.Match(data, model).Groups[1].Value;
                Speed = (int)Convert.ToDouble(Regex.Match(data, speed).Groups[1].Value, CultureInfo.GetCultureInfo("en-US"));
                Threads = Convert.ToInt32(Regex.Match(data, threads).Groups[1].Value);
            }
            else
            {

                var CPUKey = "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0";
                Name = (string)Registry.LocalMachine.OpenSubKey(CPUKey).GetValue("ProcessorNameString", "N/A");
                Speed = Convert.ToInt32(Registry.LocalMachine.OpenSubKey(CPUKey).GetValue("~MHz", 0));
                

                Threads = Environment.ProcessorCount;
            }
        }
    }
    public class OsInfo
    {
        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Architecture { get; private set; }

        public OsInfo()
        {
            Version = "N/A";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RegistryKey sk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\MICROSOFT\\Windows NT\\CurrentVersion");
                Name = (string)sk.GetValue("ProductName", "");
                Version = (string)sk.GetValue("CurrentVersion", "") + "." + (string)sk.GetValue("CurrentBuild", "") + " " + (string)sk.GetValue("ReleaseId", "");
            }
            else
            {
                string[] lines = File.ReadAllLines("/etc/os-release");
                foreach (string line in lines)
                {
                    if (line.StartsWith("NAME"))
                    {
                        Name = line.Split('=')[1];
                        Name = Name.Replace("\"", "");
                    }
                    else if (line.StartsWith("VERSION=")) 
                    {
                        Version = line.Split('=')[1];
                        Version = Version.Replace("\"", "");
                    }
                }
            }

            Architecture = Environment.Is64BitOperatingSystem == true ? "64-bit" : "32-bit";
        }
    }
    public class MemInfo
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        public long FreeMemory
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    PerformanceInformation pi = new PerformanceInformation();
                    if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                    {
                        return Convert.ToInt64((pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1024 / 1024));
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    string[] lines = File.ReadAllLines("/proc/meminfo");
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("MemFree"))
                        {
                            return Convert.ToInt64(line.Split(':')[1].Replace("kB", "").Trim()) / 1024;
                        }
                    }
                    return -1;
                }
            }
        }
        public long TotalMemory { get; private set; }

        public MemInfo()
        {
            TotalMemory = -1;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                PerformanceInformation pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    TotalMemory = Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1024 / 1024));
                }
            }
            else
            {
            
                string[] lines = File.ReadAllLines("/proc/meminfo");
                foreach (string line in lines)
                {
                    if (line.StartsWith("MemTotal"))
                    {
                        TotalMemory = Convert.ToInt64(line.Split(':')[1].Replace("kB", "").Trim()) / 1024;
                        break;
                    }
                }
            }
        }
    }
    public class VideoCardInfo
    {
        public struct CardInfo
        {
            public string Name { get; private set; }
            public ulong RAM { get; private set; }

            public CardInfo(string Name, ulong RAM)
            {
                this.Name = Name;
                this.RAM = RAM;
            }
        }

        public List<CardInfo> VideoCards = new List<CardInfo>();
        
        public VideoCardInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            {
                RegistryKey mainkey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\DirectX");
                RegistryKey mainkey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\DirectX");

                string[] names32 = mainkey32.GetSubKeyNames();
                string[] names64 = mainkey64.GetSubKeyNames();

                for (int i = 0; i < names32.Length; i++)
                {
                    RegistryKey subkey = mainkey32.OpenSubKey(names32[i]);
                    ulong mem = Convert.ToUInt64(subkey.GetValue("DedicatedVideoMemory", "0x0"));
                    if (mem > 0)
                    {
                        VideoCards.Add(new CardInfo((string)subkey.GetValue("Description", "0x0"), mem));
                    }
                }
                for (int i = 0; i < names64.Length; i++)
                {
                    RegistryKey subkey = mainkey64.OpenSubKey(names64[i]);
                    ulong mem = Convert.ToUInt64(subkey.GetValue("DedicatedVideoMemory", "0x0"));
                    if (mem > 0)
                    {
                        VideoCards.Add(new CardInfo((string)subkey.GetValue("Description", "0x0"), mem));
                    }
                }
            }
            else
            {
                ProcessStartInfo info = new ProcessStartInfo() {
                    FileName = "lspci",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                Process p = Process.Start(info);
                p.WaitForExit();
                string[] gpus = p.StandardOutput.ReadToEnd().Split('\n').Where(t => t.Contains("VGA")).ToArray();
                foreach (string gpu in gpus)
                {
                    Logger.Log(LogType.SysInfo, ConsoleColor.Green, null, gpu);
                    CardInfo ci = new CardInfo(gpu.Split(':')[2].Trim(), 0);
                    VideoCards.Add(ci);
                }
                
            }
        }
    }

    public class SysInfo
    {
        public CPUInfo CPU { get; private set; }
        public OsInfo OS { get; private set; }
        public MemInfo MemInfo { get; private set; }
        public VideoCardInfo VideoCardInfo { get; private set; }

        public SysInfo() { }

        public void Load()
        {
            Logger.Log(LogType.SysInfo, ConsoleColor.Magenta, null, "Loading system information...");

            CPU = new CPUInfo(); // 0ms
            OS = new OsInfo(); // 0ms
            MemInfo = new MemInfo(); // 7-8ms
            VideoCardInfo = new VideoCardInfo(); // 1ms

            Logger.Log(LogType.SysInfo, ConsoleColor.Magenta, null, "System information loaded!");
        }
    }
}
