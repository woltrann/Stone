namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal readonly struct TidyFmPatch
    {
        public static readonly TidyFmPatch ElectricPiano = new TidyFmPatch(1f, 2.2f, 0.22f, 0.3f, 0.003f, 7f, 0.12f, 0.035f);
        public static readonly TidyFmPatch Celesta = new TidyFmPatch(4f, 1.2f, 0.06f, 0.28f, 0.002f, 2f, 0.18f, 0.25f);
        public static readonly TidyFmPatch Marimba = new TidyFmPatch(3.5f, 1.4f, 0.03f, 0.12f, 0.001f, 4f, 0.1f, 0.02f);
        public static readonly TidyFmPatch SoftPluck = new TidyFmPatch(2f, 1.6f, 0.08f, 0.2f, 0.003f, 3f, 0.05f, 0.05f);

        public TidyFmPatch(float modulatorRatio, float modulationIndex, float indexDecay, float decayFraction, float attack, float overtoneRatio, float overtoneLevel, float overtoneDecay)
        {
            ModulatorRatio = modulatorRatio;
            ModulationIndex = modulationIndex;
            IndexDecay = indexDecay;
            DecayFraction = decayFraction;
            Attack = attack;
            OvertoneRatio = overtoneRatio;
            OvertoneLevel = overtoneLevel;
            OvertoneDecay = overtoneDecay;
        }

        public float ModulatorRatio { get; }

        public float ModulationIndex { get; }

        public float IndexDecay { get; }

        public float DecayFraction { get; }

        public float Attack { get; }

        public float OvertoneRatio { get; }

        public float OvertoneLevel { get; }

        public float OvertoneDecay { get; }
    }
}
