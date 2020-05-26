using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using osuBeatmapUtilities;

namespace Chino_chan.Modules
{
    public class PPCalcSTDReport
    {
        public double Accuracy { get; set; }
        public double MaxCombo { get; set; }
        public double PP { get; set; }
        public double SameAccFCPP { get; set; }
        public double MaxPP { get; set; }
        public double Stars { get; set; }

        public double AR { get; set; }
        public double OD { get; set; }

        public Beatmap Beatmap { get; private set; }
        public double HitCount { get; private set; }

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
            Beatmap = map;
            HitCount = map.WorkingBeatmap.Beatmap.HitObjects.Count;
        }
    }

    public class PPCalculator
    {
        public static PPCalcSTDReport CountStd(int BeatmapID, int Combo = 0, int s100 = 0, int s50 = 0, int Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            Beatmap b = Global.Beatmaps.LoadBeatmap(BeatmapID);
            PPCalcSTDReport report = new PPCalcSTDReport(Calculator.CalculateSTDPerformance(b, 0, s100, s50, Misses, Combo, Acc,(Mods)EnabledMods), b);
            return report;
        }
        public static PPCalcSTDReport CountStd(string BeatmapFile, int Combo = 0, int s100 = 0, int s50 = 0, int Misses = 0, double Acc = 0, int EnabledMods = 0)
        {
            Beatmap b = Global.Beatmaps.LoadBeatmapFromFile(BeatmapFile);
            PPCalcSTDReport report = new PPCalcSTDReport(Calculator.CalculateSTDPerformance(b, 0, s100, s50, Misses, Combo, Acc,(Mods)EnabledMods), b);
            return report;
        }
    }
}
