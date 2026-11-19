using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SugarRush {
    [RequireComponent(typeof(Rigidbody2D))]
    public class WitchPawn : AbstractPawn {
        [Header("Attack")]
        [Tooltip("Seconds the witch winds up before lunging. She can still move (slowed by Windup Speed Multiplier) and aim with the stick during it.")]
        [SerializeField] private float attackWindup = 0.25f;
        [Tooltip("Running speed during the windup, as a fraction of normal speed (0.4 = 40%).")]
        [SerializeField] private float windupSpeedMultiplier = 0.4f;
        [Tooltip("Speed of the lunge, in units per second.")]
        [SerializeField] private float lungeSpeed = 18f;
        [Tooltip("Seconds the lunge lasts. The hitbox is active for this whole time.")]
        [SerializeField] private float lungeDuration = 0.15f;
        [Tooltip("Seconds from the start of one attack until the next one is allowed.")]
        [SerializeField] private float attackCooldown = 1f;
        [Tooltip("Hitbox size: X is how far it reaches forward, Y is how wide it is. Select the witch to see it in the Scene view.")]
        [SerializeField] private Vector2 hitboxSize = new(0.6f, 0.7f);
        [Tooltip("How far in front of the centre of the witch's collider the hitbox's centre sits.")]
        [SerializeField] private float hitboxDistance = 0.5f;
        [Tooltip("Seconds into the Attack clips where the strike pose starts. The clip is sped up or slowed down so that pose appears just as the lunge starts. 0 = play the clip at normal speed.")]
        [SerializeField] private float attackAnimationStrikeTime = 1f / 6f;

        [Header("On Whiff")]
        [Tooltip("Running speed after a lunge that hits nothing, as a fraction of normal speed (0.5 = half speed).")]
        [SerializeField] private float whiffSlowMultiplier = 0.5f;
        [Tooltip("Seconds the whiff slow lasts.")]
        [SerializeField] private float whiffSlowDuration = 1f;

        [Header("On Hit")]
        [Tooltip("Speed a hit Hansel is pushed at along the attack direction. The push fades out (Knockback Deceleration on the Hansel) and the Hansel keeps control throughout.")]
        [SerializeField] private float knockbackSpeed = 14f;
        [Tooltip("A hit Hansel's running speed, as a multiple of normal speed (1.5 = 50% faster), so it can get away.")]
        [SerializeField] private float hanselSpeedBuffMultiplier = 1.5f;
        [Tooltip("Seconds the Hansel's speed buff lasts.")]
        [SerializeField] private float hanselSpeedBuffDuration = 2f;
        [Tooltip("Score taken from each Hansel the attack lands on and given to this witch. A Hansel can't lose more than they have.")]
        [SerializeField, Min(0)] private int scoreStolenPerHit = 2;
        [Tooltip("Fewest points this witch gets for each Hansel the attack lands on. If the Hansel had less than this to steal, " +
                 "the witch is given the difference on top (the Hansel doesn't lose it).")]
        [SerializeField, Min(0)] private int minScorePerHit = 1;

        [Header("When Outnumbered")]
        [Tooltip("While there are more Hansels than witches in the game, Score Stolen Per Hit is multiplied by this (2 = double).")]
        [SerializeField, Min(1)] private int outnumberedStealMultiplier = 2;
        [Tooltip("While there are more Hansels than witches in the game, the fewest points this witch gets for each Hansel the attack lands on. " +
                 "Used in place of Min Score Per Hit if it's higher.")]
        [SerializeField, Min(0)] private int outnumberedMinScorePerHit = 2;

        [Tooltip("Called once for each Hansel an attack lands on.")]
        public UnityEvent<HanselPawn> onAttackLanded = new();

        [Header("Attack Effects")]
        [Tooltip("Shine emitted once when the windup starts. Tinted with the player's color. Auto-found (a child named Shine...) if left empty.")]
        [SerializeField] private ParticleSystem windupShineParticles;
        [Tooltip("Force emitted once when the windup starts. Tinted with the player's color. Auto-found (a child named Force...) if left empty.")]
        [SerializeField] private ParticleSystem windupForceParticles;
        [Tooltip("One slash emitted when the lunge starts, turned to the strike direction. Set it up in the scene for a strike in any one direction " +
                 "(it's turned from there, sprite included). Tinted with the player's color. Auto-found (a child named Swing...) if left empty.")]
        [SerializeField] private ParticleSystem swingParticles;
        [Tooltip("Specs emitted when the lunge starts, sprayed along the strike direction. Set up like Swing Particles. " +
                 "Tinted with the player's color. Auto-found (a child named Specs...) if left empty.")]
        [SerializeField] private ParticleSystem swingSpecsParticles;
        [Tooltip("How many specs each lunge emits.")]
        [SerializeField, Min(0)] private int swingSpecsCount = 12;

        [Header("Attack Sounds")]
        [Tooltip("Played when the windup starts.")]
        [SerializeField] private AudioClip windupSound;
        [Tooltip("Played once when the attack lands on one or more Hansels.")]
        [SerializeField] private AudioClip hitSound;

        private SugarRushPawnManager _pawnManager;

        // How Swing and Specs are set up in the scene. Each lunge turns them from here to the strike direction.
        private Quaternion _swingRestRotation;
        private Quaternion _specsRestRotation;

        private bool _isAttacking;
        private bool _hasLunged; // whether this attack's lunge has started (and played its swing)
        private float _attackStartTime;
        private float _nextAttackTime;
        private Vector2 _attackDirection;
        private Collider2D _collider;
        private readonly List<Collider2D> _hitboxResults = new();
        private readonly List<HanselPawn> _hitHansels = new();

        // Animator states playing the Witch_Red_Attack_ clips. Right reuses Left, flipped.
        private static readonly int AttackDown = Animator.StringToHash("Attack_Down");
        private static readonly int AttackLeft = Animator.StringToHash("Attack_Left");
        private static readonly int AttackUp = Animator.StringToHash("Attack_Up");

        /// True from pressing Attack until the lunge ends (or a hit lands).
        public bool IsAttacking => _isAttacking;
        /// True during the first part of the attack, where the witch moves slowly before lunging.
        private bool IsWindingUp => _isAttacking && Time.time < _attackStartTime + attackWindup;
        /// True during the part of the attack where the lunge carries the witch and the hitbox is active.
        private bool IsLunging => _isAttacking && Time.time >= _attackStartTime + attackWindup;

        // Witches always trail their movement particles, even while standing still
        protected override bool ShouldEmitMovementParticles => true;
        // No turning, dashing or starting another attack mid-attack (or during the opening)
        protected override bool CanAct => !_isAttacking && base.CanAct;
        protected override float MoveSpeed => IsWindingUp ? base.MoveSpeed * windupSpeedMultiplier : base.MoveSpeed;

        // The attack animation plays for the whole attack, windup and lunge
        protected override int GetAnimationState() {
            if (!_isAttacking) return base.GetAnimationState();
            return _facing switch {
                Facing.Up => AttackUp,
                Facing.Left or Facing.Right => AttackLeft,
                _ => AttackDown,
            };
        }

        // Turning mid-attack (e.g. Attack_Down -> Attack_Up while aiming) carries on from the same point in the clip,
        // so the strike pose still lands on the lunge
        protected override float GetAnimationStartTime(int previousState, int newState) {
            if (animator == null || !IsAttackState(previousState) || !IsAttackState(newState)) return base.GetAnimationStartTime(previousState, newState);
            return Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
        }

        private static bool IsAttackState(int state) => state == AttackDown || state == AttackLeft || state == AttackUp;

        protected override void Update() {
            AimWindup();

            // Stretch the attack clip so its strike pose lands on the lunge; everything else plays at normal speed
            if (animator != null) {
                bool syncAttack = _isAttacking && attackAnimationStrikeTime > 0f && attackWindup > 0f;
                animator.speed = syncAttack ? attackAnimationStrikeTime / attackWindup : 1f;
            }
            base.Update();
        }

        // During the windup the stick still aims the attack (and turns the witch). Once the lunge launches, the direction is locked.
        private void AimWindup() {
            if (!IsWindingUp || !IsMoving) return;
            _attackDirection = _moveInput.normalized;
            _facing = GetFacing(_attackDirection);
        }

        // Awake rather than Start: PawnManager binds players in its Start, which may run before this one's
        protected override void Awake() {
            base.Awake();
            _collider = GetComponent<Collider2D>();

            if (windupShineParticles == null) windupShineParticles = FindChildParticles("Shine");
            if (windupForceParticles == null) windupForceParticles = FindChildParticles("Force");
            if (swingParticles == null) swingParticles = FindChildParticles("Swing");
            if (swingSpecsParticles == null) swingSpecsParticles = FindChildParticles("Specs");
            SetUpParticles(windupShineParticles);
            SetUpParticles(windupForceParticles);
            SetUpParticles(swingParticles);
            SetUpParticles(swingSpecsParticles);
            if (swingParticles != null) _swingRestRotation = swingParticles.transform.localRotation;
            if (swingSpecsParticles != null) _specsRestRotation = swingSpecsParticles.transform.localRotation;

            onPlayerIndexChanged.AddListener(ApplyParticleColor);
            ApplyParticleColor(playerIndex);
        }

        // Tints the trail and the attack effects with the player's color. Only newly emitted particles pick it up; Color over Lifetime still fades them out.
        private void ApplyParticleColor(int index) {
            Color color = PlayerColors.Get(index);
            SetStartColor(movementParticles, color);
            SetStartColor(windupShineParticles, color);
            SetStartColor(windupForceParticles, color);
            SetStartColor(swingParticles, color);
            SetStartColor(swingSpecsParticles, color);
        }

        private static void SetStartColor(ParticleSystem particles, Color color) {
            if (particles == null) return;
            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {
            base.OnActionPressed(context);
            if (!isPawnInputEnabled || IsFrozen) return;

            // Attack
            if (context.action.name == PawnAction.ButtonB) Attack();
        }

        /// <summary>
        /// Winds up (the witch can still move, slowly), then lunges forward with a hitbox in front.
        /// Aims where the stick is held, or where the witch is facing if it isn't held. The stick keeps aiming through the windup;
        /// the direction is locked once the lunge launches.
        /// Does nothing while already attacking or on cooldown.
        /// </summary>
        protected virtual void Attack() {
            if (!CanAct || Time.time < _nextAttackTime) return;

            _attackDirection = IsMoving ? _moveInput.normalized : FacingDirection;
            _facing = GetFacing(_attackDirection);
            _isAttacking = true;
            _hasLunged = false;
            _attackStartTime = Time.time;
            _nextAttackTime = Time.time + attackCooldown;
            RestartAnimation(); // always start the attack clip from its first frame

            EmitParticles(windupShineParticles, 1);
            EmitParticles(windupForceParticles, 1);
            PlaySound(windupSound);
        }

        protected override void FixedUpdate() {
            if (_isAttacking) {
                float elapsed = Time.time - _attackStartTime;
                if (elapsed >= attackWindup + lungeDuration) {
                    // The lunge ended without hitting anything: hand control back, but slowed
                    _isAttacking = false;
                    ApplySpeedMultiplier(whiffSlowMultiplier, whiffSlowDuration);
                }
                else if (elapsed >= attackWindup) {
                    // First step of the lunge: the direction is locked now, so the swing goes where the strike goes
                    if (!_hasLunged) {
                        _hasLunged = true;
                        PlaySwingEffects();
                    }
                    _rigidbody.linearVelocity = _attackDirection * lungeSpeed;
                    CheckHitbox();
                    return;
                }
                // Otherwise still winding up: move as normal, slowed by MoveSpeed
            }

            base.FixedUpdate();
        }

        // One slash and a spray of specs along the strike
        private void PlaySwingEffects() {
            if (swingParticles != null) {
                float turn = AimParticles(swingParticles, _swingRestRotation, _attackDirection);
                // The slash sprite faces the camera, so turning the emitter doesn't turn the sprite: roll it by the same amount,
                // on top of the system's own Start Rotation. Particle rotation is in degrees and goes clockwise, hence the minus.
                float restRoll = swingParticles.main.startRotation.constant * Mathf.Rad2Deg;
                swingParticles.Emit(new ParticleSystem.EmitParams { rotation = restRoll - turn }, 1);
            }

            if (swingSpecsParticles != null) {
                AimParticles(swingSpecsParticles, _specsRestRotation, _attackDirection);
                EmitParticles(swingSpecsParticles, swingSpecsCount);
            }
        }

        /// <summary>
        /// Turns particles around the 2D plane, from how they're set up in the scene (restRotation), so they fire along direction.
        /// </summary>
        /// <returns>How far they were turned, in degrees anticlockwise.</returns>
        private static float AimParticles(ParticleSystem particles, Quaternion restRotation, Vector2 direction) {
            float restAngle = GetAngle(restRotation * Vector3.forward); // shapes fire along the transform's forward
            float turn = Mathf.DeltaAngle(restAngle, GetAngle(direction));
            particles.transform.localRotation = Quaternion.AngleAxis(turn, Vector3.forward) * restRotation;
            return turn;
        }

        private static void EmitParticles(ParticleSystem particles, int count) {
            if (particles != null && count > 0) particles.Emit(count);
        }

        private void CheckHitbox() {
            Physics2D.OverlapBox(GetHitboxCenter(_attackDirection), hitboxSize, GetAngle(_attackDirection),
                ContactFilter2D.noFilter, _hitboxResults);

            _hitHansels.Clear();
            foreach (Collider2D hit in _hitboxResults) {
                HanselPawn hansel = hit.GetComponentInParent<HanselPawn>();
                if (hansel == null || hansel.IsInOpening) continue; // still walking in, can't be hit yet
                if (!_hitHansels.Contains(hansel)) _hitHansels.Add(hansel);
            }

            if (_hitHansels.Count > 0) LandHit();
        }

        // Pushes every Hansel in the hitbox along the attack, gives them a burst of speed to get away, and steals some of their score.
        // Landing a hit ends the attack straight away, with no whiff slow.
        private void LandHit() {
            PlaySound(hitSound); // once, however many Hansels it caught
            bool isOutnumbered = AreWitchesOutnumbered();
            foreach (HanselPawn hansel in _hitHansels) {
                hansel.Knockback(_attackDirection * knockbackSpeed);
                hansel.PlayHurtEffect(_attackDirection);
                hansel.ApplySpeedMultiplier(hanselSpeedBuffMultiplier, hanselSpeedBuffDuration);
                StealScoreFrom(hansel, isOutnumbered);
                onAttackLanded.Invoke(hansel);
            }

            _isAttacking = false;
        }

        private void StealScoreFrom(HanselPawn hansel, bool isOutnumbered) {
            if (_pawnManager == null) _pawnManager = FindAnyObjectByType<SugarRushPawnManager>();
            if (_pawnManager == null) {
                Debug.LogWarning($"{nameof(WitchPawn)}: no {nameof(SugarRushPawnManager)} in the scene, so no score was stolen.", this);
                return;
            }

            // Outnumbered witches steal more, and are guaranteed more per hit
            int toSteal = isOutnumbered ? scoreStolenPerHit * outnumberedStealMultiplier : scoreStolenPerHit;
            int minScore = isOutnumbered ? Mathf.Max(minScorePerHit, outnumberedMinScorePerHit) : minScorePerHit;

            int stolen = _pawnManager.StealScore(hansel.playerIndex, playerIndex, toSteal);

            // A Hansel with little or nothing left still pays out at least the minimum
            if (stolen < minScore) _pawnManager.AddScore(playerIndex, minScore - stolen);
        }

        /// <returns>True if more players are playing Hansels than witches right now. Pawns without a player don't count.</returns>
        private static bool AreWitchesOutnumbered() {
            int witches = 0;
            int hansels = 0;
            foreach (AbstractPawn pawn in FindObjectsByType<AbstractPawn>(FindObjectsInactive.Include)) {
                if (!pawn.IsBound) continue;
                if (pawn is WitchPawn) witches++;
                else if (pawn is HanselPawn) hansels++;
            }
            return hansels > witches;
        }

        protected override void ResetActionState() {
            base.ResetActionState();
            _isAttacking = false;
            _nextAttackTime = 0f;
        }

        // The hitbox sits in front of the witch's collider (at her feet), not her sprite's centre
        private Vector2 GetHitboxCenter(Vector2 direction) {
            Collider2D col = _collider != null ? _collider : GetComponent<Collider2D>();
            Vector2 position = Application.isPlaying && _rigidbody != null ? _rigidbody.position : (Vector2)transform.position;
            Vector2 origin = col != null ? position + (Vector2)transform.TransformVector(col.offset) : position;
            return origin + direction * hitboxDistance;
        }

        private static float GetAngle(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Shows the hitbox when the witch is selected: yellow where it would be, red while it's active
        private void OnDrawGizmosSelected() {
            Vector2 direction = _isAttacking ? _attackDirection : FacingDirection;
            Gizmos.color = IsLunging ? Color.red : Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(GetHitboxCenter(direction), Quaternion.Euler(0f, 0f, GetAngle(direction)), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
        }
    }
}
