using System;
using System.Collections.Generic;
using UnityEngine;

namespace D22
{
    public sealed class D22Note
    {
        public Vector2 pos;
        public float life;
    }

    public sealed class D22Bird
    {
        public Vector2 pos, vel;
    }

    public sealed class D22Hunt
    {
        public const float CricketCd = 3, PigeonCd = 7, GrindCd = 2, DrinkCd = 5;
        readonly D22GameFlow flow;
        readonly AudioSource near, flock, grindLoop, oneshot;
        readonly List<D22WorldMark> marks = new();
        readonly List<D22Bird> birds = new();
        readonly List<D22Note> notes = new();
        D22WorldMark cricketMark, grindMark, pigeonMark;
        Vector3 pigeonHome;
        AudioClip cricketSfx, pigeonSfx, grindSfx;
        float grindStroke, hopT, hopDur, hopArc, coverDelay, dropLeft;
        int grindHits;
        Vector2 hopFrom, hopTo, lastKnife, trap;
        bool knifeHeld, heardCricket, heardPigeon, heardGrind;

        public string Game { get; private set; }
        public bool Open => Game != null;
        public int Cricket { get; private set; }
        public int Grind { get; private set; }
        public int Pigeon { get; private set; }
        public Vector2 Bug { get; private set; }
        public Vector2 Knife { get; private set; }
        public Vector2 Trap => trap;
        public bool Dropping { get; private set; }
        public float DropLeft => dropLeft;
        public const float TrapSize = .26f;
        public const float DropTime = .3f;
        public IReadOnlyList<D22Bird> Birds => birds;
        public IReadOnlyList<D22Note> Notes => notes;
        public IReadOnlyList<D22WorldMark> Marks => marks;
        public bool Spawned { get; private set; }
        public bool Unlocked => flow.Abilities != null
            && flow.Abilities.Learned("cricket")
            && flow.Abilities.Learned("pigeon")
            && flow.Abilities.Learned("scissors");
        public const int NeedPigeon = 10, NeedGrind = 7, NeedCricket = 4, NeedDrink = 3;
        public bool QuotaDone(int sips) => Pigeon >= NeedPigeon && Grind >= NeedGrind && Cricket >= NeedCricket && sips >= NeedDrink;
        public string Quota(int sips) => $"需要 {Pigeon}/{NeedPigeon}个鸽哨 {Grind}/{NeedGrind}磨刀 {Cricket}/{NeedCricket}个蛐蛐 {sips}/{NeedDrink}酒";
        public float CdCricket { get; private set; }
        public float CdPigeon { get; private set; }
        public float CdGrind { get; private set; }

        public D22Hunt(D22GameFlow host)
        {
            flow = host;
            near = host.gameObject.AddComponent<AudioSource>();
            near.playOnAwake = false;
            near.loop = true;
            near.spatialBlend = 0;
            flock = host.gameObject.AddComponent<AudioSource>();
            flock.playOnAwake = false;
            flock.loop = true;
            flock.spatialBlend = 0;
            grindLoop = host.gameObject.AddComponent<AudioSource>();
            grindLoop.playOnAwake = false;
            grindLoop.loop = true;
            grindLoop.spatialBlend = 0;
            oneshot = host.gameObject.AddComponent<AudioSource>();
            oneshot.playOnAwake = false;
            oneshot.spatialBlend = 0;
        }

        public void SetClips(AudioClip cricket, AudioClip pigeon = null, AudioClip grind = null)
        {
            cricketSfx = cricket ? cricket : MakeChirp();
            pigeonSfx = pigeon;
            grindSfx = grind;
            if (near.clip != cricketSfx) near.clip = cricketSfx;
            if (Spawned)
            {
                StartFlock(false);
                StartGrind(false);
            }
        }

        public void Reset()
        {
            Close();
            ClearMarks();
            Cricket = Grind = Pigeon = 0;
            heardCricket = heardPigeon = heardGrind = false;
            Spawned = false;
            CdCricket = CdPigeon = CdGrind = 0;
        }

        public void ClearMarks()
        {
            near.Stop();
            flock.Stop();
            grindLoop.Stop();
            foreach (var m in marks) if (m) UnityEngine.Object.Destroy(m.gameObject);
            marks.Clear();
            cricketMark = grindMark = pigeonMark = null;
            Spawned = false;
        }

        public void AfterDoor()
        {
            if (Spawned) return;
            Spawned = true;
            var hutong = UnityEngine.Object.FindAnyObjectByType<D22HutongLighting>();
            cricketMark = Make(D22MarkKind.Cricket, hutong ? hutong.cricketMark : new Vector3(-1.7f, -.55f, -1.15f), 1.35f);
            grindMark = Make(D22MarkKind.Grind, hutong ? hutong.grindMark : new Vector3(1.85f, .45f, -4.3f), 1.45f);
            pigeonHome = hutong ? hutong.pigeonMark : new Vector3(0, 2.7f, -5.8f);
            pigeonMark = Make(D22MarkKind.Pigeons, pigeonHome, 3.2f);
            var exit = UnityEngine.Object.FindAnyObjectByType<D22Exit>();
            if (exit && !hutong)
            {
                exit.radius = .85f;
                exit.transform.position = new Vector3(0, .15f, -8.2f);
            }
            StartFlock(true);
            StartGrind(true);
        }

        D22WorldMark Make(D22MarkKind kind, Vector3 pos, float radius)
        {
            var go = new GameObject("Mark " + kind);
            go.transform.position = pos;
            var mark = go.AddComponent<D22WorldMark>();
            mark.kind = kind;
            mark.radius = radius;
            marks.Add(mark);
            return mark;
        }

        public void Close()
        {
            if (Game == "pigeon") CdPigeon = Time.unscaledTime + PigeonCd;
            else if (Game == "cricket") CdCricket = Time.unscaledTime + CricketCd;
            else if (Game == "grind") CdGrind = Time.unscaledTime + GrindCd;
            Game = null;
            knifeHeld = false;
            Dropping = false;
            dropLeft = 0;
            birds.Clear();
            notes.Clear();
        }

        public float CooldownLeft(string id)
        {
            float until = 0;
            if (id == "cricket") until = CdCricket;
            else if (id == "pigeon") until = CdPigeon;
            else if (id == "scissors" || id == "grind") until = CdGrind;
            return Mathf.Max(0, until - Time.unscaledTime);
        }

        public static string HuntId(string abilityId) => abilityId == "scissors" ? "grind" : abilityId;
        public static string AbilityId(string huntId) => huntId == "grind" ? "scissors" : huntId;

        public bool TryOpen(string id)
        {
            if (Open) return false;
            if (CooldownLeft(id) > 0 || CooldownLeft(AbilityId(id)) > 0) return false;
            Game = id;
            grindStroke = 0;
            grindHits = 0;
            coverDelay = .2f;
            Dropping = false;
            dropLeft = 0;
            Knife = new Vector2(.2f, .75f);
            hopFrom = hopTo = Bug = new Vector2(.16f, .74f);
            trap = new Vector2(.5f, .5f);
            hopT = 0;
            hopDur = .7f;
            hopArc = .12f;
            birds.Clear();
            notes.Clear();
            if (id == "pigeon")
            {
                birds.Add(new D22Bird { pos = new Vector2(.5f, .4f), vel = Vector2.zero });
                PlayPigeon();
            }
            return true;
        }

        public string AimHint(Camera camera)
        {
            if (!Spawned || camera == null) return null;
            var aim = Aim(camera);
            if (aim == D22MarkKind.Cricket)
                return Cd(CdCricket, flow.Abilities != null && flow.Abilities.Learned("cricket") ? "2 蛐蛐" : "E 蛐蛐");
            if (aim == D22MarkKind.Grind)
                return Cd(CdGrind, flow.Abilities != null && flow.Abilities.Learned("scissors") ? "4 磨刀" : "E 磨刀");
            if (aim == D22MarkKind.Pigeons)
                return Cd(CdPigeon, flow.Abilities != null && flow.Abilities.Learned("pigeon") ? "3 鸽子" : "E 鸽子");
            return null;
        }

        static string Cd(float until, string ready)
        {
            float left = until - Time.unscaledTime;
            return left > 0 ? $"{ready}  {Mathf.CeilToInt(left)}s" : ready;
        }

        public D22MarkKind? Aim(Camera camera)
        {
            if (!Spawned || camera == null) return null;
            var pos = camera.transform.position;
            var fwd = camera.transform.forward;
            if (pigeonMark)
            {
                Vector3 to = pigeonMark.transform.position - pos;
                if (fwd.y > .28f && to.y > .6f && Vector3.Dot(fwd, to.normalized) > .72f && to.magnitude < 14)
                    return D22MarkKind.Pigeons;
            }
            if (cricketMark && Vector3.Distance(pos, cricketMark.transform.position) < cricketMark.radius)
                return D22MarkKind.Cricket;
            if (grindMark && Vector3.Distance(pos, grindMark.transform.position) < grindMark.radius)
                return D22MarkKind.Grind;
            return null;
        }

        public bool Interact(Camera camera, Action<D22Line[], Action> talk, D22Story story)
        {
            var aim = Aim(camera);
            if (aim == D22MarkKind.Cricket)
            {
                if (!heardCricket) { heardCricket = true; talk(story.cricketTalk, () => { flow.ResumeHunt("cricket"); }); }
                else flow.ResumeHunt("cricket");
                return true;
            }
            if (aim == D22MarkKind.Pigeons)
            {
                if (!heardPigeon) { heardPigeon = true; talk(story.pigeonTalk, () => { flow.ResumeHunt("pigeon"); }); }
                else flow.ResumeHunt("pigeon");
                return true;
            }
            if (aim == D22MarkKind.Grind)
            {
                if (!heardGrind) { heardGrind = true; talk(story.grindTalk, () => { flow.ResumeHunt("grind"); }); }
                else flow.ResumeHunt("grind");
                return true;
            }
            return false;
        }

        public void Tick(Camera camera, float volume)
        {
            float dt = Time.unscaledDeltaTime;
            if (pigeonMark)
            {
                float t = Time.unscaledTime;
                pigeonMark.transform.position = pigeonHome + new Vector3(Mathf.Sin(t * .7f) * 2.1f, Mathf.Sin(t * .45f) * .35f, Mathf.Cos(t * .7f) * 1.3f);
            }
            UpdateNear(camera, volume);
            UpdateFlock();
            UpdateGrindLoop(camera);
            if (!Open) return;
            coverDelay = Mathf.Max(0, coverDelay - dt);
            if (Game == "cricket")
            {
                hopT += dt;
                if (hopT >= hopDur)
                {
                    hopFrom = hopTo;
                    hopTo = new Vector2(UnityEngine.Random.Range(.14f, .86f), UnityEngine.Random.Range(.22f, .8f));
                    hopT = 0;
                    hopDur = UnityEngine.Random.Range(.65f, 1.15f);
                    hopArc = UnityEngine.Random.Range(.08f, .18f);
                }
                float u = hopDur < .001f ? 1 : Mathf.Clamp01(hopT / hopDur);
                u = u * u * (3f - 2f * u);
                Bug = Vector2.Lerp(hopFrom, hopTo, u) + new Vector2(0, -Mathf.Sin(u * Mathf.PI) * hopArc);
                if (Dropping)
                {
                    dropLeft -= dt;
                    if (dropLeft <= 0)
                    {
                        Dropping = false;
                        dropLeft = 0;
                        if (InTrap(Bug, trap)) Catch();
                    }
                }
            }
            else if (Game == "pigeon")
            {
                for (int i = notes.Count - 1; i >= 0; i--)
                {
                    notes[i].life -= dt;
                    if (notes[i].life <= 0) notes.RemoveAt(i);
                }
            }
        }

        void UpdateNear(Camera camera, float volume)
        {
            if (!cricketMark || camera == null || cricketSfx == null || (Open && Game == "cricket"))
            {
                near.volume = 0;
                if (near.isPlaying) near.Stop();
                return;
            }
            float d = Vector3.Distance(camera.transform.position, cricketMark.transform.position);
            float vol = d > 8 ? 0 : Mathf.Clamp01(Mathf.Pow(1 - d / 8f, 1.15f)) * Mathf.Lerp(.7f, 1f, volume);
            near.volume = vol;
            if (vol > .02f)
            {
                if (near.clip != cricketSfx) near.clip = cricketSfx;
                if (!near.isPlaying) near.Play();
            }
            else if (near.isPlaying) near.Stop();
        }

        public void Pointer(Vector2 n, bool down, bool drag, bool up)
        {
            if (!Open) return;
            if (Game == "cricket" && down) Drop(n);
            else if (Game == "pigeon" && down) ClickPigeon(n);
            else if (Game == "grind")
            {
                if (down) { knifeHeld = true; Knife = n; lastKnife = n; grindStroke = 0; }
                else if (drag && knifeHeld)
                {
                    Knife = n;
                    bool onStone = n.y > .28f && n.y < .8f && n.x > .08f && n.x < .92f;
                    if (onStone) grindStroke += Vector2.Distance(n, lastKnife);
                    lastKnife = n;
                    if (grindStroke > .42f)
                    {
                        grindStroke = 0;
                        grindHits++;
                        if (grindHits >= 3)
                        {
                            grindHits = 0;
                            Grind++;
                            if (Grant("scissors")) flow.CloseHunt();
                            flow.Stage?.NoteGrind();
                        }
                    }
                }
                else if (up) knifeHeld = false;
            }
        }

        public void AimTrap(Vector2 n)
        {
            if (Game != "cricket" || Dropping) return;
            trap = n;
        }

        public void Drop(Vector2 n)
        {
            if (Game != "cricket" || Dropping || coverDelay > 0) return;
            trap = n;
            Dropping = true;
            dropLeft = DropTime;
        }

        static bool InTrap(Vector2 bug, Vector2 box)
        {
            float h = TrapSize * .5f;
            return bug.x > box.x - h && bug.x < box.x + h && bug.y > box.y - h && bug.y < box.y + h;
        }

        void Catch()
        {
            if (Game != "cricket") return;
            Cricket++;
            Grant("cricket");
            if (cricketSfx)
            {
                oneshot.pitch = 1;
                oneshot.volume = 1f;
                oneshot.PlayOneShot(cricketSfx);
            }
            Close();
        }

        void ClickPigeon(Vector2 n)
        {
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(n, notes[i].pos) < .1f)
                {
                    notes.RemoveAt(i);
                    Pigeon++;
                    if (Grant("pigeon")) flow.CloseHunt();
                    return;
                }
            }
            foreach (var b in birds)
            {
                if (Vector2.Distance(n, b.pos) < .12f)
                {
                    notes.Add(new D22Note
                    {
                        pos = new Vector2(UnityEngine.Random.Range(.12f, .88f), UnityEngine.Random.Range(.16f, .84f)),
                        life = 1.6f
                    });
                    return;
                }
            }
        }

        void StartFlock(bool fromStart)
        {
            if (!pigeonSfx || !Spawned) return;
            if (flock.clip != pigeonSfx) flock.clip = pigeonSfx;
            flock.loop = true;
            flock.volume = FlockVol();
            if (!flock.isPlaying || fromStart) flock.Play();
        }

        void PlayPigeon()
        {
            if (!pigeonSfx) return;
            oneshot.pitch = 1;
            oneshot.volume = 1f;
            oneshot.PlayOneShot(pigeonSfx);
        }

        void UpdateFlock()
        {
            if (!Spawned || !pigeonSfx)
            {
                if (flock.isPlaying) flock.Stop();
                return;
            }
            if (flock.clip != pigeonSfx) flock.clip = pigeonSfx;
            flock.volume = Open && Game == "pigeon" ? 0 : FlockVol();
            if (!flock.isPlaying) flock.Play();
        }

        float FlockVol() => Mathf.Lerp(.55f, .95f, flow.Volume);

        void StartGrind(bool fromStart)
        {
            if (!grindSfx || !Spawned) return;
            if (grindLoop.clip != grindSfx) grindLoop.clip = grindSfx;
            grindLoop.loop = true;
            grindLoop.volume = FlockVol();
            if (!grindLoop.isPlaying || fromStart) grindLoop.Play();
        }

        void PlayGrind()
        {
            if (!grindSfx) return;
            oneshot.pitch = 1;
            oneshot.volume = 1f;
            oneshot.PlayOneShot(grindSfx);
        }

        void UpdateGrindLoop(Camera camera)
        {
            if (!grindSfx)
            {
                if (grindLoop.isPlaying) grindLoop.Stop();
                return;
            }
            if (grindLoop.clip != grindSfx) grindLoop.clip = grindSfx;
            grindLoop.loop = true;
            if (Open && Game == "grind")
            {
                grindLoop.volume = 1f;
                if (!grindLoop.isPlaying) grindLoop.Play();
                return;
            }
            if (!Spawned || grindMark == null || camera == null)
            {
                if (grindLoop.isPlaying) grindLoop.Stop();
                return;
            }
            float d = Vector3.Distance(camera.transform.position, grindMark.transform.position);
            float vol = d > 8f ? 0 : Mathf.Clamp01(Mathf.Pow(1f - d / 8f, 1.15f)) * Mathf.Lerp(.75f, 1f, flow.Volume);
            grindLoop.volume = vol;
            if (vol > .02f)
            {
                if (!grindLoop.isPlaying) grindLoop.Play();
            }
            else if (grindLoop.isPlaying) grindLoop.Stop();
        }

        bool Grant(string abilityId)
        {
            if (flow.Abilities != null && flow.Abilities.Learn(abilityId))
            {
                flow.UI.PlayLearnFly(abilityId);
                return true;
            }
            return false;
        }

        public string Tally(int sips) => Quota(sips);

        static AudioClip MakeChirp()
        {
            const int rate = 44100;
            int samples = (int)(rate * .55f);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                int pulse = Mathf.FloorToInt(t / .055f);
                float local = t - pulse * .055f;
                if (pulse > 5 || local > .018f) continue;
                float env = Mathf.Sin(local / .018f * Mathf.PI);
                data[i] = Mathf.Sin(t * 3800 * Mathf.PI * 2) * env * .9f;
            }
            var clip = AudioClip.Create("cricket-near", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
