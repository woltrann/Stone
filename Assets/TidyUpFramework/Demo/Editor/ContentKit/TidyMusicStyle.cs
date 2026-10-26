namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal enum TidyLeadInstrument
    {
        Celesta,
        ElectricPiano,
        MusicBox
    }

    internal readonly struct TidyChord
    {
        public TidyChord(int bass, params int[] tones)
        {
            Bass = bass;
            Tones = tones;
        }

        public int Bass { get; }

        public int[] Tones { get; }
    }

    internal sealed class TidyMusicStyle
    {
        public float Tempo { get; set; } = 72f;

        public int RootMidi { get; set; } = 60;

        public TidyChord[] Progression { get; set; } = new TidyChord[0];

        public int[] MelodyScale { get; set; } = { 0, 2, 4, 7, 9 };

        public float MelodyDensity { get; set; } = 0.4f;

        public float Swing { get; set; }

        public bool UseDrums { get; set; }

        public bool UseKeysComping { get; set; }

        public TidyLeadInstrument Lead { get; set; } = TidyLeadInstrument.Celesta;

        public float PadBrightness { get; set; } = 0.45f;

        public float MasterLowPass { get; set; } = 8000f;

        public float ReverbWet { get; set; } = 0.3f;
    }

    internal static class TidyMusicStyles
    {
        public const int CosyLibrary = 0;
        public const int BrightStore = 1;
        public const int MellowWarehouse = 2;
        public const int SoftBedroom = 3;

        private static readonly int[] MinorPentatonic = { 0, 3, 5, 7, 10 };
        private static readonly int[] MajorPentatonic = { 0, 2, 4, 7, 9 };

        public static TidyMusicStyle ForMood(int mood)
        {
            switch (mood)
            {
                case BrightStore:
                    return BrightStoreStyle();
                case MellowWarehouse:
                    return MellowWarehouseStyle();
                case SoftBedroom:
                    return SoftBedroomStyle();
                default:
                    return CosyLibraryStyle();
            }
        }

        public static TidyMusicStyle Menu()
        {
            return new TidyMusicStyle
            {
                Tempo = 84f,
                RootMidi = 55,
                Progression = new[]
                {
                    new TidyChord(0, 0, 4, 7, 11),
                    new TidyChord(-5, -5, -1, 2, 7),
                    new TidyChord(-3, -3, 0, 4, 7),
                    new TidyChord(-7, 5, 9, 12, 16)
                },
                MelodyScale = MajorPentatonic,
                MelodyDensity = 0.45f,
                Lead = TidyLeadInstrument.Celesta,
                PadBrightness = 0.5f,
                MasterLowPass = 9000f,
                ReverbWet = 0.32f
            };
        }

        private static TidyMusicStyle CosyLibraryStyle()
        {
            return new TidyMusicStyle
            {
                Tempo = 70f,
                RootMidi = 57,
                Progression = new[]
                {
                    new TidyChord(0, 0, 3, 7, 10),
                    new TidyChord(-4, -4, 0, 3, 7),
                    new TidyChord(3, 3, 7, 10, 14),
                    new TidyChord(-2, -2, 2, 5, 9)
                },
                MelodyScale = MinorPentatonic,
                MelodyDensity = 0.35f,
                Lead = TidyLeadInstrument.Celesta,
                PadBrightness = 0.38f,
                MasterLowPass = 6500f,
                ReverbWet = 0.38f
            };
        }

        private static TidyMusicStyle BrightStoreStyle()
        {
            return new TidyMusicStyle
            {
                Tempo = 100f,
                RootMidi = 62,
                Progression = new[]
                {
                    new TidyChord(0, 0, 4, 7),
                    new TidyChord(-3, -3, 0, 4),
                    new TidyChord(-7, 5, 9, 12),
                    new TidyChord(-5, -5, -1, 2)
                },
                MelodyScale = MajorPentatonic,
                MelodyDensity = 0.55f,
                UseDrums = true,
                Lead = TidyLeadInstrument.ElectricPiano,
                PadBrightness = 0.5f,
                MasterLowPass = 9500f,
                ReverbWet = 0.22f
            };
        }

        private static TidyMusicStyle MellowWarehouseStyle()
        {
            return new TidyMusicStyle
            {
                Tempo = 76f,
                RootMidi = 53,
                Progression = new[]
                {
                    new TidyChord(0, 0, 4, 7, 11),
                    new TidyChord(-3, -3, 0, 4, 7),
                    new TidyChord(2, 2, 5, 9, 12),
                    new TidyChord(-5, -5, -1, 2, 5)
                },
                MelodyScale = MajorPentatonic,
                MelodyDensity = 0.3f,
                Swing = 0.12f,
                UseDrums = true,
                UseKeysComping = true,
                Lead = TidyLeadInstrument.ElectricPiano,
                PadBrightness = 0.3f,
                MasterLowPass = 5000f,
                ReverbWet = 0.25f
            };
        }

        private static TidyMusicStyle SoftBedroomStyle()
        {
            return new TidyMusicStyle
            {
                Tempo = 64f,
                RootMidi = 63,
                Progression = new[]
                {
                    new TidyChord(0, 0, 4, 7),
                    new TidyChord(4, 4, 7, 11),
                    new TidyChord(5, 5, 9, 12),
                    new TidyChord(-5, -5, -1, 2)
                },
                MelodyScale = MajorPentatonic,
                MelodyDensity = 0.4f,
                Lead = TidyLeadInstrument.MusicBox,
                PadBrightness = 0.35f,
                MasterLowPass = 7000f,
                ReverbWet = 0.4f
            };
        }
    }
}
