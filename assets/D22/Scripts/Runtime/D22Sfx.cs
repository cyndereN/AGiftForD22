using UnityEngine;

namespace D22
{
    public static class D22Sfx
    {
        public static AudioClip Touch()
        {
            const int rate = 44100;
            const float seconds = .32f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(7);
            float hp = 0, prev = 0, bp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float white = (float)rng.NextDouble() * 2 - 1;
                hp = white - prev + .92f * hp;
                prev = white;
                bp = bp * .82f + hp * .18f;
                float metal = Mathf.Sin(t * 2400 * Mathf.PI * 2) * Mathf.Exp(-t * 14) * .42f;
                float metal2 = Mathf.Sin(t * 3900 * Mathf.PI * 2) * Mathf.Exp(-t * 20) * .2f;
                float env = Mathf.Exp(-t * 7.5f) * (1f - Mathf.Exp(-t * 200));
                data[i] = Mathf.Clamp((hp * .5f + bp * .65f + metal + metal2) * env, -1f, 1f);
            }
            return Clip("stone-touch", data, rate);
        }

        public static AudioClip[] Whistles()
        {
            return new[]
            {
                Whistle(820, 1230, .70f, .012f, 5.5f, .07f, -40, 11),
                Whistle(1060, 1590, .55f, .022f, 7.4f, .05f, 50, 12),
                Whistle(690, 1380, .84f, .008f, 4.1f, .11f, -15, 13),
                Whistle(1520, 2280, .46f, .03f, 9.2f, .04f, -90, 14),
                Whistle(940, 1010, .62f, .016f, 6.1f, .06f, 25, 15)
            };
        }

        static AudioClip Whistle(float f1, float f2, float seconds, float vib, float vibHz, float noise, float glide, int seed)
        {
            const int rate = 44100;
            int n = (int)(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(seed);
            float p1 = 0, p2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Min(1f, t / .03f) * Mathf.Min(1f, (seconds - t) / .16f);
                float wob = 1f + vib * Mathf.Sin(t * vibHz * Mathf.PI * 2);
                float f = (f1 + glide * t) * wob;
                p1 += Mathf.PI * 2 * f / rate;
                p2 += Mathf.PI * 2 * (f2 * wob) / rate;
                float air = ((float)rng.NextDouble() * 2 - 1) * noise * Mathf.Exp(-t * 1.5f);
                float tone = Mathf.Sin(p1) + .4f * Mathf.Sin(p2) + .15f * Mathf.Sin(p1 * 2);
                data[i] = Mathf.Clamp((tone * .33f + air) * env, -1f, 1f);
            }
            return Clip("whistle-" + seed, data, rate);
        }

        static AudioClip Clip(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
