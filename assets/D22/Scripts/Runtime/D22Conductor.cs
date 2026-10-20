using UnityEngine;

namespace D22
{
    // A needle sweeping a circle in time with the finale. Each lap has 2, 3, 4, or 5 dots.
    public sealed class D22Conductor
    {
        public const int Turns = 8;
        public const float Bpm = 48f;
        const float Window = .2f;
        static readonly float TurnSeconds = 4f * (60f / Bpm);

        public bool Running { get; private set; }
        public float Angle { get; private set; }
        public float Near { get; private set; }
        public float Flash { get; private set; }
        public int Cue { get; private set; } = 4;
        public int Approach { get; private set; }
        public int Dots { get; private set; } = 4;

        readonly int[] laps = new int[Turns];
        int turn, rewarded, shown;
        float turnT;

        public void Begin()
        {
            Running = true;
            turn = 0;
            turnT = 0;
            rewarded = -1;
            shown = 0;
            Flash = 0;
            Cue = RollCue();
            Angle = 0;
            Near = 0;
            Approach = 0;
            DealLaps();
            Dots = laps[0];
        }

        public void End() => Running = false;

        public bool Tick(float dt, int pressedSlot, out int played)
        {
            played = -1;
            if (!Running) return false;
            turnT += dt / TurnSeconds;
            if (turnT >= 1f)
            {
                turn++;
                turnT -= 1f;
                if (turn >= Turns)
                {
                    Running = false;
                    return true;
                }
                Dots = laps[turn];
                shown = 0;
                rewarded = -1;
                Cue = RollCue();
            }
            Angle = turnT * 360f;
            float local = turnT * Dots;
            float frac = local - Mathf.Floor(local);
            int next = Mathf.Clamp(Mathf.FloorToInt(local) + 1, 1, Dots);
            float toNext = next - local;
            float since = local - (next - 1);
            int hot = toNext <= Window ? next : since <= Window && next > 1 ? next - 1 : -1;
            Near = Mathf.Clamp01(1f - toNext / Window);
            Approach = Mathf.Clamp(Mathf.FloorToInt(local), 0, Dots - 1);
            int hitId = turn * 8 + hot;
            if (pressedSlot >= 1 && hot >= 1 && hitId != rewarded)
            {
                rewarded = hitId;
                Flash = 1f;
                played = pressedSlot;
            }
            Flash = Mathf.MoveTowards(Flash, 0, dt * 2.4f);
            int passed = Mathf.FloorToInt(local);
            if (passed >= 1 && passed < Dots && passed != shown)
            {
                shown = passed;
                Cue = RollCue();
            }
            return false;
        }

        void DealLaps()
        {
            int[] bag = { 2, 3, 4, 5, 2, 3, 4, 5 };
            for (int i = 0; i < Turns; i++)
            {
                int j = Random.Range(i, Turns);
                int swap = bag[i];
                bag[i] = bag[j];
                bag[j] = swap;
                laps[i] = bag[i];
            }
        }

        static int RollCue() => Random.Range(1, 7);
    }
}
