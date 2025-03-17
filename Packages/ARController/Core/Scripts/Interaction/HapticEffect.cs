/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

/*
 * Uncomment the following line to opt in to alpha-quality Windows Bluetooth support.
 * Before doing so, please read the documentation for this feature.
 * Disabling this functionality fully will require a restart of the Unity Editor (the Litho DLL will remain loaded even after exiting Play mode).
 */
//#define WINDOWS_BLUETOOTH_ALPHA_OPTIN

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace LITHO
{

    /// <summary>
    /// Represents a Litho haptic effect
    /// </summary>
    public static class HapticEffect
    {
        public enum Type : byte
        {
            StrongClick_100 = 1,    // 1 Strong Click - 100%
            StrongClick_60,         // 2 Strong Click - 60%
            StrongClick_30,         // 3 Strong Click - 30%
            SharpClick_100,         // 4 Sharp Click - 100%
            SharpClick_60,          // 5 Sharp Click - 60%
            SharpClick_30,          // 6 Sharp Click - 30%
            SoftBump_100,           // 7 Soft Bump - 100%
            SoftBump_60,            // 8 Soft Bump - 60%
            SoftBump_30,            // 9 Soft Bump - 30%
            DoubleClick_100,        // 10 Double Click - 100%
            DoubleClick_60,         // 11 Double Click - 60%
            TripleClick_100,        // 12 Triple Click - 100%
            SoftFuzz_60,            // 13 Soft Fuzz - 60%
            StrongBuzz_100,         // 14 Strong Buzz - 100%
            Alert_750ms_100,        // 15 750 ms Alert 100%
            Alert_1000ms_100,       // 16 1000 ms Alert 100%
            StrongClick_1_100,      // 17 Strong Click 1 - 100%
            StrongClick_2_80,       // 18 Strong Click 2 - 80%
            StrongClick_3_60,       // 19 Strong Click 3 - 60%
            StrongClick_4_30,       // 20 Strong Click 4 - 30%
            MediumClick_1_100,      // 21 Medium Click 1 - 100%
            MediumClick_2_80,       // 22 Medium Click 2 - 80%
            MediumClick_3_60,       // 23 Medium Click 3 - 60%
            SharpTick_1_100,        // 24 Sharp Tick 1 - 100%
            SharpTick_2_80,         // 25 Sharp Tick 2 - 80%
            SharpTick_3_60,         // 26 Sharp Tick 3 – 60%
            ShortDoubleClickStrong_1_100,       // 27 Short Double Click Strong 1 – 100%
            ShortDoubleClickStrong_2_80,        // 28 Short Double Click Strong 2 – 80%
            ShortDoubleClickStrong_3_60,        // 29 Short Double Click Strong 3 – 60%
            ShortDoubleClickStrong_4_30,        // 30 Short Double Click Strong 4 – 30%
            ShortDoubleClickMedium_1_100,       // 31 Short Double Click Medium 1 – 100%
            ShortDoubleClickMedium_2_80,        // 32 Short Double Click Medium 2 – 80%
            ShortDoubleClickMedium_3_60,        // 33 Short Double Click Medium 3 – 60%
            ShortDoubleSharpTick_1_100,         // 34 Short Double Sharp Tick 1 – 100%
            ShortDoubleSharpTick_2_80,          // 35 Short Double Sharp Tick 2 – 80%
            ShortDoubleSharpTick_3_60,          // 36 Short Double Sharp Tick 3 – 60%
            LongDoubleSharpClickStrong_1_100,   // 37 Long Double Sharp Click Strong 1 – 100%
            LongDoubleSharpClickStrong_2_80,    // 38 Long Double Sharp Click Strong 2 – 80%
            LongDoubleSharpClickStrong_3_60,    // 39 Long Double Sharp Click Strong 3 – 60%
            LongDoubleSharpClickStrong_4_30,    // 40 Long Double Sharp Click Strong 4 – 30%
            LongDoubleSharpClickMedium_1_100,   // 41 Long Double Sharp Click Medium 1 – 100%
            LongDoubleSharpClickMedium_2_80,    // 42 Long Double Sharp Click Medium 2 – 80%
            LongDoubleSharpClickMedium_3_60,    // 43 Long Double Sharp Click Medium 3 – 60%
            LongDoubleSharpTick_1_100,          // 44 Long Double Sharp Tick 1 – 100%
            LongDoubleSharpTick_2_80,           // 45 Long Double Sharp Tick 2 – 80%
            LongDoubleSharpTick_3_60,           // 46 Long Double Sharp Tick 3 – 60%
            Buzz_1_100,             // 47 Buzz 1 – 100%
            Buzz_2_80,              // 48 Buzz 2 – 80%
            Buzz_3_60,              // 49 Buzz 3 – 60%
            Buzz_4_40,              // 50 Buzz 4 – 40%
            Buzz_5_20,              // 51 Buzz 5 – 20%
            PulsingStrong_1_100,    // 52 Pulsing Strong 1 – 100%
            PulsingStrong_2_60,     // 53 Pulsing Strong 2 – 60%
            PulsingMedium_1_100,    // 54 Pulsing Medium 1 – 100%
            PulsingMedium_2_60,     // 55 Pulsing Medium 2 – 60%
            PulsingSharp_1_100,     // 56 Pulsing Sharp 1 – 100%
            PulsingSharp_2_60,      // 57 Pulsing Sharp 2 – 60%
            TransitionClick_1_100,  // 58 Transition Click 1 – 100%
            TransitionClick_2_80,   // 59 Transition Click 2 – 80%
            TransitionClick_3_60,   // 60 Transition Click 3 – 60%
            TransitionClick_4_40,   // 61 Transition Click 4 – 40%
            TransitionClick_5_20,   // 62 Transition Click 5 – 20%
            TransitionClick_6_10,   // 63 Transition Click 6 – 10%
            TransitionHum_1_100,    // 64 Transition Hum 1 – 100%
            TransitionHum_2_80,     // 65 Transition Hum 2 – 80%
            TransitionHum_3_60,     // 66 Transition Hum 3 – 60%
            TransitionHum_4_40,     // 67 Transition Hum 4 – 40%
            TransitionHum_5_20,     // 68 Transition Hum 5 – 20%
            TransitionHum_6_10,     // 69 Transition Hum 6 – 10%
            TransitionRampDownLongSmooth_1_100_to_0,    // 70 Ramp Down Long Smooth 1 – 100 to 0%
            TransitionRampDownLongSmooth_2_100_to_0,    // 71 Ramp Down Long Smooth 2 – 100 to 0%
            TransitionRampDownMediumSmooth_1_100_to_0,  // 72 Ramp Down Medium Smooth 1 – 100 to 0%
            TransitionRampDownMediumSmooth_2_100_to_0,  // 73 Ramp Down Medium Smooth 2 – 100 to 0%
            TransitionRampDownShortSmooth_1_100_to_0,   // 74 Ramp Down Short Smooth 1 – 100 to 0%
            TransitionRampDownShortSmooth_2_100_to_0,   // 75 Ramp Down Short Smooth 2 – 100 to 0%
            TransitionRampDownLongSharp_1_100_to_0,     // 76 Ramp Down Long Sharp 1 – 100 to 0%
            TransitionRampDownLongSharp_2_100_to_0,     // 77 Ramp Down Long Sharp 2 – 100 to 0%
            TransitionRampDownMediumSharp_1_100_to_0,   // 78 Ramp Down Medium Sharp 1 – 100 to 0%
            TransitionRampDownMediumSharp_2_100_to_0,   // 79 Ramp Down Medium Sharp 2 – 100 to 0%
            TransitionRampDownShortSharp_1_100_to_0,    // 80 Ramp Down Short Sharp 1 – 100 to 0%
            TransitionRampDownShortSharp_2_100_to_0,    // 81 Ramp Down Short Sharp 2 – 100 to 0%
            TransitionRampUpLongSmooth_1_0_to_100,      // 82 Ramp Up Long Smooth 1 – 0 to 100%
            TransitionRampUpLongSmooth_2_0_to_100,      // 83 Ramp Up Long Smooth 2 – 0 to 100%
            TransitionRampUpMediumSmooth_1_0_to_100,    // 84 Ramp Up Medium Smooth 1 – 0 to 100%
            TransitionRampUpMediumSmooth_2_0_to_100,    // 85 Ramp Up Medium Smooth 2 – 0 to 100%
            TransitionRampUpShortSmooth_1_0_to_100,     // 86 Ramp Up Short Smooth 1 – 0 to 100%
            TransitionRampUpShortSmooth_2_0_to_100,     // 87 Ramp Up Short Smooth 2 – 0 to 100%
            TransitionRampUpLongSharp_1_0_to_100,       // 88 Ramp Up Long Sharp 1 – 0 to 100%
            TransitionRampUpLongSharp_2_0_to_100,       // 89 Ramp Up Long Sharp 2 – 0 to 100%
            TransitionRampUpMediumSharp_1_0_to_100,     // 90 Ramp Up Medium Sharp 1 – 0 to 100%
            TransitionRampUpMediumSharp_2_0_to_100,     // 91 Ramp Up Medium Sharp 2 – 0 to 100%
            TransitionRampUpShortSharp_1_0_to_100,      // 92 Ramp Up Short Sharp 1 – 0 to 100%
            TransitionRampUpShortSharp_2_0_to_100,      // 93 Ramp Up Short Sharp 2 – 0 to 100%
            TransitionRampDownLongSmooth_1_50_to_0,     // 94 Ramp Down Long Smooth 1 – 50 to 0%
            TransitionRampDownLongSmooth_2_50_to_0,     // 95 Ramp Down Long Smooth 2 – 50 to 0%
            TransitionRampDownMediumSmooth_1_50_to_0,   // 96 Ramp Down Medium Smooth 1 – 50 to 0%
            TransitionRampDownMediumSmooth_2_50_to_0,   // 97 Ramp Down Medium Smooth 2 – 50 to 0%
            TransitionRampDownShortSmooth_1_50_to_0,    // 98 Ramp Down Short Smooth 1 – 50 to 0%
            TransitionRampDownShortSmooth_2_50_to_0,    // 99 Ramp Down Short Smooth 2 – 50 to 0%
            TransitionRampDownLongSharp_1_50_to_0,      // 100 Ramp Down Long Sharp 1 – 50 to 0%
            TransitionRampDownLongSharp_2_50_to_0,      // 101 Ramp Down Long Sharp 2 – 50 to 0%
            TransitionRampDownMediumSharp_1_50_to_0,    // 102 Ramp Down Medium Sharp 1 – 50 to 0%
            TransitionRampDownMediumSharp_2_50_to_0,    // 103 Ramp Down Medium Sharp 2 – 50 to 0%
            TransitionRampDownShortSharp_1_50_to_0,     // 104 Ramp Down Short Sharp 1 – 50 to 0%
            TransitionRampDownShortSharp_2_50_to_0,     // 105 Ramp Down Short Sharp 2 – 50 to 0%
            TransitionRampUpLongSmooth_1_0_to_50,       // 106 Ramp Up Long Smooth 1 – 0 to 50%
            TransitionRampUpLongSmooth_2_0_to_50,       // 107 Ramp Up Long Smooth 2 – 0 to 50%
            TransitionRampUpMediumSmooth_1_0_to_50,     // 108 Ramp Up Medium Smooth 1 – 0 to 50%
            TransitionRampUpMediumSmooth_2_0_to_50,     // 109 Ramp Up Medium Smooth 2 – 0 to 50%
            TransitionRampUpShortSmooth_1_0_to_50,      // 110 Ramp Up Short Smooth 1 – 0 to 50%
            TransitionRampUpShortSmooth_2_0_to_50,      // 111 Ramp Up Short Smooth 2 – 0 to 50%
            TransitionRampUpLongSharp_1_0_to_50,        // 112 Ramp Up Long Sharp 1 – 0 to 50%
            TransitionRampUpLongSharp_2_0_to_50,        // 113 Ramp Up Long Sharp 2 – 0 to 50%
            TransitionRampUpMediumSharp_1_0_to_50,      // 114 Ramp Up Medium Sharp 1 – 0 to 50%
            TransitionRampUpMediumSharp_2_0_to_50,      // 115 Ramp Up Medium Sharp 2 – 0 to 50%
            TransitionRampUpShortSharp_1_0_to_50,       // 116 Ramp Up Short Sharp 1 – 0 to 50%
            TransitionRampUpShortSharp_2_0_to_50,       // 117 Ramp Up Short Sharp 2 – 0 to 50%
            LongBuzz_100,           // 118 Long buzz for programmatic stopping – 100%
            SmoothHum_1_50,         // 119 Smooth Hum 1 (No kick or brake pulse) – 50%
            SmoothHum_2_40,         // 120 Smooth Hum 2 (No kick or brake pulse) – 40%
            SmoothHum_3_30,         // 121 Smooth Hum 3 (No kick or brake pulse) – 30%
            SmoothHum_4_20,         // 122 Smooth Hum 4 (No kick or brake pulse) – 20%
            SmoothHum_5_10,         // 123 Smooth Hum 5 (No kick or brake pulse) – 10%
            // When the high bit is set, the low 7 bits are a delay, in 10ms units.
            // Use like "Delay | 100" for a 1000ms delay
            //Delay = 0x80,
        };

        // Delay lengths can only be integer intervals of 10ms
        // e.g. delay = 2 generates a 20ms delay
        // 'lengthBy10ms' must be in the range [1, 127]
        public static Type Delay(byte lengthDividedBy10ms)
        {
            return (Type)(0x80 | lengthDividedBy10ms);
        }
    }

}
