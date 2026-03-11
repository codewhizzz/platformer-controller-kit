# 🎮 Platformer Controller Kit — Godot 4.x (C#)

A production-ready 2D character controller for Godot 4.x built with C#. Features the tight, responsive movement mechanics found in modern indie platformers.

![Godot 4.x](https://img.shields.io/badge/Godot-4.2-478CBF?logo=godotengine&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET%206-512BD4?logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Features

### Core Movement
- **Responsive horizontal movement** with separate ground/air acceleration and deceleration curves
- **Variable jump height** — tap for short hops, hold for full jumps
- **Coyote time** — grace period after walking off a ledge (configurable)
- **Jump buffering** — press jump slightly before landing and it registers on contact

### Advanced Mechanics
- **Wall sliding** — grip walls and slide down at reduced speed
- **Wall jumping** — leap off walls with directional momentum and brief input lock
- **Dash** — burst of speed with cooldown, freezes Y-velocity mid-dash

### Game Feel / Juice
- **Squash & stretch** — sprite deforms on jump, land, and dash
- **Screen shake** — camera shakes proportional to landing impact velocity
- **Dust particles** — emitted on jump and hard landings
- **Smooth camera** — look-ahead tracking with vertical deadzone

### Developer Tools
- **Debug HUD** — real-time display of player state, velocity, and active inputs (toggle with F1)
- **Procedural test level** — auto-generated platforms, walls, and gaps labeled by mechanic
- **Fully exported parameters** — tune every value from the Godot Inspector

---

## Controls

| Action     | Key           |
|------------|---------------|
| Move       | A/D or ← →   |
| Jump       | Space or ↑    |
| Dash       | Shift         |
| Toggle HUD | F1            |

---

## Project Structure

```
├── Scenes/
│   └── Main.tscn              # Main scene with player, level, camera, HUD
├── Scripts/
│   ├── PlayerController.cs    # Core character controller (state machine)
│   ├── CameraController.cs    # Smooth-follow camera with shake
│   ├── LevelGenerator.cs      # Procedural test level builder
│   └── DebugHud.cs            # Real-time debug overlay
├── project.godot
└── PlatformerControllerKit.csproj
```

---

## Getting Started

1. Clone the repo
2. Open in **Godot 4.2+** with .NET support
3. Build the C# solution (Build → Build Solution)
4. Press F5 to run

---

## Customization

All movement parameters are exposed as `[Export]` properties on the `PlayerController` node. Select the Player in the scene tree and adjust values in the Inspector:

- **Movement**: speed, acceleration, deceleration (ground & air)
- **Jump**: force, cut multiplier, coyote time, buffer time, fall gravity
- **Wall**: slide speed, wall jump force, input lock duration
- **Dash**: speed, duration, cooldown
- **Juice**: shake intensity, squash/stretch amounts

---

## Architecture

The controller uses a **finite state machine** with these states:

```
Idle ↔ Run ↔ Jump ↔ Fall ↔ WallSlide
                ↕
              Dash
```

Each state handles its own physics and transition logic. Coyote time and jump buffering are implemented as decrementing timers checked across state boundaries.

---

## License

MIT — use freely in your projects.
