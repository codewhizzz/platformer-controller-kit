using Godot;

namespace PlatformerControllerKit;

/// <summary>
/// Debug HUD displaying current player state, velocity, and active inputs.
/// Toggle visibility with F1.
/// </summary>
public partial class DebugHud : CanvasLayer
{
    private Label _stateLabel;
    private Label _velocityLabel;
    private Label _inputLabel;
    private Label _infoLabel;
    private PlayerController _player;

    public override void _Ready()
    {
        var panel = new PanelContainer();
        panel.Position = new Vector2(16, 16);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0, 0, 0, 0.6f);
        styleBox.CornerRadiusBottomLeft = 8;
        styleBox.CornerRadiusBottomRight = 8;
        styleBox.CornerRadiusTopLeft = 8;
        styleBox.CornerRadiusTopRight = 8;
        styleBox.ContentMarginLeft = 12;
        styleBox.ContentMarginRight = 12;
        styleBox.ContentMarginTop = 8;
        styleBox.ContentMarginBottom = 8;
        panel.AddThemeStyleboxOverride("panel", styleBox);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);

        _stateLabel = CreateLabel("State: ---");
        _velocityLabel = CreateLabel("Vel: ---");
        _inputLabel = CreateLabel("Input: ---");
        _infoLabel = CreateLabel("[F1] Toggle HUD");

        vbox.AddChild(_stateLabel);
        vbox.AddChild(_velocityLabel);
        vbox.AddChild(_inputLabel);
        vbox.AddChild(new HSeparator());
        vbox.AddChild(_infoLabel);

        panel.AddChild(vbox);
        AddChild(panel);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_home") || Input.IsKeyPressed(Key.F1))
            Visible = !Visible;

        if (!Visible) return;

        if (_player == null)
            _player = GetTree().Root.FindChild("Player", true, false) as PlayerController;

        if (_player == null) return;

        _velocityLabel.Text = $"Vel: ({_player.Velocity.X:F0}, {_player.Velocity.Y:F0})";

        var inputs = "";
        if (Input.IsActionPressed("move_left")) inputs += "← ";
        if (Input.IsActionPressed("move_right")) inputs += "→ ";
        if (Input.IsActionPressed("jump")) inputs += "⬆ ";
        if (Input.IsActionPressed("dash")) inputs += "DASH ";
        _inputLabel.Text = $"Input: {(inputs.Length > 0 ? inputs.Trim() : "none")}";
    }

    private Label CreateLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1f));
        return label;
    }
}
