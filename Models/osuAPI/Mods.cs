using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public enum Mods
    {
        None =          0,
        NoFail =        1,
        Easy =          2,
        NoVideo  =      4,
        Hidden =        8,
        HardRock =      16,
        SuddenDeath =   32,
        DoubleTime =    64,
        Relax =         128,
        HalfTime =      256,
        Nightcore =     512,
        Flashlight =    1024,
        Autoplay =      2048,
        SpunOut =       4096,
        AutoPilot =     8192,
        Perfect =       16384,
        Key4 =          32768,
        Key5 =          65536,
        Key6 =          131072,
        Key7 =          262144,
        Key8 =          524288,
        keyMod = Key4 | Key5 | Key6 | Key7 | Key8,
        FadeIn =        1048576,
        Random =        2097152,
        LastMod =       4194304,
        FreeModAllowed = NoFail | Easy | Hidden | HardRock | SuddenDeath | Flashlight | FadeIn | Relax | AutoPilot | SpunOut | keyMod,
        Key9 =          16777216,
        Key10 =         33554432,
        Key1 =          67108864,
        Key3 =          134217728,
        Key2 =          268435456
    }
    public enum ShortPPMods
    {
        None = 0,
        NF = 1,
        EZ = 2,
        HD = 8,
        HR = 16,
        DT = 64,
        HT = 256,
        NC = 512,
        FL = 1024,
        SO = 4096
    }
}
