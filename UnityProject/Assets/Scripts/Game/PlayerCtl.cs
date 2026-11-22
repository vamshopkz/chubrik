using System.Collections.Generic;
using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// The player: third-person platformer controller (run, jump, double jump, glide with wings, jetpack boost),
    /// orbit camera with collision, moving-platform carry, bounce pads, weapon attacks, respawn when falling into water.
    /// </summary>
    public partial class PlayerCtl : MonoBehaviour   // 22.11: swimming is in Water.cs
    {
        public static PlayerCtl I;
        public Camera Cam;
        public ChubrikModel Model;
        public ChubrikAnim Anim;
        public CharacterController CC;
        /// <summary>World 4: covered in snow (a snowball hit) — slower until this time.</summary>
        public float SlowUntil;
        /// <summary>Something else (the snowboard) moves the chubrik; input only steers it.</summary>
        public bool Riding;
        /// <summary>This frame's stick / keys (the snowboard steers with it).</summary>
        public Vector2 Steer { get; private set; }
        /// <summary>Dive: after the second jump a hit throws the chubrik forward on his belly; he slides on landing
        /// and lies there until you press jump.</summary>
        int dive;            // 0 = no, 1 = flying belly-first, 2 = lying / sliding on the ground
        bool airJumped;      // the second jump was used (the dive needs it)
        public bool AirJumped => airJumped;
        public bool Diving => dive == 1;
        public bool Lying => dive == 2;
        public int Dives { get; private set; }
        /// <summary>The leap: running, B (hit) then A (jump) — flying far forward with a «хэ-хэй!».</summary>
        bool leaping;
        float leapReady;
        public bool Leaping => leaping;
        public int Leaps { get; private set; }
        /// <summary>17.11 the plunge: one or two jumps, then B — straight down head first with a funny flop on landing.
        /// A quick tap after one jump = a small plunge; holding B (after one or two jumps) = the big one.</summary>
        int plunge;          // 0 = no, 1 = small, 2 = big
        float attackDownT = -1;
        public bool Plunging => plunge != 0;
        public bool PlungeBig => plunge == 2;
        public int Plunges { get; private set; }
        bool superBounce;
        public int SuperBounces { get; private set; }
        public bool HoldAttack;   // tests: B held from code
        public bool AttackHeld { get; private set; }
        /// <summary>Ledge grab: a jump that falls short catches the edge by itself and climbs up.</summary>
        int hang;            // 0 = no, 1 = hanging / climbing
        float hangT, grabCd;
        Vector3 hangPos, hangTop;
        public bool Hanging => hang != 0;
        /// <summary>18.11: when we last stood on a tree top (two jumps off it lead into the backrooms).</summary>
        public float LastTreeTop;
        public int Grabs { get; private set; }
        /// <summary>The jet snowmobile from the frozen mailbox: fast, climbs steep slopes, knocks snowmen over.</summary>
        public bool Snowmobile;
        /// <summary>Standing on ice (a collider with an Ice marker): you slide unless you wear skates.</summary>
        public bool OnIce { get; private set; }
        public bool Skates => look != null && look.Of("shoes") == Catalog.Skates;
        /// <summary>The Shipozavr crown keeps ordinary spikes from hurting.</summary>
        public bool SpikeProof => look != null && look.Of("hat") == Catalog.ShipoCrown;
        Pet pet;
        /// <summary>v0.31: the pet (it does a trick for every sticker).</summary>
        public Pet PetNow => pet;
        Aura aura;
        BlobShadow shadow;

        public Vector2 MoveInput, LookDelta;
        public bool Frozen, JumpHeld, HoldJump;   // HoldJump: held from code (tests)
        bool jumpQueued;
        public float yaw, pitch = 16f;
        public Vector3 Velocity => vel;
        Vector3 vel;
        public bool Grounded { get; private set; }
        int airJumps;
        float coyote, idle, safeT, lastGroundY, camDist = 6.5f, attackCd, fallStart;
        Transform ground;
        Vector3 groundLocal; Quaternion groundRot;
        public Vector3 SafePos = new Vector3(0, 0.5f, -4);
        public bool MenuOrbit;         // title screen: camera circles the lobby
        /// <summary>17.11 the wardrobe shows the real chubrik in the world: the camera turns to the nearest wall
        /// (he stands with his back to it), the chubrik sits in the free middle between the wardrobe panels.</summary>
        public bool WardrobeView;
        public float WardrobeCx = 0.5f, WardrobeSpin;
        public string WardrobeZoom = "body";
        public bool ShowWalk;
        public Vector3 WardrobeDir { get; private set; }   // from the chubrik to the camera
        public bool WardrobeWall { get; private set; }
        Vector3 wCamPos; Quaternion wCamRot; bool wCamInit;
        float orbitT;
        public float RunSpeed = 6.8f;
        public float WaterY = -0.9f;
        public int Hearts = 3;
        public Vector3 Checkpoint;
        float invuln;
        Renderer[] rends;
        Look look;
        public int Jumps { get; private set; }   // total jumps made (autotest)
        public int Flips { get; private set; }   // somersaults made (autotest)

        public static void Create()
        {
            var go = new GameObject("Player") { layer = Layers.Player };
            I = go.AddComponent<PlayerCtl>();
            I.CC = go.AddComponent<CharacterController>();
            I.CC.radius = 0.38f; I.CC.height = 1.45f; I.CC.center = new Vector3(0, 0.74f, 0);
            I.CC.slopeLimit = 50; I.CC.stepOffset = 0.35f; I.CC.skinWidth = 0.04f;
            var cam = new GameObject("Camera") { tag = "MainCamera" };
            I.Cam = cam.AddComponent<Camera>();
            I.Cam.fieldOfView = 58; I.Cam.nearClipPlane = 0.1f; I.Cam.farClipPlane = 600;
            I.Cam.cullingMask = ~(1 << Layers.Preview);
            cam.AddComponent<AudioListener>();
            I.Model = ChubrikModel.Create("Chubrik", go.transform, Save.CurrentLook(), Layers.Player);
            I.Anim = go.AddComponent<ChubrikAnim>();
            I.Anim.model = I.Model;
            I.Anim.OnStep = I.Step;
            I.pet = new GameObject("Pet").AddComponent<Pet>();
            I.pet.owner = go.transform;
            I.aura = go.AddComponent<Aura>();
            I.shadow = go.AddComponent<BlobShadow>();
            I.shadow.target = go.transform;
            I.ApplyLook(Save.CurrentLook());
        }

        public void ApplyLook(Look l)
        {
            look = l.Copy();
            Model.Build(look);
            Anim.model = Model;
            Anim.walkStyle = Mathf.Max(0, look.Idx("walk"));
            Anim.emote = Mathf.Max(0, look.Idx("emote"));
            pet.Set(look.Of("pet"), Layers.Player);
            pet.scale = Mathf.Clamp(Model.SizeScale, 0.7f, 1.5f);
            aura.model = Model;
            aura.Set(Aura.KindOf(look));
            shadow.Set(Mathf.Max(0, look.Idx("shadow")), Catalog.TintOf(look.Of("shadow")));
            shadow.size = Model.SizeScale;
            float s = Model.SizeScale;
            BaseCapsule();
            if (swim != 0) SwimCapsule();
            camDist = 6.5f * Mathf.Clamp(s, 0.8f, 1.5f);
            rends = Model.GetComponentsInChildren<Renderer>(true);
            Hearts = Mathf.Clamp(Hearts, 1, Save.P.maxHearts);
        }

        public void Teleport(Vector3 pos, float faceYaw = float.NaN)
        {
            CC.enabled = false;
            EndSwim();
            transform.position = pos;
            CC.enabled = true;
            vel = Vector3.zero;
            ground = null;
            SetDive(0);
            EndLeap();
            plunge = 0; Anim.plunge = false; attackDownT = -1;
            if (hang != 0) { hang = 0; Anim.hang = false; }
            if (!float.IsNaN(faceYaw)) { yaw = faceYaw; transform.rotation = Quaternion.Euler(0, faceYaw, 0); }
        }

        public void QueueJump() { jumpQueued = true; }

        public void Emote()
        {
            if (Anim.emoting) Anim.StopEmote(); else Anim.PlayEmote();
        }

        void Update()
        {
            Zones.Tick();
            Backrooms.Tick(this);
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (MenuOrbit) { vel = Vector3.zero; Anim.speed01 = 0; Anim.grounded = true; return; }
            if (WardrobeView)
            {
                vel = Vector3.zero;
                transform.rotation = Quaternion.LookRotation(WardrobeDir) * Quaternion.Euler(0, WardrobeSpin, 0);
                Anim.grounded = true; Anim.vy = 0; Anim.speed01 = ShowWalk ? 0.8f : 0;
                return;
            }

            // ---- input ----
            Vector2 mv = MoveInput;
            bool kbJump = false, kbAttack = false;
            if (!Frozen && !Menus.AnyOpen && !UIRoot.TypingText)
            {
                var k = new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
                if (k.sqrMagnitude > 0) mv = k.normalized;
                kbJump = Input.GetKeyDown(KeyCode.Space);
                JumpHeld = Input.GetKey(KeyCode.Space) || Hud.JumpButtonHeld || HoldJump;
                kbAttack = Input.GetKeyDown(KeyCode.F) || (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButtonDown(0));
                if (Input.GetKeyDown(KeyCode.Q)) Stickers.Open();   // v0.31 stickers instead of emotions
                if (Input.GetKeyDown(KeyCode.E)) { if (!Zones.Interact()) Attack(); }
                if (Cursor.lockState == CursorLockMode.Locked)
                    LookDelta += new Vector2(Input.GetAxisRaw("Mouse X"), -Input.GetAxisRaw("Mouse Y")) * 2.2f * Save.P.sens;
                else if (Input.GetMouseButton(1))
                    LookDelta += new Vector2(Input.GetAxisRaw("Mouse X"), -Input.GetAxisRaw("Mouse Y")) * 2.2f * Save.P.sens;
            }
            else { mv = Vector2.zero; JumpHeld = false; }
            AttackHeld = !Frozen && (HoldAttack || Hud.AttackButtonHeld || (!UIRoot.TypingText && (Input.GetKey(KeyCode.F) || (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(0)))));
            Steer = mv;
            if (kbAttack) Attack();
            yaw += LookDelta.x * 0.9f;
            pitch = Mathf.Clamp(pitch + LookDelta.y * 0.6f, -10f, 65f);
            LookDelta = Vector2.zero;

            if (hang != 0) { HangUpdate(dt); return; }
            if (Riding) { ground = null; vel = Vector3.zero; Grounded = true; Anim.grounded = true; Anim.speed01 = 0.3f; return; }   // the snowboard moves us
            if (SwimUpdate(dt, mv, jumpQueued || kbJump)) { jumpQueued = false; return; }   // 22.11 in the water: swimming

            // ---- ride moving platforms ----
            if (ground != null && Grounded)
            {
                var now = ground.TransformPoint(groundLocal);
                var delta = now - transform.position;
                float dyaw = (ground.rotation * Quaternion.Inverse(groundRot)).eulerAngles.y;
                if (delta.sqrMagnitude > 0.000001f && delta.sqrMagnitude < 4) CC.Move(delta);
                if (Mathf.Abs(Mathf.DeltaAngle(0, dyaw)) > 0.01f) { transform.Rotate(0, dyaw, 0); yaw += dyaw; }
            }

            // ---- horizontal ----
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            var wish = (fwd * mv.y + right * mv.x);
            if (wish.sqrMagnitude > 1) wish.Normalize();
            float speed = RunSpeed * (look != null && look.Idx("walk") == 2 ? 0.75f : 1f) * Mathf.Lerp(1f, 1.25f, Mathf.InverseLerp(1f, 1.8f, Model.SizeScale));
            if (Skates) speed *= 1.2f;
            if (Snowmobile) speed *= 1.8f;
            if (Time.time < SlowUntil) speed *= 0.55f;
            if (Cheats.Flying) speed *= 1.6f;
            var hv = new Vector3(vel.x, 0, vel.z);
            var target = wish * speed;
            float accel = Grounded ? (OnIce && !Skates && !Snowmobile ? 3.5f : 40f) : 16f;   // ice: hard to start, harder to stop
            if (plunge == 2) hv = Vector3.MoveTowards(hv, Vector3.zero, 40f * dt);            // the big plunge: straight down
            else if (plunge == 1) hv = Vector3.MoveTowards(hv, hv * 0.5f, 10f * dt);
            else if (dive == 1) { }                                                               // flying belly-first: no steering
            else if (leaping) hv = Vector3.MoveTowards(hv, hv.normalized * 14f + wish * 3f, 4f * dt);
            else if (dive == 2) hv = Vector3.MoveTowards(hv, Vector3.zero, (OnIce ? 4f : 28f) * dt);   // a short belly slide (~2.5 m, longer on ice)
            else hv = Vector3.MoveTowards(hv, target, accel * dt);
            if (wish.sqrMagnitude > 0.01f && dive == 0)
            {
                var face = Quaternion.LookRotation(wish);
                transform.rotation = Quaternion.Slerp(transform.rotation, face, dt * 14);
                idle = 0;
                if (Anim.emoting) Anim.StopEmote();
            }

            // ---- vertical ----
            bool wantJump = jumpQueued || kbJump;
            jumpQueued = false;
            if (Grounded) { coyote = 0.12f; airJumps = Model.HasJet ? 2 : 1; airJumped = false; }
            else coyote -= dt;
            if (dive == 2 && wantJump && !Frozen && !Menus.AnyOpen)
            {
                // stand up with a little hop
                SetDive(0);
                vel.y = 6f; Grounded = false; coyote = 0;
                AudioFx.Play("jump4");
                Effects.Emit("dust", transform.position, 8, 0.4f);
                wantJump = false;
            }
            if (dive == 1 || plunge != 0) wantJump = false;
            if (splashAt > 0 && Time.time > splashAt) { splashAt = -1; if (!Grounded) StartPlunge(true); else splashFx = false; }
            if (twoB >= 0 && !Grounded && hang == 0 && !Riding)
            {
                // v0.31 after two jumps: B held — straight down with the super bounce; B just tapped — the belly flop
                if (AttackHeld && Time.time - twoB > 0.15f) { twoB = -1; StartPlunge(true); superBounce = true; }
                else if (!AttackHeld) { twoB = -1; StartFlop(); }
            }
            else if (Grounded) twoB = -1;
            if (attackDownT >= 0 && !Grounded && plunge != 2 && hang == 0 && !Riding)
            {
                if (!AttackHeld) attackDownT = -1;
                else if (Time.time - attackDownT > 0.15f) StartPlunge(true);   // B held: the big one
            }
            if (wantJump && !Frozen && !Menus.AnyOpen)
            {
                if (coyote > 0 && Time.time < leapReady && dive == 0 && !Riding && !Snowmobile) { Leap(); hv = new Vector3(vel.x, 0, vel.z); }
                else if (coyote > 0) DoJump(11f, false);
                else if (airJumps > 0) { airJumps--; airJumped = true; DoJump(9.5f, true); }
            }
            float g = -30f;
            bool gliding = false;
            if (!Grounded && vel.y < 0 && JumpHeld && Model.CanGlide)
            {
                gliding = true;
                vel.y = Mathf.Max(vel.y + g * 0.15f * dt, -2.2f);
                hv = Vector3.MoveTowards(hv, wish * speed * 1.25f, 12 * dt);
            }
            else vel.y += g * dt;
            if (plunge == 2) vel.y = Mathf.Min(vel.y, -24f);
            else if (plunge == 1) vel.y = Mathf.Min(vel.y, -11f);
            if (Cheats.Flying && !Riding && !Frozen)
            {
                // 18.11 the flying cheat: A held — up, B held — down, nothing — hover
                vel.y = (JumpHeld ? 8f : 0f) - (AttackHeld ? 8f : 0f);
                if (vel.y > 0) Grounded = false;
            }
            if (Grounded && vel.y < 0) vel.y = -3f;
            vel.x = hv.x; vel.z = hv.z;

            CC.slopeLimit = Snowmobile ? 80f : 50f;
            bool wasGrounded = Grounded;
            float vyBefore = vel.y;
            ground = null;
            var flags = CC.Move(vel * dt);
            Grounded = (flags & CollisionFlags.Below) != 0;
            OnIce = Grounded && ground != null && ground.GetComponentInParent<Ice>() != null;
            if (Grounded && ground != null && ground.GetComponent<TreeTop>() != null) LastTreeTop = Time.time;
            if ((flags & CollisionFlags.Above) != 0 && vel.y > 0) vel.y = 0;
            if (Grounded && ground != null) { groundLocal = ground.InverseTransformPoint(transform.position); groundRot = ground.rotation; }
            if (plunge != 0 && Grounded) PlungeLand();
            else if (dive == 1 && Grounded && flop)
            {
                flop = false; SetDive(2);   // v0.31: the A-A-B dive lands on the belly and lies there until A
                vel = new Vector3(0, vel.y, 0);
                AudioFx.Play("land4", 0.4f);
                Effects.Emit("dust", transform.position, 8, 0.35f);
            }
            else if (dive == 1 && Grounded)
            {
                SetDive(2);
                AudioFx.Play("land4", 0.5f); AudioFx.Play("jump4");
                Effects.Emit("dust", transform.position + transform.forward * 0.4f, 16, 0.5f);
            }
            else if (dive == 2 && !Grounded && vel.y < -4) SetDive(1);   // slid off an edge: fly belly-first again
            if (!wasGrounded && Grounded)
            {
                EndLeap();
                float impact = Mathf.InverseLerp(-4, -22, vyBefore);
                Anim.Land(impact);
                if (vyBefore < -6) { AudioFx.Land(Mathf.Max(0, look.Idx("lsnd")), Catalog.PitchOf(look.Of("lsnd"))); Effects.Emit("dust", transform.position, 6, 0.3f); }
            }
            if (wasGrounded && !Grounded) fallStart = transform.position.y;
            grabCd -= dt;
            if (!Grounded && dive == 0 && plunge == 0 && grabCd <= 0 && vel.y < 2.5f && !Frozen && !Snowmobile && TryGrab()) return;

            // safe spot for respawns
            bool overWater = Water.At(transform.position, out _, out _);   // 22.11 a swimming river is not the sea
            if (Grounded && ground != null && ground.GetComponent<Mover>() == null && !overWater)
            {
                safeT += dt;
                if (safeT > 0.5f) { safeT = 0; SafePos = transform.position + Vector3.up * 0.2f; }
            }
            if (transform.position.y < WaterY - 0.5f && !overWater) { if (Worlds.Level) FellOff(); else FellInWater(); }
            TickInvuln(dt);

            // ---- animation ----
            Anim.speed01 = Mathf.Clamp01(new Vector3(vel.x, 0, vel.z).magnitude / RunSpeed);
            Anim.grounded = Grounded;
            Anim.vy = vel.y;
            Anim.gliding = gliding;
            if (gliding && Random.value < 0.3f) Effects.Emit("comet", transform.position + Vector3.up * 0.5f, 1, 0.3f);
            if (Grounded && Anim.speed01 < 0.05f && !Frozen)
            {
                idle += dt;
                if (idle > 6f && !Anim.emoting && dive == 0) { Anim.PlayEmote(); idle = -4f; }
            }
            attackCd -= dt;
        }

        /// <summary>After a hit: blinking while you can't be hurt.</summary>
        void TickInvuln(float dt)
        {
            if (invuln <= 0) return;
            invuln -= dt;
            bool show = invuln <= 0 || ((int)(invuln * 12)) % 2 == 0;
            if (rends != null) foreach (var r in rends) if (r) r.enabled = show;
        }

        void DoJump(float v, bool air)
        {
            EndLeap();
            vel.y = v;
            Grounded = false; coyote = 0;
            Jumps++;
            AudioFx.Jump(Mathf.Max(0, look.Idx("jsnd")), air, Catalog.PitchOf(look.Of("jsnd")));
            if (air)
            {
                Effects.Emit(Model.HasJet ? "fire" : "dust", transform.position + Vector3.up * 0.2f, 8, 0.25f, Vector3.down);
                Anim.Flip();   // the second jump is a somersault
                AudioFx.Play("flip");
                Flips++;
            }
            if (Anim.emoting) Anim.StopEmote();
            idle = 0;
        }

        void Leap()
        {
            leapReady = 0;
            var f = transform.forward; f.y = 0; f.Normalize();
            vel = f * 14.5f + Vector3.up * 9.5f;
            Grounded = false; coyote = 0;
            leaping = true; Anim.leap = true;
            Jumps++; Leaps++;
            AudioFx.Voice("heyhey", 1.35f);
            AudioFx.Play("swing", 0.6f);
            Effects.Emit("stardust", transform.position + Vector3.up * 0.5f, 12, 0.35f);
            if (Anim.emoting) Anim.StopEmote();
            idle = 0;
        }

        void EndLeap() { leaping = false; if (Anim) Anim.leap = false; }

        /// <summary>A wall right in front at chest height with a flat top within reach: hang on its edge.</summary>
        bool TryGrab()
        {
            var f = transform.forward; f.y = 0;
            if (f.sqrMagnitude < 0.01f) return false;
            f.Normalize();
            var hv = new Vector3(vel.x, 0, vel.z);
            if (Vector3.Dot(hv, f) < 1.2f && Vector3.Dot(Steer, Vector2.up) < 0.3f && Steer.sqrMagnitude < 0.1f) return false;
            float s = Mathf.Clamp(Model.SizeScale, 0.6f, 1.6f);
            var chest = transform.position + Vector3.up * 1.0f * s;
            if (!Physics.Raycast(chest, f, out var wall, 0.8f * s, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
            if (Mathf.Abs(wall.normal.y) > 0.35f) return false;
            if (wall.collider.GetComponentInParent<BouncePad>() != null || wall.collider.GetComponentInParent<Hurter>() != null || wall.collider.GetComponentInParent<Mover>() != null) return false;
            var n = wall.normal; n.y = 0; n.Normalize();
            var from = wall.point - n * 0.3f * s + Vector3.up * 1.4f * s;
            if (!Physics.Raycast(from, Vector3.down, out var top, 1.9f * s, Layers.SolidMask, QueryTriggerInteraction.Ignore)) return false;
            if (top.normal.y < 0.75f || top.collider.GetComponentInParent<Hurter>() != null) return false;
            float rise = top.point.y - transform.position.y;
            if (rise < 0.55f * s || rise > 1.95f * s) return false;
            var stand = new Vector3(wall.point.x, top.point.y + 0.06f, wall.point.z) - n * 0.5f * s;
            float r = CC.radius * 0.85f;
            if (Physics.CheckCapsule(stand + Vector3.up * (r + 0.08f), stand + Vector3.up * (CC.height - r), r, Layers.BlockMask, QueryTriggerInteraction.Ignore)) return false;
            hang = 1; hangT = 0; Grabs++;
            hangTop = stand;
            hangPos = new Vector3(wall.point.x, top.point.y - 1.3f * s, wall.point.z) + n * 0.42f * s;
            CC.enabled = false;
            transform.position = hangPos;
            transform.rotation = Quaternion.LookRotation(-n);
            vel = Vector3.zero;
            EndLeap();
            Anim.hang = true; Anim.grounded = false; Anim.speed01 = 0; Anim.vy = 0;
            AudioFx.Play("land4", 0.5f);
            Effects.Emit("dust", new Vector3(wall.point.x, top.point.y, wall.point.z), 6, 0.25f);
            return true;
        }

        /// <summary>Hang for a moment, then pull up and step onto the top.</summary>
        void HangUpdate(float dt)
        {
            hangT += dt;
            Anim.grounded = false; Anim.speed01 = 0; Anim.vy = 0;
            const float hold = 0.3f, climb = 0.4f;
            if (hangT < hold) { transform.position = hangPos; return; }
            float t = Mathf.Clamp01((hangT - hold) / climb);
            if (t < 0.6f) transform.position = Vector3.Lerp(hangPos, new Vector3(hangPos.x, hangTop.y, hangPos.z), t / 0.6f);
            else transform.position = Vector3.Lerp(new Vector3(hangPos.x, hangTop.y, hangPos.z), hangTop, (t - 0.6f) / 0.4f);
            if (t < 1) return;
            hang = 0; Anim.hang = false; grabCd = 0.4f;
            CC.enabled = true;
            vel = Vector3.zero; Grounded = true; coyote = 0.12f;
            Anim.Land(0.4f);
            AudioFx.Play("jump4", 0.6f);
        }

        bool splashFx; float splashAt = -1;
        /// <summary>v0.31 sticker «Взлёт и плюх»: up high, then straight down — rainbow drops splash round you.</summary>
        public void StickerSplash()
        {
            if (Riding || Snowmobile || swim != 0) return;
            Launch(15f);
            splashAt = Time.time + 0.6f; splashFx = true;
            AudioFx.Play("boing");
        }
        /// <summary>v0.31 stickers «Ракета», «Прыжок до неба», «Сальто»: a jump up.</summary>
        public void StickerJump(float v) { if (Riding || Snowmobile || swim != 0) return; Launch(v); }
        public int Splashes { get; private set; }

        float twoB = -1;
        public int Flops { get; private set; }
        bool flop;
        /// <summary>v0.31 (A, A, B tapped): no hop — he flies down and forward belly first, arms straight ahead,
        /// and on the ground simply stands up again (no flop, no slide).</summary>
        void StartFlop()
        {
            if (plunge != 0 || dive != 0) return;
            EndLeap();
            superBounce = false;
            Flops++;
            var f = transform.forward; f.y = 0; f = f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
            vel = f * 9f + Vector3.down * 7f;
            flop = true;
            SetDive(1);   // flying belly first (no steering)
            AudioFx.Play("swing"); AudioFx.Play("boing", 0.5f);
            Effects.Emit("stardust", transform.position + Vector3.up * 0.6f, 8, 0.3f);
        }

        void StartPlunge(bool big)
        {
            if (big && plunge == 2) return;
            superBounce = false;
            bool was = plunge != 0;
            SetDive(0);
            EndLeap();
            plunge = big ? 2 : 1;
            attackDownT = -1;
            Anim.plunge = true;
            if (!was) Plunges++;
            vel = new Vector3(vel.x * (big ? 0.2f : 0.5f), big ? -24f : -11f, vel.z * (big ? 0.2f : 0.5f));
            AudioFx.Play("swing"); AudioFx.Play(big ? "jump5" : "jump4", big ? 0.4f : 0.7f);
            Effects.Emit("stardust", transform.position + Vector3.up * 0.6f, big ? 14 : 6, 0.3f);
        }

        /// <summary>Hits the ground head first: a funny flop on the belly with a little bounce (a big plunge also smashes things).</summary>
        void PlungeLand()
        {
            bool big = plunge == 2;
            plunge = 0; Anim.plunge = false;
            if (splashFx) { splashFx = false; Splashes++; Stickers.Rainbow(transform.position); }
            if (big && superBounce)
            {
                // the super bounce: higher than a double jump
                superBounce = false;
                SuperBounces++;
                SetDive(0);
                vel = new Vector3(0, 18.5f, 0); Grounded = false; coyote = 0; airJumps = 0; airJumped = true;
                AudioFx.Play("boing", 1f); AudioFx.Play("land4", 0.8f);
                Effects.Emit("dust", transform.position, 26, 0.8f);
                Effects.Emit("stardust", transform.position + Vector3.up * 0.5f, 18, 0.6f);
                Hit(transform.position + Vector3.up * 0.3f, 1.8f);
                World4.HitNear(transform.position, true);
                Rescue.HitNear(transform.position);
                Anim.Flip();
                return;
            }
            SetDive(2);
            vel = new Vector3(0, big ? 4.5f : 2.5f, 0); Grounded = false;   // boing!
            AudioFx.Play("land4", big ? 1f : 0.6f); AudioFx.Play("punch", big ? 0.9f : 0.5f);
            AudioFx.Voice("heyhey", big ? 0.7f : 0.9f, 0.6f);
            Effects.Emit("dust", transform.position, big ? 26 : 12, big ? 0.8f : 0.5f);
            Effects.Emit("stardust", transform.position + Vector3.up * 0.8f, big ? 10 : 4, 0.4f);
            Hit(transform.position + Vector3.up * 0.3f, big ? 1.8f : 1.1f);   // like a jump attack: breaks boxes, snowmen, ice cubes
            World4.HitNear(transform.position, true);
            Rescue.HitNear(transform.position);
            Anim.Land(1f);
        }

        void SetDive(int d)
        {
            dive = d;
            if (d != 1) flop = false;
            if (Anim) Anim.belly = d != 0;
        }

        /// <summary>Bounce pads, springs: launch upwards.</summary>
        public void Launch(float v)
        {
            vel.y = v;
            Grounded = false; coyote = 0; airJumps = Model.HasJet ? 2 : 1;
            SetDive(0);
            plunge = 0; Anim.plunge = false; attackDownT = -1;
            Anim.Land(0.6f);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
            {
                ground = hit.collider.transform;
                var pad = hit.collider.GetComponentInParent<BouncePad>();
                if (pad != null && vel.y <= 0.5f) pad.Bounce(this);
                var tile = hit.collider.GetComponent<NoteTile>();
                if (tile != null) tile.Press();
            }
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic && hit.rigidbody == Football.Ball) Football.Touch(hit.moveDirection, new Vector3(vel.x, 0, vel.z).magnitude);
            var hurt = hit.collider.GetComponentInParent<Hurter>();
            if (hurt != null) hurt.Touch(this);
        }

        void FellInWater()
        {
            AudioFx.Play("splash");
            Effects.Emit("splash", new Vector3(transform.position.x, WaterY, transform.position.z), 40, 0.4f, Vector3.up * 4);
            Effects.Damage(Mathf.Max(0, look.Idx("dmg")), Model);
            var back = SafePos;
            Teleport(back);
            Effects.Victory(Mathf.Max(0, look.Idx("win")), Model);
            Hud.Toast("Плюх! Ты вернулся на берег", UIKit.Cyan);
        }

        /// <summary>Training cactus: knock back + the chosen damage effect (the lobby is safe — no hearts are lost).</summary>
        public void Hurt(Vector3 from)
        {
            var away = transform.position - from; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            vel = away.normalized * 9 + Vector3.up * 7;
            Grounded = false;
            Effects.Damage(Mathf.Max(0, look.Idx("dmg")), Model);
            Hud.HeartFlash();
        }

        /// <summary>World 2: a mob touched you from the side — knock back and lose a heart.</summary>
        public void HurtBy(Vector3 from)
        {
            if (invuln > 0) return;
            if (Worlds.Current == 6 || Worlds.Current == World67.Id) { Hurt(from); return; }   // endless lives in the glitch worlds
            var away = transform.position - from; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            vel = away.normalized * 8 + Vector3.up * 6;
            Grounded = false;
            Effects.Damage(Mathf.Max(0, look.Idx("dmg")), Model);
            LoseHeart();
        }

        /// <summary>World 2: fell off the islands — a heart less, back to the last safe spot.</summary>
        void FellOff()
        {
            if (Worlds.Current == 6 || Worlds.Current == World67.Id)
            {
                // the glitch worlds: lives are endless here (the HUD says 100)
                Teleport(SafePos); invuln = 1f; Hud.Glitch();
                Hud.Toast(Worlds.Current == 6 ? "ОШИБКА ПАДЕНИЯ… возвращаем на место! (жизни тут бесконечные)" : "ОШИБКА 67: падение отменено! Возвращаем на место", UIKit.Pink);
                return;
            }
            Effects.Damage(Mathf.Max(0, look.Idx("dmg")), Model);
            invuln = 0;
            LoseHeart();
            if (Worlds.Current == 3) Effects.Emit("fire", transform.position, 40, 0.5f, Vector3.up * 4);
            if (Worlds.Current == 5)
            {
                // the poisonous swamp: a heart less and back to the start of the level
                Effects.Emit("bubble", transform.position, 30, 0.6f, Vector3.up * 2, new Color(0.8f, 0.5f, 1f));
                if (Hearts > 0) { Teleport(World5.Spawn, 0); invuln = 1.5f; Hud.Toast("Ядовитое болото! −1 сердечко, назад на старт", UIKit.Violet); }
                return;
            }
            if (Hearts > 0) { Teleport(SafePos); invuln = 1.5f; Hud.Toast(Worlds.Current == 3 ? "Ой, лава! −1 сердечко" : Worlds.Current == 4 ? "Брр, ледяная вода! −1 сердечко" : "Упал с острова! −1 сердечко", UIKit.Red); }
        }

        void LoseHeart()
        {
            Hearts--;
            invuln = 1.5f;
            Hud.HeartFlash();
            if (Hearts > 0) return;
            Hearts = Save.P.maxHearts;
            Hud.Toast("Ой! Сердечки кончились — начинаем с флажка", UIKit.Orange);
            Frozen = true;
            PlayVictory(() => { Teleport(Checkpoint); Frozen = UIRoot.Open != null; });
        }

        public bool Invulnerable => invuln > 0;

        public void PlayVictory(System.Action mid = null) => Effects.Victory(Mathf.Max(0, look.Idx("win")), Model, mid);

        void Step(bool left)
        {
            if (dive != 0) return;   // no footsteps while sliding on the belly
            var foot = (left ? Model.FootL : Model.FootR);
            var p = foot ? foot.position : transform.position;
            p.y = transform.position.y + 0.03f;
            int k = Effects.StepKind(look);
            if (k >= 0 || Random.value < 0.3f) Effects.Step(k, p, Catalog.TintOf(look.Of("steps")));
            AudioFx.Step(look.Idx("shoes"), look.Idx("walk") == 4);
            if (look.Of("skin") == Catalog.IceSkin) Effects.Emit("stardust", p + Vector3.up * 0.05f, 4, 0.2f);   // frosty trail
            if (Worlds.Current == 4) World4.Footprint(p, transform.rotation);
        }

        // ---------------- weapons ----------------

        public void Attack()
        {
            if (Frozen || plunge != 0 || swim != 0) return;   // in the water B is «dive» (held)
            if (Cheats.Flying && !Grounded) return;   // flying: B is «down», not a plunge
            if (dive == 1 && !Grounded) { attackDownT = Time.time; return; }   // holding B turns the dive into the plunge
            if (attackCd > 0 || dive != 0) return;
            if (!Grounded && !airJumped && !Riding && hang == 0 && !Snowmobile && !leaping)
            {
                // one jump, then B: a small plunge; hold B for the big one
                StartPlunge(false);
                attackDownT = Time.time;
                attackCd = 0.3f;
                // it is still a jump attack: ice cubes, snowmen, cages, crates next to you get it
                World4.HitNear(transform.position, true);
                Rescue.HitNear(transform.position);
                Hit(transform.position + Vector3.up * 0.6f + transform.forward * 0.6f, 1.0f);
                return;
            }
            if (!Grounded && airJumped && !Riding && hang == 0 && !Snowmobile)
            {
                // v0.31: A + A, then B: tapped — a hop up and a flop down on the belly, arms straight ahead;
                // held — 18.11's straight down with the bounce higher than a double jump (decided in Update)
                twoB = Time.time;
                attackCd = 0.4f;
                return;
            }
            attackCd = 0.4f;
            Anim.Attack();
            if (Football.TryKick(this)) return;   // 18.11: B next to the football kicks it
            if (Grounded && new Vector3(vel.x, 0, vel.z).magnitude > RunSpeed * 0.35f) leapReady = Time.time + 0.6f;   // B then A on the run = the leap
            if (Grounded && Zones.HitEnter()) { AudioFx.Play("punch"); return; }   // a hit next to a shop goes in
            if (Grounded && Chests.OpenNear(transform.position)) return;   // v0.31: B next to a chest opens it
            if (Grounded && Friends.TalkNear(transform.position)) return;   // v0.31: B next to a friend — he tells a story
            Rescue.HitNear(transform.position);   // a hit next to a cage opens it (with a key)
            World3.HitNear(transform.position);   // singing crystals of the Masters' riddle
            World4.HitNear(transform.position, !Grounded);   // snowmen, ice cubes (a jump attack breaks them), the chimney
            var id = look.Of("weapon");
            int set = Catalog.SetOf(id), i = Catalog.Index(id);
            int kind = set >= 0 ? 100 + set : i;
            var origin = transform.position + Vector3.up * 0.8f * Model.SizeScale + transform.forward * 0.4f;
            bool ranged = kind == 0 || kind == 2 || kind == 4 || kind == 5 || kind == 101;
            if (string.IsNullOrEmpty(id))
            {
                AudioFx.Play("punch");
                Hit(origin + transform.forward * 0.4f, 0.8f);   // a punch
                return;
            }
            if (ranged)
            {
                AudioFx.Play(kind == 0 ? "blaster" : kind == 5 ? "swing" : "magic");
                Projectile.Fire(kind, origin, transform.forward, transform);
            }
            else
            {
                AudioFx.Play(kind == 3 || kind == 103 || kind == 104 ? "saber" : "swing");
                Hit(origin + transform.forward * 0.6f, 1.1f);
                Effects.Emit(kind == 3 ? "elec" : kind == 104 ? "magic" : "dust", origin + transform.forward * 0.7f, 6, 0.4f);
            }
        }

        public static bool Hit(Vector3 at, float r)
        {
            bool any = false;
            var done = new HashSet<IHittable>();
            foreach (var c in Physics.OverlapSphere(at, r, ~0, QueryTriggerInteraction.Collide))
            {
                var b = c.GetComponentInParent<Breakable>();
                if (b != null) { b.Break(); any = true; }
                var e = c.GetComponentInParent<Enemy>();
                if (e != null && !e.Dead) { e.Die(); any = true; }
                var h = c.GetComponentInParent<IHittable>();
                if (h != null && done.Add(h) && h.OnHit(at)) any = true;
            }
            return any;
        }

        // ---------------- camera ----------------

        /// <summary>Wardrobe: finds the nearest wall around the chubrik; the camera will look at it, he stands with his back to it.</summary>
        public void StartWardrobeView()
        {
            var chest = transform.position + Vector3.up * 0.9f * Model.SizeScale;
            float best = 999; Vector3 bestDir = Vector3.zero;
            for (int k = 0; k < 32; k++)
            {
                var d = Quaternion.Euler(0, k * 11.25f, 0) * Vector3.forward;
                if (!Physics.Raycast(chest, d, out var hit, 16f, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.5f || hit.distance < 0.7f) continue;
                // the camera stands on the other side: it needs some room there
                float room = Physics.SphereCast(chest, 0.25f, -d, out var back, 4f, Layers.SolidMask, QueryTriggerInteraction.Ignore) ? back.distance : 4f;
                if (room < 1.6f) continue;
                if (hit.distance < best) { best = hit.distance; bestDir = d; }
            }
            WardrobeWall = best < 999;
            var toWall = WardrobeWall ? bestDir : Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            WardrobeDir = -toWall;
            WardrobeSpin = 0;
            WardrobeView = true;
            yaw = Quaternion.LookRotation(toWall).eulerAngles.y;
            pitch = 10;
            SetDive(0); EndLeap(); plunge = 0; Anim.plunge = false;
        }

        public void StopWardrobeView()
        {
            if (!WardrobeView) return;
            WardrobeView = false; ShowWalk = false;
            transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(WardrobeDir).eulerAngles.y + WardrobeSpin, 0);
        }

        void WardrobeCamera()
        {
            float s = Model.SizeScale;
            float h, dist;
            switch (WardrobeZoom)
            {
                case "head": h = 1.15f; dist = 2.3f; break;
                case "feet": h = 0.45f; dist = 2.1f; break;
                case "back": h = 0.85f; dist = 2.7f; break;
                case "hand": h = 0.7f; dist = 2.2f; break;
                default: h = 0.8f; dist = 2.7f; break;
            }
            h *= s; dist *= Mathf.Clamp(s, 0.7f, 1.8f);
            var target = transform.position + Vector3.up * h;
            var dir = (WardrobeDir + Vector3.up * 0.18f).normalized;
            if (Physics.SphereCast(target, 0.2f, dir, out var hit, dist, Layers.SolidMask, QueryTriggerInteraction.Ignore)) dist = Mathf.Max(1.0f, hit.distance - 0.1f);
            var rot = Quaternion.LookRotation(-dir);
            // shift the camera sideways so the chubrik stands in the middle of the free space between the panels
            float halfW = Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * Cam.aspect * dist;
            var pos = target + dir * dist + rot * Vector3.right * ((0.5f - WardrobeCx) * 2f * halfW);
            if (!wCamInit) { wCamPos = Cam.transform.position; wCamRot = Cam.transform.rotation; wCamInit = true; }
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f);
            wCamPos = Vector3.Lerp(wCamPos, pos, k); wCamRot = Quaternion.Slerp(wCamRot, rot, k);
            Cam.transform.SetPositionAndRotation(wCamPos, wCamRot);
        }

        void LateUpdate()
        {
            if (Cam == null) return;
            CameraUpdate();
            Water.CameraFx(Cam);   // 22.11 under the water surface the screen turns light blue
        }

        void CameraUpdate()
        {
            if (MenuOrbit)
            {
                orbitT += Time.deltaTime * 0.08f;
                var c = new Vector3(0, 4, 6);
                Cam.transform.position = c + new Vector3(Mathf.Sin(orbitT) * 30, 9 + Mathf.Sin(orbitT * 0.7f) * 2, Mathf.Cos(orbitT) * 30);
                Cam.transform.rotation = Quaternion.LookRotation(c - Cam.transform.position);
                return;
            }
            if (WardrobeView) { WardrobeCamera(); return; }
            if (FootballFans.CameraUpdate(Cam)) return;   // 19.11 watching someone's football
            wCamInit = false;
            float s = Model.SizeScale;
            // 22.11 diving: the camera follows him under the water — lower, closer, looking along
            camUnderK = Mathf.MoveTowards(camUnderK, swim == 2 ? 1 : 0, Time.deltaTime * 2.5f);
            var targetPos = transform.position + Vector3.up * Mathf.Lerp(1.15f * s + 0.2f, 0.45f * s, camUnderK);
            var rot = Quaternion.Euler(Mathf.Lerp(pitch, Mathf.Min(pitch, 6f), camUnderK), yaw, 0);
            var dir = rot * Vector3.back;
            float dist = Mathf.Lerp(camDist, 3.6f * Mathf.Clamp(s, 0.8f, 1.5f), camUnderK);
            if (Physics.SphereCast(targetPos, 0.25f, dir, out var hit, dist, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(1.2f, hit.distance - 0.1f);
            Cam.transform.position = targetPos + dir * dist;
            Cam.transform.rotation = rot;
        }
    }
}
