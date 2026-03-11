using Godot;

namespace PlatformerControllerKit;

/// <summary>
/// Smooth-follow camera with look-ahead, vertical deadzone, and shake support.
/// </summary>
public partial class CameraController : Camera2D
{
    [Export] public NodePath TargetPath { get; set; }
    [Export] public float SmoothSpeed { get; set; } = 8f;
    [Export] public float LookAheadDistance { get; set; } = 60f;
    [Export] public float LookAheadSmooth { get; set; } = 4f;
    [Export] public float VerticalDeadzone { get; set; } = 30f;

    private Node2D _target;
    private float _lookAheadX;
    private float _lastTargetY;

    public override void _Ready()
    {
        if (TargetPath != null)
            _target = GetNodeOrNull<Node2D>(TargetPath);

        if (_target != null)
            _lastTargetY = _target.GlobalPosition.Y;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target == null) return;

        float dt = (float)delta;
        var targetPos = _target.GlobalPosition;

        // Look-ahead based on velocity
        if (_target is CharacterBody2D body)
        {
            float desiredLookAhead = Mathf.Sign(body.Velocity.X) * LookAheadDistance;
            _lookAheadX = Mathf.Lerp(_lookAheadX, desiredLookAhead, LookAheadSmooth * dt);
        }

        // Vertical deadzone: only follow if target moves beyond threshold
        float targetY = GlobalPosition.Y;
        if (Mathf.Abs(targetPos.Y - _lastTargetY) > VerticalDeadzone || _target.IsOnFloor())
        {
            targetY = targetPos.Y;
        }

        var desiredPos = new Vector2(targetPos.X + _lookAheadX, targetY);
        GlobalPosition = GlobalPosition.Lerp(desiredPos, SmoothSpeed * dt);

        _lastTargetY = targetPos.Y;
    }
}
