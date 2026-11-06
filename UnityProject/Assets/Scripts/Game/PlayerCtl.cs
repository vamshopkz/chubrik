using UnityEngine;

namespace Chubrik
{
    /// <summary>
    /// The player: third-person platformer controller (run, jump, double jump, glide with wings, jetpack boost),
    /// orbit camera with collision, moving-platform carry, bounce pads, weapon attacks, respawn when falling into water.
    /// </summary>
    public class PlayerCtl : MonoBehaviour
    {
        public static PlayerCtl I;
        public Camera Cam;
        public ChubrikModel Model;
        public ChubrikAnim Anim;
        public CharacterController CC;
        Pet pet;
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
            shadow.Set(Mathf.Max(0, look.Idx("shadow")));
            shadow.size = Model.SizeScale;
            float s = Model.SizeScale;
            CC.radius = 0.38f * Mathf.Clamp(s, 0.6f, 1.6f); CC.height = 1.45f * s; CC.center = new Vector3(0, 0.74f * s, 0);
            camDist = 6.5f * Mathf.Clamp(s, 0.8f, 1.5f);
            rends = Model.GetComponentsInChildren<Renderer>(true);
            Hearts = Mathf.Clamp(Hearts, 1, Save.P.maxHearts);
        }

        public void Teleport(Vector3 pos, float faceYaw = float.NaN)
        {
            CC.enabled = false;
            transform.position = pos;
            CC.enabled = true;
            vel = Vector3.zero;
            ground = null;
            if (!float.IsNaN(faceYaw)) { yaw = faceYaw; transform.rotation = Quaternion.Euler(0, faceYaw, 0); }
        }

        public void QueueJump() { jumpQueued = true; }

        public void Emote()
        {
            if (Anim.emoting) Anim.StopEmote(); else Anim.PlayEmote();
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (MenuOrbit) { vel = Vector3.zero; Anim.speed01 = 0; Anim.grounded = true; return; }

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
                if (Input.GetKeyDown(KeyCode.Q)) Emote();
                if (Input.GetKeyDown(KeyCode.E)) { if (!Zones.Interact()) Attack(); }
                if (Cursor.lockState == CursorLockMode.Locked)
                    LookDelta += new Vector2(Input.GetAxisRaw("Mouse X"), -Input.GetAxisRaw("Mouse Y")) * 2.2f * Save.P.sens;
                else if (Input.GetMouseButton(1))
                    LookDelta += new Vector2(Input.GetAxisRaw("Mouse X"), -Input.GetAxisRaw("Mouse Y")) * 2.2f * Save.P.sens;
            }
            else { mv = Vector2.zero; JumpHeld = false; }
            if (kbAttack) Attack();
            yaw += LookDelta.x * 0.9f;
            pitch = Mathf.Clamp(pitch + LookDelta.y * 0.6f, -10f, 65f);
            LookDelta = Vector2.zero;

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
            var hv = new Vector3(vel.x, 0, vel.z);
            var target = wish * speed;
            float accel = Grounded ? 40f : 16f;
            hv = Vector3.MoveTowards(hv, target, accel * dt);
            if (wish.sqrMagnitude > 0.01f)
            {
                var face = Quaternion.LookRotation(wish);
                if (look != null && look.Idx("walk") == 6 && Grounded) face = Quaternion.LookRotation(-wish);   // moonwalk: backwards
                transform.rotation = Quaternion.Slerp(transform.rotation, face, dt * 14);
                idle = 0;
                if (Anim.emoting) Anim.StopEmote();
            }

            // ---- vertical ----
            bool wantJump = jumpQueued || kbJump;
            jumpQueued = false;
            if (Grounded) { coyote = 0.12f; airJumps = Model.HasJet ? 2 : 1; }
            else coyote -= dt;
            if (wantJump && !Frozen && !Menus.AnyOpen)
            {
                if (coyote > 0) DoJump(11f, false);
                else if (airJumps > 0) { airJumps--; DoJump(9.5f, true); }
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
            if (Grounded && vel.y < 0) vel.y = -3f;
            vel.x = hv.x; vel.z = hv.z;

            bool wasGrounded = Grounded;
            float vyBefore = vel.y;
            ground = null;
            var flags = CC.Move(vel * dt);
            Grounded = (flags & CollisionFlags.Below) != 0;
            if ((flags & CollisionFlags.Above) != 0 && vel.y > 0) vel.y = 0;
            if (Grounded && ground != null) { groundLocal = ground.InverseTransformPoint(transform.position); groundRot = ground.rotation; }
            if (!wasGrounded && Grounded)
            {
                float impact = Mathf.InverseLerp(-4, -22, vyBefore);
                Anim.Land(impact);
                if (vyBefore < -6) { AudioFx.Land(Mathf.Max(0, look.Idx("lsnd"))); Effects.Emit("dust", transform.position, 6, 0.3f); }
            }
            if (wasGrounded && !Grounded) fallStart = transform.position.y;

            // safe spot for respawns
            if (Grounded && ground != null && ground.GetComponent<Mover>() == null)
            {
                safeT += dt;
                if (safeT > 0.5f) { safeT = 0; SafePos = transform.position + Vector3.up * 0.2f; }
            }
            if (transform.position.y < WaterY - 0.5f) { if (Worlds.Level) FellOff(); else FellInWater(); }
            if (invuln > 0)
            {
                invuln -= dt;
                bool show = invuln <= 0 || ((int)(invuln * 12)) % 2 == 0;
                if (rends != null) foreach (var r in rends) if (r) r.enabled = show;
            }

            // ---- animation ----
            Anim.speed01 = Mathf.Clamp01(new Vector3(vel.x, 0, vel.z).magnitude / RunSpeed);
            Anim.grounded = Grounded;
            Anim.vy = vel.y;
            Anim.gliding = gliding;
            if (gliding && Random.value < 0.3f) Effects.Emit("comet", transform.position + Vector3.up * 0.5f, 1, 0.3f);
            if (Grounded && Anim.speed01 < 0.05f && !Frozen)
            {
                idle += dt;
                if (idle > 6f && !Anim.emoting) { Anim.PlayEmote(); idle = -4f; }
            }
            attackCd -= dt;
        }

        void DoJump(float v, bool air)
        {
            vel.y = v;
            Grounded = false; coyote = 0;
            Jumps++;
            AudioFx.Jump(Mathf.Max(0, look.Idx("jsnd")), air);
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

        /// <summary>Bounce pads, springs: launch upwards.</summary>
        public void Launch(float v)
        {
            vel.y = v;
            Grounded = false; coyote = 0; airJumps = Model.HasJet ? 2 : 1;
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
            Effects.Damage(Mathf.Max(0, look.Idx("dmg")), Model);
            invuln = 0;
            LoseHeart();
            if (Worlds.Current == 3) Effects.Emit("fire", transform.position, 40, 0.5f, Vector3.up * 4);
            if (Hearts > 0) { Teleport(SafePos); invuln = 1.5f; Hud.Toast(Worlds.Current == 3 ? "Ой, лава! −1 сердечко" : "Упал с острова! −1 сердечко", UIKit.Red); }
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
            var foot = (left ? Model.FootL : Model.FootR);
            var p = foot ? foot.position : transform.position;
            p.y = transform.position.y + 0.03f;
            int k = Effects.StepKind(look);
            if (k >= 0 || Random.value < 0.3f) Effects.Step(k, p);
            AudioFx.Step(look.Idx("shoes"), look.Idx("walk") == 4);
        }

        // ---------------- weapons ----------------

        public void Attack()
        {
            if (Frozen || attackCd > 0) return;
            attackCd = 0.4f;
            Anim.Attack();
            Rescue.HitNear(transform.position);   // a hit next to a cage opens it (with a key)
            World3.HitNear(transform.position);   // singing crystals of the Masters' riddle
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
            foreach (var c in Physics.OverlapSphere(at, r, ~0, QueryTriggerInteraction.Collide))
            {
                var b = c.GetComponentInParent<Breakable>();
                if (b != null) { b.Break(); any = true; }
                var e = c.GetComponentInParent<Enemy>();
                if (e != null && !e.Dead) { e.Die(); any = true; }
            }
            return any;
        }

        // ---------------- camera ----------------

        void LateUpdate()
        {
            if (Cam == null) return;
            if (MenuOrbit)
            {
                orbitT += Time.deltaTime * 0.08f;
                var c = new Vector3(0, 4, 6);
                Cam.transform.position = c + new Vector3(Mathf.Sin(orbitT) * 30, 9 + Mathf.Sin(orbitT * 0.7f) * 2, Mathf.Cos(orbitT) * 30);
                Cam.transform.rotation = Quaternion.LookRotation(c - Cam.transform.position);
                return;
            }
            float s = Model.SizeScale;
            var targetPos = transform.position + Vector3.up * (1.15f * s + 0.2f);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            var dir = rot * Vector3.back;
            float dist = camDist;
            if (Physics.SphereCast(targetPos, 0.25f, dir, out var hit, dist, Layers.SolidMask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(1.2f, hit.distance - 0.1f);
            Cam.transform.position = targetPos + dir * dist;
            Cam.transform.rotation = rot;
        }
    }
}
