using Godot;
using System;

namespace PlatformerControllerKit;

/// <summary>
/// A polished 2D platformer character controller featuring:
/// - Coyote time (grace period after leaving a ledge)
/// - Jump buffering (press jump slightly before landing)
/// - Variable jump height (tap vs hold)
/// - Wall sliding and wall jumping
/// - Dash with cooldown and ghost trail
/// - Screen shake on landing impact
/// - Squash and stretch juice
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    #region Exports

    [ExportGroup("Movement")]
    [Export] public float MoveSpeed { get; set; } = 280f;
    [Export] public float Acceleration { get; set; } = 2400f;
    [Export] public float Deceleration { get; set; } = 2000f;
    [Export] public float AirAcceleration { get; set; } = 1600f;
    [Export] public float AirDeceleration { get; set; } = 800f;

    [ExportGroup("Jump")]
    [Export] public float JumpForce { get; set; } = -420f;
    [Export] public float JumpCutMultiplier { get; set; } = 0.4f;
    [Export] public float CoyoteTime { get; set; } = 0.12f;
    [Export] public float JumpBufferTime { get; set; } = 0.1f;
    [Export] public float FallGravityMultiplier { get; set; } = 1.8f;
    [Export] public float MaxFallSpeed { get; set; } = 600f;

    [ExportGroup("Wall")]
    [Export] public float WallSlideSpeed { get; set; } = 80f;
    [Export] public Vector2 WallJumpForce { get; set; } = new(320f, -400f);
    [Export] public float WallJumpLockTime { get; set; } = 0.15f;

    [ExportGroup("Dash")]
    [Export] public float DashSpeed { get; set; } = 600f;
    [Export] public float DashDuration { get; set; } = 0.15f;
    [Export] public float DashCooldown { get; set; } = 0.6f;

    [ExportGroup("Juice")]
    [Export] public float LandingShakeIntensity { get; set; } = 4f;
    [Export] public float SquashAmount { get; set; } = 0.3f;
    [Export] public float StretchAmount { get; set; } = 0.2f;

    #endregion

    #region State

    private enum State { Idle, Run, Jump, Fall, WallSlide, Dash }
    private State _currentState = State.Idle;
    private State _previousState = State.Idle;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _wallJumpLockTimer;
    private bool _isDashing;
    private int _dashDirection;
    private int _facingDirection = 1;
    private bool _wasOnFloor;
    private float _lastYVelocity;

    private float _baseGravity;

    #endregion

    #region Node References

    private Sprite2D _sprite;
    private AnimationPlayer _animPlayer;
    private GpuParticles2D _dustParticles;
    private GpuParticles2D _dashGhostParticles;
    private RayCast2D _wallCheckLeft;
    private RayCast2D _wallCheckRight;
    private Camera2D _camera;
    private Timer _shakeTimer;
    private Label _stateLabel;

    #endregion

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _dustParticles = GetNodeOrNull<GpuParticles2D>("DustParticles");
        _dashGhostParticles = GetNodeOrNull<GpuParticles2D>("DashGhostParticles");
        _wallCheckLeft = GetNodeOrNull<RayCast2D>("WallCheckLeft");
        _wallCheckRight = GetNodeOrNull<RayCast2D>("WallCheckRight");
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        _stateLabel = GetNodeOrNull<Label>("StateLabel");

        _baseGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        UpdateTimers(dt);
        HandleState(dt);
        MoveAndSlide();
        UpdateVisuals(dt);
        HandleLanding();

        _wasOnFloor = IsOnFloor();
        _lastYVelocity = Velocity.Y;
    }

    #region Timers

    private void UpdateTimers(float dt)
    {
        _coyoteTimer -= dt;
        _jumpBufferTimer -= dt;
        _dashCooldownTimer -= dt;
        _wallJumpLockTimer -= dt;

        if (_isDashing)
        {
            _dashTimer -= dt;
            if (_dashTimer <= 0)
            {
                _isDashing = false;
            }
        }

        // Coyote time: reset when grounded
        if (IsOnFloor())
            _coyoteTimer = CoyoteTime;

        // Jump buffer
        if (Input.IsActionJustPressed("jump"))
            _jumpBufferTimer = JumpBufferTime;
    }

    #endregion

    #region State Machine

    private void HandleState(float dt)
    {
        _previousState = _currentState;

        switch (_currentState)
        {
            case State.Idle:
            case State.Run:
                GroundedState(dt);
                break;
            case State.Jump:
            case State.Fall:
                AirborneState(dt);
                break;
            case State.WallSlide:
                WallSlideState(dt);
                break;
            case State.Dash:
                DashState(dt);
                break;
        }
    }

    private void GroundedState(float dt)
    {
        float inputX = Input.GetAxis("move_left", "move_right");
        ApplyHorizontalMovement(inputX, Acceleration, Deceleration, dt);
        ApplyGravity(dt);

        // Transitions
        if (_jumpBufferTimer > 0)
        {
            ExecuteJump();
            return;
        }

        if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0)
        {
            ExecuteDash(inputX);
            return;
        }

        if (!IsOnFloor() && _coyoteTimer <= 0)
        {
            _currentState = State.Fall;
            return;
        }

        _currentState = Mathf.Abs(Velocity.X) > 10f ? State.Run : State.Idle;
    }

    private void AirborneState(float dt)
    {
        float inputX = Input.GetAxis("move_left", "move_right");

        if (_wallJumpLockTimer <= 0)
            ApplyHorizontalMovement(inputX, AirAcceleration, AirDeceleration, dt);

        ApplyGravity(dt);

        // Variable jump height: cut velocity on release
        if (Input.IsActionJustReleased("jump") && Velocity.Y < 0)
        {
            var vel = Velocity;
            vel.Y *= JumpCutMultiplier;
            Velocity = vel;
        }

        // Coyote jump
        if (_jumpBufferTimer > 0 && _coyoteTimer > 0)
        {
            ExecuteJump();
            return;
        }

        // Wall slide detection
        if (!IsOnFloor() && IsOnWall() && inputX != 0)
        {
            _currentState = State.WallSlide;
            return;
        }

        // Dash
        if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0)
        {
            ExecuteDash(inputX);
            return;
        }

        if (IsOnFloor())
        {
            _currentState = State.Idle;
            return;
        }

        _currentState = Velocity.Y < 0 ? State.Jump : State.Fall;
    }

    private void WallSlideState(float dt)
    {
        float inputX = Input.GetAxis("move_left", "move_right");
        int wallDir = GetWallDirection();

        // Slide down slowly
        var vel = Velocity;
        vel.Y = Mathf.MoveToward(vel.Y, WallSlideSpeed, _baseGravity * dt);
        vel.X = 0;
        Velocity = vel;

        // Wall jump
        if (Input.IsActionJustPressed("jump"))
        {
            ExecuteWallJump(wallDir);
            return;
        }

        // Let go of wall
        if (IsOnFloor() || !IsOnWall() || (wallDir == -1 && inputX > 0) || (wallDir == 1 && inputX < 0))
        {
            _currentState = IsOnFloor() ? State.Idle : State.Fall;
        }
    }

    private void DashState(float dt)
    {
        if (!_isDashing)
        {
            _currentState = IsOnFloor() ? State.Idle : State.Fall;
            return;
        }

        var vel = Velocity;
        vel.X = _dashDirection * DashSpeed;
        vel.Y = 0; // Freeze Y during dash
        Velocity = vel;
    }

    #endregion

    #region Actions

    private void ExecuteJump()
    {
        var vel = Velocity;
        vel.Y = JumpForce;
        Velocity = vel;
        _currentState = State.Jump;
        _coyoteTimer = 0;
        _jumpBufferTimer = 0;

        ApplySquashStretch(1f - SquashAmount, 1f + StretchAmount);
        EmitDust();
    }

    private void ExecuteWallJump(int wallDir)
    {
        var vel = Velocity;
        vel.X = -wallDir * WallJumpForce.X;
        vel.Y = WallJumpForce.Y;
        Velocity = vel;
        _currentState = State.Jump;
        _wallJumpLockTimer = WallJumpLockTime;
        _facingDirection = -wallDir;

        ApplySquashStretch(1f - SquashAmount, 1f + StretchAmount);
    }

    private void ExecuteDash(float inputX)
    {
        _dashDirection = inputX != 0 ? (int)Mathf.Sign(inputX) : _facingDirection;
        _dashTimer = DashDuration;
        _dashCooldownTimer = DashCooldown;
        _isDashing = true;
        _currentState = State.Dash;

        ApplySquashStretch(1f + StretchAmount, 1f - SquashAmount * 0.5f);

        if (_dashGhostParticles != null)
            _dashGhostParticles.Emitting = true;
    }

    #endregion

    #region Movement Helpers

    private void ApplyHorizontalMovement(float inputX, float accel, float decel, float dt)
    {
        var vel = Velocity;

        if (Mathf.Abs(inputX) > 0.1f)
        {
            vel.X = Mathf.MoveToward(vel.X, inputX * MoveSpeed, accel * dt);
            _facingDirection = (int)Mathf.Sign(inputX);
        }
        else
        {
            vel.X = Mathf.MoveToward(vel.X, 0, decel * dt);
        }

        Velocity = vel;
    }

    private void ApplyGravity(float dt)
    {
        if (IsOnFloor()) return;

        var vel = Velocity;
        float gravMult = vel.Y > 0 ? FallGravityMultiplier : 1f;
        vel.Y = Mathf.Min(vel.Y + _baseGravity * gravMult * dt, MaxFallSpeed);
        Velocity = vel;
    }

    private int GetWallDirection()
    {
        if (_wallCheckLeft != null && _wallCheckLeft.IsColliding()) return -1;
        if (_wallCheckRight != null && _wallCheckRight.IsColliding()) return 1;
        // Fallback: use built-in wall normal
        if (IsOnWall())
        {
            var normal = GetWallNormal();
            return normal.X < 0 ? 1 : -1;
        }
        return 0;
    }

    #endregion

    #region Visuals & Juice

    private void UpdateVisuals(float dt)
    {
        // Flip sprite
        if (_facingDirection != 0)
            _sprite.FlipH = _facingDirection < 0;

        // Smooth squash/stretch back to normal
        _sprite.Scale = _sprite.Scale.Lerp(Vector2.One, 12f * dt);

        // State label (debug HUD)
        if (_stateLabel != null)
            _stateLabel.Text = _currentState.ToString();

        // Stop dash particles when not dashing
        if (_dashGhostParticles != null && !_isDashing)
            _dashGhostParticles.Emitting = false;
    }

    private void HandleLanding()
    {
        if (IsOnFloor() && !_wasOnFloor && _lastYVelocity > 100f)
        {
            float intensity = Mathf.Remap(_lastYVelocity, 100f, MaxFallSpeed, 0.5f, 1f);
            ApplySquashStretch(1f + SquashAmount * intensity, 1f - SquashAmount * intensity * 0.6f);
            ApplyScreenShake(LandingShakeIntensity * intensity);
            EmitDust();
        }
    }

    private void ApplySquashStretch(float scaleX, float scaleY)
    {
        _sprite.Scale = new Vector2(scaleX, scaleY);
    }

    private void ApplyScreenShake(float intensity)
    {
        if (_camera == null) return;

        var tween = CreateTween();
        tween.TweenProperty(_camera, "offset",
            new Vector2((float)GD.RandRange(-intensity, intensity), (float)GD.RandRange(-intensity, intensity)),
            0.04f);
        tween.TweenProperty(_camera, "offset",
            new Vector2((float)GD.RandRange(-intensity * 0.5f, intensity * 0.5f), (float)GD.RandRange(-intensity * 0.5f, intensity * 0.5f)),
            0.04f);
        tween.TweenProperty(_camera, "offset", Vector2.Zero, 0.08f);
    }

    private void EmitDust()
    {
        if (_dustParticles != null)
        {
            _dustParticles.Restart();
            _dustParticles.Emitting = true;
        }
    }

    #endregion
}
