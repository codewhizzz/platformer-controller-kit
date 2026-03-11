using Godot;
using System;

namespace PlatformerControllerKit;

/// <summary>
/// Generates a simple test level with platforms, walls, and gaps
/// to showcase all controller features. Attach to a Node2D in Main.tscn.
/// </summary>
public partial class LevelGenerator : Node2D
{
    [Export] public Color PlatformColor { get; set; } = new(0.18f, 0.2f, 0.28f);
    [Export] public Color WallColor { get; set; } = new(0.14f, 0.16f, 0.22f);
    [Export] public Color AccentColor { get; set; } = new(0.3f, 0.55f, 0.85f);

    public override void _Ready()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
        // Ground platforms
        CreatePlatform(new Vector2(-200, 400), new Vector2(600, 40), PlatformColor);
        CreatePlatform(new Vector2(500, 400), new Vector2(300, 40), PlatformColor);
        CreatePlatform(new Vector2(900, 400), new Vector2(500, 40), PlatformColor);
        CreatePlatform(new Vector2(1500, 400), new Vector2(400, 40), PlatformColor);

        // Elevated platforms (for jump testing)
        CreatePlatform(new Vector2(200, 260), new Vector2(180, 20), AccentColor);
        CreatePlatform(new Vector2(500, 160), new Vector2(140, 20), AccentColor);
        CreatePlatform(new Vector2(780, 80), new Vector2(120, 20), AccentColor);

        // Wall jump corridor
        CreatePlatform(new Vector2(1050, 100), new Vector2(20, 300), WallColor); // Left wall
        CreatePlatform(new Vector2(1200, 100), new Vector2(20, 300), WallColor); // Right wall
        CreatePlatform(new Vector2(1050, 80), new Vector2(170, 20), AccentColor); // Top platform

        // Dash gap
        CreatePlatform(new Vector2(1400, 250), new Vector2(100, 20), AccentColor);
        CreatePlatform(new Vector2(1700, 250), new Vector2(100, 20), AccentColor);

        // Moving platform area
        CreatePlatform(new Vector2(1900, 400), new Vector2(800, 40), PlatformColor);

        // Step platforms
        for (int i = 0; i < 5; i++)
        {
            CreatePlatform(
                new Vector2(2000 + i * 120, 350 - i * 60),
                new Vector2(80, 16),
                AccentColor.Lerp(PlatformColor, i / 4f)
            );
        }

        // Boundary walls
        CreatePlatform(new Vector2(-220, -200), new Vector2(20, 640), WallColor);
        CreatePlatform(new Vector2(2700, -200), new Vector2(20, 640), WallColor);

        // Labels for areas
        CreateLabel(new Vector2(100, 370), "BASIC MOVEMENT", AccentColor);
        CreateLabel(new Vector2(300, 230), "JUMP CHAIN", AccentColor);
        CreateLabel(new Vector2(1080, 60), "WALL JUMP", AccentColor);
        CreateLabel(new Vector2(1480, 220), "DASH GAP →", AccentColor);
        CreateLabel(new Vector2(2050, 150), "STAIRCASE", AccentColor);
    }

    private void CreatePlatform(Vector2 position, Vector2 size, Color color)
    {
        var body = new StaticBody2D();
        body.Position = position;

        var shape = new CollisionShape2D();
        var rect = new RectangleShape2D();
        rect.Size = size;
        shape.Shape = rect;
        body.AddChild(shape);

        var visual = new ColorRect();
        visual.Size = size;
        visual.Position = -size / 2;
        visual.Color = color;
        body.AddChild(visual);

        AddChild(body);
    }

    private void CreateLabel(Vector2 position, string text, Color color)
    {
        var label = new Label();
        label.Position = position;
        label.Text = text;
        label.AddThemeColorOverride("font_color", color.Lerp(Colors.White, 0.3f));
        label.AddThemeFontSizeOverride("font_size", 11);
        AddChild(label);
    }
}
