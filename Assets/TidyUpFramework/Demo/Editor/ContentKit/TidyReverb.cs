namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal static class TidyReverb
    {
        private const float InputGain = 0.02f;
        private const float WetScale = 3f;
        private const float AllPassFeedback = 0.5f;

        private static readonly int[] CombDelays = { 1116, 1188, 1277, 1356, 1422, 1491 };
        private static readonly int[] AllPassDelays = { 556, 441, 341 };

        public static void Apply(float[] buffer, float wet, float roomSize, float damping)
        {
            var combs = new Comb[CombDelays.Length];
            for (int i = 0; i < combs.Length; i++)
            {
                combs[i] = new Comb(CombDelays[i], roomSize, damping);
            }

            var allPasses = new AllPass[AllPassDelays.Length];
            for (int i = 0; i < allPasses.Length; i++)
            {
                allPasses[i] = new AllPass(AllPassDelays[i]);
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                float input = buffer[i] * InputGain;
                float sum = 0f;
                foreach (Comb comb in combs)
                {
                    sum += comb.Process(input);
                }

                foreach (AllPass allPass in allPasses)
                {
                    sum = allPass.Process(sum);
                }

                buffer[i] += sum * wet * WetScale;
            }
        }

        private sealed class Comb
        {
            private readonly float[] line;
            private readonly float feedback;
            private readonly float damping;
            private int index;
            private float store;

            public Comb(int delay, float feedback, float damping)
            {
                line = new float[delay];
                this.feedback = feedback;
                this.damping = damping;
            }

            public float Process(float input)
            {
                float output = line[index];
                store = output * (1f - damping) + store * damping;
                line[index] = input + store * feedback;
                index = index + 1 >= line.Length ? 0 : index + 1;
                return output;
            }
        }

        private sealed class AllPass
        {
            private readonly float[] line;
            private int index;

            public AllPass(int delay)
            {
                line = new float[delay];
            }

            public float Process(float input)
            {
                float delayed = line[index];
                line[index] = input + delayed * AllPassFeedback;
                index = index + 1 >= line.Length ? 0 : index + 1;
                return delayed - input;
            }
        }
    }
}
