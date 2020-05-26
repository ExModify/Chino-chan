using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                LinuxCpuInfo inf = new LinuxCpuInfo();
                inf.GetValues();
                Name = inf.ModelName;
                Speed = (int)inf.MHz;
                Threads = inf.Cores;
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
            RegistryKey sk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\MICROSOFT\\Windows NT\\CurrentVersion");
            Name = (string)sk.GetValue("ProductName", "");
            Version = "10.0." + (string)sk.GetValue("CurrentBuild", "") + " " + (string)sk.GetValue("ReleaseId", "");
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
        }
        public long TotalMemory { get; private set; }

        public MemInfo()
        {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
            {
                TotalMemory = Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1024 / 1024));
            }
            else
            {
                TotalMemory = - 1;
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
    public class LinuxCpuInfo
    {
        public string ModelName { get; private set; }
        public int Cores { get; private set; }
        public double MHz { get; private set; }

        public void GetValues()
        {
            string[] cpuInfoLines = File.ReadAllLines(@"/proc/cpuinfo");

            CpuInfoMatch[] cpuInfoMatches =
            {
                new CpuInfoMatch(@"^model name\s+:\s+(.+)", value => ModelName = value),
                new CpuInfoMatch(@"^cpu MHz\s+:\s+(.+)", value => MHz = Convert.ToDouble(value)),
                new CpuInfoMatch(@"^cpu cores\s+:\s+(.+)", value => Cores = Convert.ToInt32(value))
            };

            foreach (string cpuInfoLine in cpuInfoLines)
            {
                foreach (CpuInfoMatch cpuInfoMatch in cpuInfoMatches)
                {
                    Match match = cpuInfoMatch.regex.Match(cpuInfoLine);
                    if (match.Groups[0].Success)
                    {
                        string value = match.Groups[1].Value;
                        cpuInfoMatch.updateValue(value);
                    }
                }
            }
        }

        public class CpuInfoMatch
        {
            public Regex regex;
            public Action<string> updateValue;

            public CpuInfoMatch(string pattern, Action<string> update)
            {
                this.regex = new Regex(pattern, RegexOptions.Compiled);
                this.updateValue = update;
            }
        }
    }
}
