using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SugarRush {
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class AbstractPawn : Pawn {
        [Header("Movement")]
        [SerializeField] protected float speed = 8f;
        protected Vector2 _moveInput = Vector2.zero;
        protected Rigidbody2D _rigidbody;
        public static bool isPawnInputEnabled = true;

        [Header("Dash")]
        [Tooltip("Impulse applied along the movement direction when dashing. With a mass of 1 this is the speed added on top of running.")]
        [SerializeField] private float _dashForce = 16f;
        [Tooltip("Seconds the dash impulse carries the pawn before normal movement takes over again.")]
        [SerializeField] private float _dashDuration = 0.15f;
        [Tooltip("Seconds from the start of one dash until the next one is allowed.")]
        [SerializeField] private float _dashCooldown = 0.75f;
        [Tooltip("How fast the stick can turn a dash while it carries the pawn, in degrees per second. The dash keeps its speed as it turns. " +
                 "0 = dashes go straight. Something very large (e.g. 10000) makes the dash follow the stick instantly.")]
        [SerializeField, Min(0f)] private float _dashTurnSpeed = 720f;
        [Tooltip("Played when a dash starts.")]
        [SerializeField] private AudioClip dashSound;
        private float _dashEndTime;
        private float _nextDashTime;

        [Header("Knockback")]
        [Tooltip("How quickly a knockback push fades out, in units per second per second. The push is added on top of normal movement, so the pawn keeps control while it's shoved.")]
        [SerializeField] private float knockbackDeceleration = 40f;
        private Vector2 _knockbackVelocity;

        // Timed speed buff or slow, see ApplySpeedMultiplier
        private float _speedMultiplier = 1f;
        private float _speedMultiplierEndTime;

        [Header("Effects")]
        [Tooltip("Dust that plays while moving or dashing (witches emit it all the time). Auto-found (a child named MovementParticles...) if left empty.")]
        [SerializeField] protected ParticleSystem movementParticles;
        [Tooltip("Particles' X rotation while moving right. Diagonals blend between the two nearest directions.")]
        [SerializeField] private float particleRotationRight = 197.3f;
        [Tooltip("Particles' X rotation while moving up.")]
        [SerializeField] private float particleRotationUp = 90f;
        [Tooltip("Particles' X rotation while moving left.")]
        [SerializeField] private float particleRotationLeft = 341.4f;
        [Tooltip("Particles' X rotation while moving down.")]
        [SerializeField] private float particleRotationDown = -90f;
        // The X angles above assume the particles' Y rotation is 90 and Z is 0, which turns the X angle into a swing around the pawn in 2D.
        // Y and Z are set here rather than read back from the transform, because Unity can read the same rotation back as different angles.
        private const float ParticleRotationY = 90f;
        private const float ParticleRotationZ = 0f;
        [Tooltip("Dirt kicked up all at once when a dash starts, sprayed out behind the dash's starting direction (aimed with the Particle Rotation angles above). " +
                 "Auto-found (a child named Dirt...) if left empty.")]
        [SerializeField] protected ParticleSystem dashDirtParticles;
        [Tooltip("How many dirt particles each dash kicks up.")]
        [SerializeField, Min(0)] private int dashDirtCount = 12;
        [Tooltip("Speed lines that play for as long as a dash carries the pawn. Auto-found (a child named SpeedLines...) if left empty.")]
        [SerializeField] protected ParticleSystem dashSpeedLines;

        [Header("Sound")]
        [Tooltip("Plays this pawn's sounds (see PlaySound). Auto-found on this object, or added, if left empty.")]
        [SerializeField] protected AudioSource audioSource;
        private AudioSource _pitchedAudioSource; // see PlaySound

        [Header("Animation")]
        [Tooltip("Animator whose controller has Idle_Down/Left/Up and Run_Down/Left/Up states. Auto-found if left empty.")]
        [SerializeField] protected Animator animator;
        [Tooltip("Sprite that gets flipped to face right. Auto-found if left empty.")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [Tooltip("Move input smaller than this counts as standing still.")]
        [SerializeField] private float moveDeadzone = 0.1f;

        [Header("Opening")]
        [Tooltip("When a player is added, the pawn appears this far to one side of its spot in the scene and walks in to it. " +
                 "Players 2 and 3 come in from the left, Players 1 and 4 from the right.")]
        [SerializeField, Min(0f)] private float openingWalkDistance = 3f;
        [Tooltip("Seconds the pawn takes to walk in to its spot.")]
        [SerializeField, Min(0f)] private float openingWalkDuration = 1f;
        [Tooltip("Seconds from the pawn appearing until its player gets control. Until then it can't be hit, and it passes through everything. " +
                 "Anything past Opening Walk Duration is spent standing still on its spot.")]
        [SerializeField, Min(0f)] private float openingDuration = 1.5f;
        private Vector3 _restingPosition; // the pawn's spot in the scene
        private Vector3 _openingStartPosition;
        private Vector2 _openingWalkDirection;
        private float _openingStartTime;
        private bool _isInOpening;
        private bool _wasBound;
        // The player's latest move input, kept through the opening so a stick already held takes effect when control starts
        private Vector2 _heldMoveInput;
        // Set by Freeze when the round ends
        private bool _isFrozen;

        protected enum Facing { Down, Left, Right, Up }
        protected Facing _facing = Facing.Down;
        private int _currentAnimState;

        
        // Stops the animation flickering between two directions diagonally
        private const float AxisSwitchBias = 0.1f;

        private static readonly int IdleDown = Animator.StringToHash("Idle_Down");
        private static readonly int IdleLeft = Animator.StringToHash("Idle_Left");
        private static readonly int IdleUp = Animator.StringToHash("Idle_Up");
        private static readonly int RunDown = Animator.StringToHash("Run_Down");
        private static readonly int RunLeft = Animator.StringToHash("Run_Left");
        private static readonly int RunUp = Animator.StringToHash("Run_Up");

        /// True when PawnManager has bound a player's input to this pawn.
        public bool IsBound => playerIndex >= 0;
        /// True while the pawn is walking in after its player was added, before the player has control. It can't be hit meanwhile.
        public bool IsInOpening => _isInOpening;
        /// True once the round is over (see Freeze): the pawn ignores its player, stands still, and can't be hit or pick anything up.
        public bool IsFrozen => _isFrozen;
        /// Where the pawn stands in play: during the opening, the spot it's walking to rather than where it is now.
        public Vector3 RestingPosition => _isInOpening ? _restingPosition : transform.position;

        /// <returns>The pawn's feet (the centre of its collider) at its RestingPosition, in world space,
        /// so spawners keep a walking-in pawn's spot clear rather than where it happens to be.</returns>
        public static Vector3 GetRestingFeet(Pawn pawn) {
            Vector3 position = pawn is AbstractPawn sugarPawn ? sugarPawn.RestingPosition : pawn.transform.position;
            Collider2D col = pawn.GetComponent<Collider2D>();
            return col != null ? position + pawn.transform.TransformVector(col.offset) : position;
        }
        /// True while the player is holding a direction past the deadzone.
        protected bool IsMoving => _moveInput.sqrMagnitude > moveDeadzone * moveDeadzone;
        /// True while a dash impulse is carrying the pawn.
        protected bool IsDashing => Time.time < _dashEndTime;
        /// True while a timed speed buff or slow is active.
        public bool HasSpeedMultiplier => Time.time < _speedMultiplierEndTime;
        /// Running speed right now, including any timed buff or slow. Override to change it in other situations (e.g. WitchPawn's attack windup).
        protected virtual float MoveSpeed => HasSpeedMultiplier ? speed * _speedMultiplier : speed;
        /// True when the pawn can turn to face its movement and start a dash or ability. Movement itself carries on regardless.
        /// False during the opening and once frozen. Override to lock these out at other times too (e.g. WitchPawn while it's attacking), keeping base.CanAct.
        protected virtual bool CanAct => !_isInOpening && !_isFrozen;
        /// Whether the movement particles should be emitting right now. Override to change when they play (WitchPawn always emits).
        protected virtual bool ShouldEmitMovementParticles => IsMoving || IsDashing;
        /// The direction the pawn is facing, as a unit vector.
        protected Vector2 FacingDirection => _facing switch {
            Facing.Up => Vector2.up,
            Facing.Left => Vector2.left,
            Facing.Right => Vector2.right,
            _ => Vector2.down,
        };

        // No gravity, and don't let collisions spin the sprite
        // Subclasses that need these must override them and call base.Reset() / base.Awake() / base.Start() / base.Update() / base.FixedUpdate()
        protected virtual void Reset() {
            ConfigureRigidbody(GetComponent<Rigidbody2D>());
        }
        protected virtual void Awake() {
            _rigidbody = GetComponent<Rigidbody2D>();
            ConfigureRigidbody(_rigidbody);
            _restingPosition = transform.position;

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (movementParticles == null) movementParticles = FindChildParticles("MovementParticles");
            if (dashDirtParticles == null) dashDirtParticles = FindChildParticles("Dirt");
            if (dashSpeedLines == null) dashSpeedLines = FindChildParticles("SpeedLines");
            SetUpParticles(movementParticles);
            SetUpParticles(dashDirtParticles);
            SetUpParticles(dashSpeedLines);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            onPlayerIndexChanged.AddListener(OnPlayerIndexChanged);
        }

        /// <summary>
        /// Plays a sound from this pawn, on top of anything it's already playing. volume is a fraction of the clip's
        /// normal loudness (0.2 = 20%). pitch is its playback speed (1 = normal; higher is faster and higher pitched).
        /// Does nothing if clip is empty or the pawn is hidden.
        /// </summary>
        public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f) {
            if (clip == null || audioSource == null || !audioSource.isActiveAndEnabled) return;
            if (Mathf.Approximately(pitch, 1f)) {
                audioSource.PlayOneShot(clip, volume);
                return;
            }

            // A source's pitch applies to everything it's playing, so pitched sounds get a second source of their own
            // rather than bending the pawn's other sounds. Pitched sounds that overlap each other share the latest pitch.
            if (_pitchedAudioSource == null) {
                _pitchedAudioSource = audioSource.gameObject.AddComponent<AudioSource>();
                _pitchedAudioSource.playOnAwake = false;
                _pitchedAudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                _pitchedAudioSource.spatialBlend = audioSource.spatialBlend;
            }
            _pitchedAudioSource.pitch = pitch;
            _pitchedAudioSource.PlayOneShot(clip, volume);
        }

        // PawnManager binds pawns in its Start, so by the first frame any pawn without a player gets hidden here
        protected virtual void Start() {
            HideIfUnbound();
        }

        // Unbound pawns are deactivated: invisible, no Update/physics/collisions, and no input since nothing is bound
        private void OnPlayerIndexChanged(int index) {
            _moveInput = Vector2.zero; // drop held input from the previous player
            _heldMoveInput = Vector2.zero;
            ResetActionState();
            // Show as soon as a player is bound. Hiding waits for Start/Update so it never runs while the pawn is being destroyed (Pawn unbinds itself in OnDestroy).
            if (index >= 0 && !gameObject.activeSelf) gameObject.SetActive(true);

            // A newly added player walks in; a player leaving mid-opening leaves the pawn on its spot, ready for the next one
            bool isBound = index >= 0;
            if (isBound && !_wasBound) BeginOpening();
            else if (!isBound && _isInOpening) EndOpening();
            _wasBound = isBound;
        }

        // Puts the pawn to one side of its spot and starts it walking in. Physics is off until control starts,
        // so it walks through walls and obstacles on the way, can't be hit and doesn't pick anything up.
        private void BeginOpening() {
            _isInOpening = true;
            _openingStartTime = Time.time;
            _openingWalkDirection = WalksInFromLeft ? Vector2.right : Vector2.left;
            _openingStartPosition = _restingPosition - (Vector3)(_openingWalkDirection * openingWalkDistance);
            _facing = _openingWalkDirection.x > 0f ? Facing.Right : Facing.Left;

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
            UpdateOpening();
        }

        // Players 2 and 3 stand on the left of the map, so they come in from the left. Everyone else comes in from the right.
        private bool WalksInFromLeft => playerIndex == 1 || playerIndex == 2;

        // Walks the pawn along its path, then hands control to the player once the opening is over
        private void UpdateOpening() {
            if (!_isInOpening) return;

            float elapsed = Time.time - _openingStartTime;
            if (elapsed >= openingDuration) {
                EndOpening();
                return;
            }

            float progress = openingWalkDuration > 0f ? Mathf.Clamp01(elapsed / openingWalkDuration) : 1f;
            transform.position = Vector3.Lerp(_openingStartPosition, _restingPosition, progress);
            _moveInput = progress < 1f ? _openingWalkDirection : Vector2.zero; // drives the run animation and dust, then idle
        }

        private void EndOpening() {
            _isInOpening = false;
            transform.position = _restingPosition;
            _rigidbody.simulated = !_isFrozen; // a player who joined after the round ended still can't be touched
            _rigidbody.position = _restingPosition;
            _rigidbody.linearVelocity = Vector2.zero;
            _moveInput = _isFrozen ? Vector2.zero : _heldMoveInput;
        }

        /// <summary>
        /// Ends the round for this pawn, for good: its player loses control, it stops where it is (idle), and its physics
        /// switches off so nothing can touch it. Witches can't hit it, it can't grab candy, and pawns pass through each other.
        /// Cancels any dash, knockback, speed buff/slow or attack in progress. RoundTimer calls this on every pawn when time runs out.
        /// </summary>
        public void Freeze() {
            _isFrozen = true;
            _moveInput = Vector2.zero;
            _heldMoveInput = Vector2.zero;
            ResetActionState();

            if (_rigidbody == null) return; // never woken up (inactive since the scene loaded), so there's nothing moving to stop
            if (_isInOpening) EndOpening(); // snap a pawn still walking in onto its spot
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.simulated = false;
        }

        /// Clears dashes, knockback, speed buffs/slows and cooldowns so a newly bound player starts fresh. Override to reset abilities too.
        protected virtual void ResetActionState() {
            _dashEndTime = 0f;
            _nextDashTime = 0f;
            _knockbackVelocity = Vector2.zero;
            _speedMultiplierEndTime = 0f;
        }

        /// <returns>True if the pawn was hidden because no player is bound to it.</returns>
        private bool HideIfUnbound() {
            if (IsBound) return false;
            gameObject.SetActive(false);
            return true;
        }
        private static void ConfigureRigidbody(Rigidbody2D rb) {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // Handle input
        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (!isPawnInputEnabled || _isFrozen) return;

            // Move. During the opening it's only remembered, for when the player gets control.
            if (context.action.name == PawnAction.Move) {
                _heldMoveInput = context.ReadValue<Vector2>();
                if (!_isInOpening) _moveInput = _heldMoveInput;
            }

            // Dash (CanAct keeps it locked during the opening)
            if (context.action.name == PawnAction.ButtonA) Dash();
        }

        /// <summary>
        /// A Default dash.
        /// Pushes the pawn in the direction of the input and puts it on a short cooldown.
        /// While the dash carries the pawn, the stick can steer it (see Dash Turn Speed).
        /// Does nothing while standing still (no direction to dash in), on cooldown, or when the pawn can't act (e.g. mid-ability).
        /// </summary>
        protected virtual void Dash() {
            if (!CanAct || !IsMoving || Time.time < _nextDashTime) return;

            Vector2 direction = _moveInput.normalized;
            _rigidbody.AddForce(direction * _dashForce, ForceMode2D.Impulse);
            _dashEndTime = Time.time + _dashDuration;
            _nextDashTime = Time.time + _dashCooldown;
            KickUpDashDirt(direction);
            PlaySound(dashSound);
        }

        // One burst of dirt behind the dash's starting direction. Steering the dash afterwards doesn't re-aim it.
        private void KickUpDashDirt(Vector2 direction) {
            if (dashDirtParticles == null || dashDirtCount <= 0) return;
            dashDirtParticles.transform.localRotation = GetMovementParticleRotation(direction);
            dashDirtParticles.Emit(dashDirtCount);
        }

        /// <summary>
        /// Shoves the pawn with this velocity. The push is added on top of normal movement and fades out
        /// (see Knockback Deceleration), so the player keeps control while being pushed. Cancels any dash in progress.
        /// </summary>
        public void Knockback(Vector2 velocity) {
            _knockbackVelocity = velocity;
            _dashEndTime = 0f;
        }

        /// <summary>
        /// Multiplies running speed for this many seconds: above 1 is a buff, below 1 is a slow.
        /// Replaces any buff or slow that's already active.
        /// </summary>
        public void ApplySpeedMultiplier(float multiplier, float duration) {
            _speedMultiplier = multiplier;
            _speedMultiplierEndTime = Time.time + duration;
        }

        protected virtual void Update() {
            if (HideIfUnbound()) return; // player disconnected mid-game
            UpdateOpening();
            UpdateAnimation();
            UpdateMovementParticles();
            // Speed lines play for as long as the dash lasts, however it ends (running out, a knockback, the round ending)
            SetEmitting(dashSpeedLines, IsDashing);
        }

        protected virtual void FixedUpdate() {
            if (_isInOpening || _isFrozen) return; // UpdateOpening moves the pawn, with physics off; a frozen pawn has physics off for good

            // Knockback fades out every physics step, whatever else the pawn is doing
            Vector2 knockback = _knockbackVelocity;
            _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, knockbackDeceleration * Time.fixedDeltaTime);

            // While dashing, let the impulse carry the pawn instead of overwriting its velocity, only turning it toward the stick
            if (IsDashing) {
                SteerDash();
                return;
            }

            // Movement, plus any knockback still pushing the pawn
            _rigidbody.linearVelocity = _moveInput * MoveSpeed + knockback;
        }

        // Turns the dash toward the stick by up to Dash Turn Speed, keeping its speed. With the stick let go it carries on straight.
        // Works on the actual velocity, so anything a collision took off the dash stays off.
        private void SteerDash() {
            if (!IsMoving || _dashTurnSpeed <= 0f) return;

            Vector2 velocity = _rigidbody.linearVelocity;
            float dashSpeed = velocity.magnitude;
            if (dashSpeed < 0.01f) return; // stopped dead (e.g. dashed straight into a wall), so there's nothing to turn

            // Turning by angle rather than rotating vectors, so the dash always turns within the 2D plane, even when the stick is held straight back
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg;
            float radians = Mathf.MoveTowardsAngle(angle, targetAngle, _dashTurnSpeed * Time.fixedDeltaTime) * Mathf.Deg2Rad;
            _rigidbody.linearVelocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * dashSpeed;
        }

        // Ignores case, so a child named "DIrt" is still found
        protected ParticleSystem FindChildParticles(string namePrefix) {
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true)) {
                if (particles.name.StartsWith(namePrefix, System.StringComparison.OrdinalIgnoreCase)) return particles;
            }
            return null;
        }

        // The pawn decides when its particles play, so start them switched on but not emitting
        protected static void SetUpParticles(ParticleSystem particles) {
            if (particles == null) return;
            particles.gameObject.SetActive(true); // Play() does nothing on an inactive object
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true; // keep emitting for as long as it's wanted, however long the system's duration is
        }

        // Starts or stops emitting. Stopping only stops emitting, so particles already out fade naturally.
        private static void SetEmitting(ParticleSystem particles, bool shouldEmit) {
            if (particles == null) return;
            if (shouldEmit && !particles.isEmitting) particles.Play();
            else if (!shouldEmit && particles.isEmitting) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // Emits dust while moving or dashing
        private void UpdateMovementParticles() {
            if (movementParticles == null) return;

            // Point the dust along the movement. Without input (stopped, or a dash with the stick let go) it keeps its last direction.
            if (IsMoving) movementParticles.transform.localRotation = GetMovementParticleRotation(_moveInput);

            SetEmitting(movementParticles, ShouldEmitMovementParticles);
        }

        // Blends the X angle between the two directions either side of the movement, taking the short way round
        private Quaternion GetMovementParticleRotation(Vector2 direction) {
            // 0 = right, 1 = up, 2 = left, 3 = down, with fractions for the diagonals in between
            float quarterTurns = Mathf.Repeat(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, 360f) / 90f;
            int from = Mathf.FloorToInt(quarterTurns);
            float x = Mathf.LerpAngle(GetParticleRotationX(from), GetParticleRotationX(from + 1), quarterTurns - from);
            return Quaternion.Euler(x, ParticleRotationY, ParticleRotationZ);
        }

        private float GetParticleRotationX(int quarterTurns) => (quarterTurns % 4) switch {
            0 => particleRotationRight,
            1 => particleRotationUp,
            2 => particleRotationLeft,
            _ => particleRotationDown,
        };

        // Picks the animation and a facing direction from movement. Right reuses the Left clips with the sprite flipped.
        protected void UpdateAnimation() {
            if (IsMoving && CanAct) _facing = GetFacing(_moveInput); // facing is locked mid-ability

            if (spriteRenderer != null) spriteRenderer.flipX = _facing == Facing.Right;
            if (animator == null) return;

            int state = GetAnimationState();

            // Only restart the clip when the state actually changes (Left <-> Right just flips, keeping the clip going)
            if (state == _currentAnimState) return;
            float startTime = GetAnimationStartTime(_currentAnimState, state);
            _currentAnimState = state;
            animator.Play(state, -1, startTime);
        }

        /// <summary>
        /// How far into the clip (0-1) a newly picked state starts. 0 = from the beginning.
        /// Override to carry on part-way, e.g. when only the direction changed (WitchPawn turning mid-attack).
        /// previousState is 0 after RestartAnimation.
        /// </summary>
        protected virtual float GetAnimationStartTime(int previousState, int newState) => 0f;

        /// <summary>
        /// The Animator state to play for the current facing (a hash from Animator.StringToHash).
        /// Idle or Run by default; override to add ability states, e.g. WitchPawn's Attack_ states.
        /// </summary>
        protected virtual int GetAnimationState() {
            bool isMoving = IsMoving || IsDashing; // keep running while a dash carries the pawn, even if the stick was let go
            return _facing switch {
                Facing.Up => isMoving ? RunUp : IdleUp,
                Facing.Left or Facing.Right => isMoving ? RunLeft : IdleLeft,
                _ => isMoving ? RunDown : IdleDown,
            };
        }

        /// Makes the next animation update play its state from the start, even if that state is already playing.
        protected void RestartAnimation() {
            _currentAnimState = 0;
        }

        protected Facing GetFacing(Vector2 direction) {
            direction.Normalize();
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            bool facingHorizontal = _facing is Facing.Left or Facing.Right;
            bool useHorizontal = facingHorizontal
                ? absX >= absY - AxisSwitchBias
                : absX > absY + AxisSwitchBias;

            if (useHorizontal) return direction.x < 0 ? Facing.Left : Facing.Right;
            return direction.y > 0 ? Facing.Up : Facing.Down;
        }
    }
}
