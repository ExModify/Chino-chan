using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using Chino_chan.Models.PerformanceCalculator;
using Newtonsoft.Json;
using osuBeatmapUtilities;

namespace Chino_chan.Modules
{
    public class PPCalcSTDReport
    {
        public double Accuracy { get; set; }
        public double MaxCombo { get; set; }
        public double TotalMaxCombo { get; set; }

        public double PP { get; set; }
        public double SameAccFCPP { get; set; }
        public double MaxPP { get; set; }
        public double Stars { get; set; }

        public double AR { get; set; }
        public double OD { get; set; }
        public double SR { get; private set; }

        public Beatmap Beatmap { get; private set; }
        public double HitCount { get; private set; }

        public long Great { get; private set; }
        public long Good { get; private set; }
        public long Meh { get; private set; }
        public long Miss { get; private set; }



        public PPCalcSTDReport(OsuReport calcReport, Beatmap map)
        {
            Accuracy = calcReport.Accuracy;
            MaxCombo = calcReport.MaxCombo;
            MaxPP = calcReport.MaxPP;
            SameAccFCPP = calcReport.SameAccPP;
            PP = calcReport.PP;
            Stars = calcReport.StarRating;
            AR = calcReport.AR;
            OD = calcReport.OD;
            SR = calcReport.StarRating;
            Beatmap = map;
            HitCount = map.WorkingBeatmap.Beatmap.HitObjects.Count;
        }
        public PPCalcSTDReport(PpcOutput ppc, PpcOutput ppcFc, PpcOutput ppcMax, Beatmap map)
        {
            Accuracy = ppc.Score.Accuracy;
            MaxCombo = ppcMax.Score.Combo;
            TotalMaxCombo = ppcMax.Score.Combo;
            MaxPP = ppcMax.PP;
            SameAccFCPP = ppcFc.PP;
            PP = ppc.PP;
            Stars = ppc.DifficultyAttributes.StarRating;
            AR = ppc.DifficultyAttributes.ApproachRate;
            OD = ppc.DifficultyAttributes.OverallDifficulty;
            SR = ppc.DifficultyAttributes.StarRating;
            Beatmap = map;
            HitCount = map.WorkingBeatmap.Beatmap.HitObjects.Count;

            Great = ppc.Score.Statistics.Great;
            Good = ppc.Score.Statistics.Ok;
            Meh = ppc.Score.Statistics.Meh;
            Miss = ppc.Score.Statistics.Miss;
        }
    }

    public class PPCalculator
    {
        private static bool UseFallbackPPCalculator = true;

        public static void Init()
        {
            Logger.Log(LogType.Info, ConsoleColor.Gray, "osu!PP", "Checking if PerformanceCalculator can be started...");
            try
            {
                string op = Process.Start(GenerateInfo()).StandardOutput.ReadToEnd();
                UseFallbackPPCalculator = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Logger.Log(LogType.Info, ConsoleColor.Yellow, "osu!PP", "PerformanceCalculator can't be found, using outdated PP calculator");
            }
        }


        public static PPCalcSTDReport CountStd(int BeatmapID, long Combo = 0, long s100 = 0, long s50 = 0, long Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            if (UseFallbackPPCalculator)
                return CountStdFallback(BeatmapID, (int)Combo, (int)s100, (int)s50, (int)Misses, Acc, EnabledMods);
            return CountStdPPC(BeatmapID, Combo, s100, s50, Misses, Acc, EnabledMods);
        }
        public static PPCalcSTDReport CountStd(string BeatmapFile, long Combo = 0, long s100 = 0, long s50 = 0, long Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            if (UseFallbackPPCalculator)
                return CountStdFallback(BeatmapFile, (int)Combo, (int)s100, (int)s50, (int)Misses, Acc, EnabledMods);
            return CountStdPPC(BeatmapFile, Combo, s100, s50, Misses, Acc, EnabledMods);
        }

        public static PPCalcSTDReport CountStdFallback(int BeatmapID, int Combo = 0, int s100 = 0, int s50 = 0, int Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            Beatmap b = Global.Beatmaps.LoadBeatmap(BeatmapID);
            PPCalcSTDReport report = new PPCalcSTDReport(Calculator.CalculateSTDPerformance(b, 0, s100, s50, Misses, Combo, Acc,(Mods)EnabledMods), b);
            return report;
        }
        public static PPCalcSTDReport CountStdFallback(string BeatmapFile, int Combo = 0, int s100 = 0, int s50 = 0, int Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            Beatmap b = Global.Beatmaps.LoadBeatmapFromFile(BeatmapFile);
            PPCalcSTDReport report = new PPCalcSTDReport(Calculator.CalculateSTDPerformance(b, 0, s100, s50, Misses, Combo, Acc,(Mods)EnabledMods), b);
            return report;
        }
        public static PPCalcSTDReport CountStdPPC(string BeatmapFile, long Combo = 0, long s100 = 0, long s50 = 0, long Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            Beatmap b = Global.Beatmaps.LoadBeatmap(BeatmapFile);
            string mods = EnabledMods == 0 ? "" : " -m " + Commands.osu.GetShortMods(EnabledMods, " -m ");
            if (mods == " -m ")
                mods = "";

            string argBase = "simulate osu " + Path.Combine(Global.Beatmaps.Folder, b.BeatmapId + ".osu") + mods + " -j ";

            string acc = Acc == 0 ? $" -G { s100 } -M { s50 }" : $" -a { Acc }";
            string combo = Combo == 0 ? "" : $"-c { Combo } ";

            string ppcRaw = Process.Start(GenerateInfo(argBase + $"{ combo }-X { Misses }{ acc }")).StandardOutput.ReadToEnd();
            PpcOutput ppc = JsonConvert.DeserializeObject<PpcOutput>(ppcRaw);

            string ppcRawFc = Process.Start(GenerateInfo(argBase + $"-X 0 -a { ppc.Score.Accuracy }")).StandardOutput.ReadToEnd();
            string ppcRawMax = Process.Start(GenerateInfo(argBase)).StandardOutput.ReadToEnd();
            PpcOutput ppcFc = JsonConvert.DeserializeObject<PpcOutput>(ppcRawFc);
            PpcOutput ppcMax = JsonConvert.DeserializeObject<PpcOutput>(ppcRawMax);

            return new PPCalcSTDReport(ppc, ppcFc, ppcMax, b);
        }
        public static PPCalcSTDReport CountStdPPC(int BeatmapID, long Combo = 0, long s100 = 0, long s50 = 0, long Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            Beatmap b = Global.Beatmaps.LoadBeatmap(BeatmapID);
            string mods = EnabledMods == 0 ? "" : " -m " + Commands.osu.GetShortMods(EnabledMods, " -m ");
            if (mods == " -m ")
                mods = "";

            string argBase = "simulate osu " + Path.Combine(Global.Beatmaps.Folder, BeatmapID + ".osu") + mods + " -j ";

            string acc = Acc == 0 ? $" -G { s100 } -M { s50 }" : $" -a { Acc }";
            string combo = Combo == 0 ? "" : $"-c { Combo } ";

            string ppcRaw = Process.Start(GenerateInfo(argBase + $"{combo}-X { Misses }{ acc }")).StandardOutput.ReadToEnd();
            PpcOutput ppc = JsonConvert.DeserializeObject<PpcOutput>(ppcRaw);

            string ppcRawFc = Process.Start(GenerateInfo(argBase + $"-X 0 -a { ppc.Score.Accuracy }")).StandardOutput.ReadToEnd();
            string ppcRawMax = Process.Start(GenerateInfo(argBase)).StandardOutput.ReadToEnd();
            PpcOutput ppcFc = JsonConvert.DeserializeObject<PpcOutput>(ppcRawFc);
            PpcOutput ppcMax = JsonConvert.DeserializeObject<PpcOutput>(ppcRawMax);

            return new PPCalcSTDReport(ppc, ppcFc, ppcMax, b);
        }

        private static ProcessStartInfo GenerateInfo(string Args = null)
        {
            return new ProcessStartInfo()
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Global.Settings.PPCPath,
                Arguments = Args ?? ""
            };
        }
    }
}
