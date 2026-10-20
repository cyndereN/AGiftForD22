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
        static Vector3 offset;
        static float restFov = 60f;
        static bool posed, wrote, tipsy;

        public static void Clear()
        {
            drink = Active = Kind.None;
            Turn = 1f;
            tipsy = false;
            offset = Vector3.zero;
            wrote = posed = false;
        }

        public static void SetTipsy(bool on) => tipsy = on;

        public static void SetDrink(int index)
        {
            drink = index == 0 ? Kind.Louder : index == 2 ? Kind.Night : Kind.Room;
            Active = drink;
            Turn = 1f;
        }

        public static void SetLane(int index)
        {
            Active = index == 0 ? Kind.Made : index == 2 ? Kind.Neither : Kind.Natural;
            Turn = 1f;
        }

        public static Look Current => Active switch
        {
            Kind.Louder => new Look
            {
                fovLo = 50f, fovHi = 78f, breathe = .95f, shake = 18f, rate = .85f, turn = 1f,
                exposure = .05f, contrast = 32f, saturation = 8f, filter = new Color(1f, .62f, .5f),
                bloom = .7f, bloomAt = .75f, vignette = .46f, vignetteSoft = .22f, chroma = .42f, mix = -1
            },
            Kind.Room => new Look
            {
                fov = 60f, shake = 3.4f, rate = .48f, turn = 1f,
                exposure = 0f, contrast = -18f, saturation = -28f, filter = Color.white,
                bloom = 0f, bloomAt = 1f, vignette = .26f, vignetteSoft = .78f, chroma = 0f, mix = -1
            },
            Kind.Night => new Look
            {
                fov = 52f, shake = 7.5f, rate = .44f, turn = 1f,
                exposure = -.62f, contrast = 6f, saturation = -38f, filter = new Color(.74f, .84f, 1f),
                bloom = 0f, bloomAt = 1f, vignette = .55f, vignetteSoft = .3f, chroma = 0f, mix = -1
            },
            Kind.Made => new Look
            {
                fov = 56f, shake = 6.6f, rate = .55f, turn = 1f,
                exposure = .04f, contrast = 20f, saturation = 6f, filter = new Color(1f, .88f, .74f),
                bloom = .2f, bloomAt = .95f, vignette = .16f, vignetteSoft = .5f, chroma = 0f, mix = 0
            },
            Kind.Natural => new Look
            {
                fov = 68f, shake = 2.6f, rate = .4f, turn = 1f,
                exposure = .02f, contrast = -14f, saturation = -22f, filter = new Color(.82f, .98f, .84f),
                bloom = 0f, bloomAt = 1f, vignette = .12f, vignetteSoft = .82f, chroma = 0f, mix = 1
            },
            Kind.Neither => new Look
            {
                fov = 60f, shake = 5.4f, rate = .52f, turn = 1f,
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

        public static void Release(Transform view)
        {
            if (!view || !wrote || posedOn != view) return;
            view.rotation = clean;
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
            Turn = 1f;
            float wobble = tipsy ? Mathf.Sin(Time.time * .55f) * (reduceShake ? 1.4f : 3.2f) : 0f;
            if (Active != Kind.None) camera.fieldOfView = Fov(look) + wobble;
            else if (tipsy)
            {
                if (restFov < 1f) restFov = 60f;
                camera.fieldOfView = restFov + wobble;
            }
            else restFov = camera.fieldOfView;
            Vector3 target = Wave(look, reduceShake);
            if (tipsy) target += Wave(new Look { shake = 5.2f, rate = .5f }, reduceShake, 1.7f);
            float follow = 1f - Mathf.Exp(-8f * Time.deltaTime);
            offset = Vector3.Lerp(offset, target, follow);
            if (offset.sqrMagnitude < .0004f && target.sqrMagnitude < .0004f)
            {
                offset = Vector3.zero;
                if (wrote && posedOn == view) view.rotation = clean;
                wrote = false;
                return;
            }
            wrote = true;
            view.rotation = clean * Quaternion.Euler(offset);
        }

        static float Fov(Look look)
        {
            if (look.breathe <= 0f) return look.fov > 1f ? look.fov : 60f;
            float u = (Mathf.Sin(Time.time * look.breathe) + 1f) * .5f;
            return Mathf.Lerp(look.fovLo, look.fovHi, u);
        }

        static Vector3 Wave(Look look, bool reduce, float phase = 0f)
        {
            float amp = look.shake;
            if (reduce) amp *= .55f;
            if (amp <= .001f) return Vector3.zero;
            float rate = look.rate > .15f ? look.rate : .4f;
            float t = Time.time * rate + phase;
            float pitch = Mathf.Sin(t * .7f) * .65f + Mathf.Sin(t * 1.15f + 1.2f) * .35f;
            float yaw = Mathf.Sin(t * .5f + .4f) * .55f + Mathf.Sin(t * .95f + 2.1f) * .25f;
            float roll = Mathf.Sin(t * .42f + .8f) * .8f + Mathf.Sin(t * .8f + 2.6f) * .35f;
            return new Vector3(pitch, yaw, roll) * amp;
        }
    }
}
