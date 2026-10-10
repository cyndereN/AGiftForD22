using UnityEngine;

namespace D22
{
    public static class D22Feel
    {
        public enum Kind { None, Louder, Room, Night, Made, Natural, Neither }

        public struct Look
        {
            public float fov, fovLo, fovHi, breathe;
            public float shake, rate;
            public bool burst;
            public float turn;
            public float exposure, contrast, saturation;
            public Color filter;
            public float bloom, bloomAt, vignette, vignetteSoft, chroma;
            public int mix;
        }

        public static Kind Active { get; private set; } = Kind.None;
        public static float Turn { get; private set; } = 1f;
        public static bool Dusk => drink == Kind.Night;

        static Kind drink = Kind.None;
        static Transform posedOn;
        static Quaternion clean;
        static bool posed, wrote;

        public static void Clear()
        {
            drink = Active = Kind.None;
            Turn = 1f;
            wrote = posed = false;
        }

        public static void SetDrink(int index)
        {
            drink = index == 0 ? Kind.Louder : index == 2 ? Kind.Night : Kind.Room;
            Active = drink;
            Turn = Current.turn;
        }

        public static void SetLane(int index)
        {
            Active = index == 0 ? Kind.Made : index == 2 ? Kind.Neither : Kind.Natural;
            Turn = Current.turn;
        }

        public static Look Current => Active switch
        {
            Kind.Louder => new Look
            {
                fovLo = 58f, fovHi = 66f, breathe = .7f, shake = 1.15f, rate = 1.6f, turn = 1.65f,
                exposure = .05f, contrast = 32f, saturation = 8f, filter = new Color(1f, .62f, .5f),
                bloom = .7f, bloomAt = .75f, vignette = .46f, vignetteSoft = .22f, chroma = .42f, mix = -1
            },
            Kind.Room => new Look
            {
                fov = 60f, shake = .2f, rate = .75f, turn = .48f,
                exposure = 0f, contrast = -18f, saturation = -28f, filter = Color.white,
                bloom = 0f, bloomAt = 1f, vignette = .26f, vignetteSoft = .78f, chroma = 0f, mix = -1
            },
            Kind.Night => new Look
            {
                fov = 52f, shake = 0f, burst = true, turn = 1f,
                exposure = -.62f, contrast = 6f, saturation = -38f, filter = new Color(.74f, .84f, 1f),
                bloom = 0f, bloomAt = 1f, vignette = .55f, vignetteSoft = .3f, chroma = 0f, mix = -1
            },
            Kind.Made => new Look
            {
                fov = 56f, shake = .7f, rate = 1.15f, turn = .78f,
                exposure = .04f, contrast = 20f, saturation = 6f, filter = new Color(1f, .88f, .74f),
                bloom = .2f, bloomAt = .95f, vignette = .16f, vignetteSoft = .5f, chroma = 0f, mix = 0
            },
            Kind.Natural => new Look
            {
                fov = 68f, shake = .05f, rate = .45f, turn = .42f,
                exposure = .02f, contrast = -14f, saturation = -22f, filter = new Color(.82f, .98f, .84f),
                bloom = 0f, bloomAt = 1f, vignette = .12f, vignetteSoft = .82f, chroma = 0f, mix = 1
            },
            Kind.Neither => new Look
            {
                fov = 60f, shake = .26f, rate = 8.2f, turn = 1f,
                exposure = -.04f, contrast = 40f, saturation = -42f, filter = new Color(.9f, .9f, .92f),
                bloom = 0f, bloomAt = 1f, vignette = .48f, vignetteSoft = .18f, chroma = .08f, mix = 2
            },
            _ => new Look { fov = 60f, turn = 1f, filter = Color.white, bloomAt = 1f, mix = -1 }
        };

        public static Quaternion Pose(Transform view)
        {
            if (view && wrote && posedOn == view) return clean;
            return view ? view.rotation : Quaternion.identity;
        }

        public static void NotePose(Transform view)
        {
            if (!view) return;
            posedOn = view;
            clean = view.rotation;
            posed = true;
        }

        public static void Apply(Camera camera, bool reduceShake)
        {
            if (!camera) return;
            var view = camera.transform;
            if (!(posed && posedOn == view))
            {
                if (wrote && posedOn == view) view.rotation = clean;
                else
                {
                    clean = view.rotation;
                    posedOn = view;
                }
            }
            posed = false;
            var look = Current;
            Turn = look.turn;
            camera.fieldOfView = Fov(look);
            if (Active == Kind.None)
            {
                if (wrote && posedOn == view) view.rotation = clean;
                wrote = false;
                return;
            }
            wrote = true;
            view.rotation = clean * Quaternion.Euler(Shake(look, reduceShake));
        }

        static float Fov(Look look)
        {
            if (look.breathe <= 0f) return look.fov > 1f ? look.fov : 60f;
            float u = (Mathf.Sin(Time.time * look.breathe) + 1f) * .5f;
            return Mathf.Lerp(look.fovLo, look.fovHi, u);
        }

        static Vector3 Shake(Look look, bool reduce)
        {
            float amp = look.shake;
            if (look.burst)
            {
                float phase = Time.time % 8.2f;
                amp = phase > 6.6f && phase < 7.02f ? 3.3f : 0f;
            }
            if (reduce) amp *= .35f;
            if (amp <= .001f) return Vector3.zero;
            float t = Time.time * look.rate;
            float Pitch(float seed) => (Mathf.PerlinNoise(seed, t) - .5f) * 2f * amp;
            return new Vector3(Pitch(2.1f), Pitch(5.7f), Pitch(9.3f) * .65f);
        }
    }
}
