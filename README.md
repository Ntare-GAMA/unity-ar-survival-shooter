# AR Survival Shooter

A mobile **augmented-reality survival shooter** for Android, built with Unity 6 and AR Foundation.
Scan your floor, place a small arena on it, and survive waves of melee and ranged enemies until
the timer runs out.

**Author:** NTARE GAMA Allan  
**Platform:** Android (ARCore). The APK targets ARM64 phones running Android 7.1 or later that support ARCore.  
**Engine:** Unity 6000.4.5f1 · URP · AR Foundation 6.4 · Input System · TextMesh Pro

| Deliverable | Link |
|---|---|
| APK (Android) | _add link_ |
| Demo video | _add link_ |
| Technical documentation | _add link_ |

---

## Gameplay

1. **Start menu:** pick **Shallows** (easy) or **Deep Sea** (hard), then tap **Start** (or open the **Leaderboard**).
2. **Scan (Find a Spot):** move the phone slowly. Detected horizontal surfaces are shown with a custom glowing tracker that has the author's name printed in its texture.
3. **Place:** tap a marked surface. The arena is placed once and anchored. Plane detection then stops and the trackers are hidden.
4. **Survive:** twin-stick controls. The left stick (**MOVE**) walks. The right stick (**AIM**) turns the character and fires continuously in that direction while pushed. Aiming is fully manual. Both sticks are relative to your view, so "up" always points away from you.
5. **Game over:** the round ends when the timer runs out (you survived) or your health reaches zero. The summary shows final score, enemies defeated and time survived. You can **Restart**, open the **Leaderboard**, or go back to the **Main Menu**.

### Enemies

| | Melee: Raptor | Shooter: Ork Gunner |
|---|---|---|
| Model | PBR Velociraptor (mobile 5K version), ~30 cm long | OrkDestroyer2, ~30 cm tall, carrying a pistol |
| Animation | Idle, run, bite, hit reaction, death | Idle, walk, hit reaction, death; procedural recoil and muzzle flash on each shot |
| Behaviour | Sprints straight at the player | Advances to 0.6 m, then holds and fires |
| Attack | Bite at close range (~0.21 m, snout plus player radius), 2 damage, 1.1 s cooldown. Damage lands 0.3 s into the bite, so it can be dodged | Long range (0.75 m), pooled projectiles fired from the pistol muzzle, 1 damage, 1.6 s cooldown |
| Bullets to kill (Shallows / Deep Sea) | 2 / 3 | 4 / 6 |
| Score | 10 | 25 |

### Difficulty

| | Shallows | Deep Sea |
|---|---|---|
| Round length | 1:30 | 2:00 |
| Spawn interval | 5 s | 3 s |
| Enemies alive at once | 1 at the start, rising to 3 | 2 at the start, rising to 5 |
| Shooter share | 30 % | 45 % |
| Enemy health / speed / damage | ×1 / ×0.85 / ×1 | ×1.5 / ×1.25 / ×1.5 |

Difficulty values live in `Assets/Data/Difficulty/*.asset` and can be tuned without code changes.

### Editor controls

In Play Mode in the editor, **WASD** moves and the **arrow keys** aim and fire. The two on-screen
joysticks send the same inputs through a virtual gamepad (left stick and right stick).

---

## Architecture

```
                 ┌──────────────────── EventBus<T> (Observer) ────────────────────┐
                 │  RoundStarted · RoundEnded · ScoreChanged · TimerChanged ·     │
                 │  PlayerHealthChanged · PlayerDamaged · PlayerDied · EnemySpawned│
                 │  EnemyKilled · EnemyShot · MeleeAttack · GameStateChanged ...  │
                 └───▲──────────────▲─────────────────▲─────────────────▲─────────┘
                     │ raise         │ raise            │ listen           │ listen
 ┌───────────────────┴──┐  ┌─────────┴────────┐  ┌──────┴──────┐  ┌───────┴──────┐
 │ GameManager          │  │ Player / Enemies │  │ UIManager + │  │ AudioManager │
 │ (Singleton + State)  │  │ Spawner, Pools   │  │ UIScreens   │  │ (Singleton)  │
 │ Menu→Placement→      │  └──────────────────┘  └─────────────┘  └──────────────┘
 │ Playing→GameOver     │
 └──────────────────────┘
```

Game systems never reference each other directly. They communicate through typed events, so the
HUD, audio and spawners can be changed or removed without touching gameplay code.

| Pattern | Where | Why |
|---|---|---|
| **State** | `States/` (`GameState` → `MainMenuState`, `PlacementState`, `PlayingState`, `GameOverState`) | Each phase owns its own enter/tick/exit logic. There's no switch statement spread across the codebase. |
| **Singleton** | `GameManager`, `AudioManager` (`Core/Singleton<T>`) | One authoritative owner of game flow and of audio sources. |
| **Observer** | `Core/EventBus<T>`, `Core/GameEvents.cs` | Keeps UI, audio and gameplay decoupled. |
| **Object Pool** | `Pooling/ObjectPool<T>`, `Combat/ProjectilePool`, `Enemies/EnemyFactory` | Bullets and enemies are pre-created and recycled, with zero `Instantiate`/`Destroy` during play. |
| **Factory** | `Enemies/EnemyFactory` | The only code that knows how to produce each `EnemyKind`. |
| **Template Method** | `Enemies/Enemy` (`StopDistance`, `PerformAttack`) | Shared chase/cooldown/health logic, with a small per-type override. |

**OOP.** `Enemy` is an abstract base class for `MeleeEnemy` and `ShooterEnemy` (inheritance and
polymorphism). The player and enemies both implement `IDamageable`, so projectiles hurt either one
without knowing which. Every UI screen derives from `UIScreen`, which handles fading and button
sound wiring. State is kept private and exposed through read-only properties (encapsulation).

### Object pooling

- `ObjectPool<T>` holds a queue of inactive instances. It is pre-warmed in `Awake`
  (40 player bullets, 40 enemy bullets, 10 of each enemy type).
- `Get()` activates an instance and calls `IPoolable.OnSpawn()`. `Release()` calls
  `OnDespawn()`, deactivates the instance and re-queues it. Both reset lifetime, trails, targets
  and health.
- If a pool is exhausted, the oldest active object is recycled instead of creating a new one.
- All pools are cleared when a round starts and ends (`RoundStartedEvent` / `RoundEndedEvent`).
- The automated test checks that pool sizes never grow during a round.

### Sound

One `AudioManager` owns a single music `AudioSource` plus a fixed pool of 8 SFX voices, used
round-robin with slight random pitch variation. Enemies and bullets have no AudioSources of their
own. Sounds are triggered by events:

| Event | Sound |
|---|---|
| Player shoots | `PlayerShoot` |
| Player takes damage / dies | `PlayerHurt` / `PlayerDeath` |
| Enemy spawns | `EnemySpawn` |
| Shooter enemy fires | `EnemyShoot` |
| Melee enemy lands a hit | `MeleeAttack` |
| Enemy hit / killed | `EnemyHit` / `EnemyDeath` |
| Round start / win / lose | `RoundStart` / `RoundWin` / `RoundLose` |
| Any UI button | `UIClick` |

**Sources:**
- **Sound effects:** all original, synthesised procedurally by
  `Assets/Scripts/Editor/SoundGenerator.cs` (menu **Tools ▸ AR Survival ▸ Generate Sounds**). They are
  imported as decompress-on-load ADPCM.
- **Background music:** "Underwater" by **Moodmode**, from [Pixabay](https://pixabay.com/) under the
  Pixabay Content License (`Assets/Underwater music/`). It is streamed as compressed Vorbis, so the
  track never sits fully in memory.

### Leaderboard

`Data/Leaderboard.cs` stores the **latest 5 sessions** (newest first) as JSON in `PlayerPrefs`, so
it persists between app launches. Each entry records score, enemies defeated, time survived,
difficulty, outcome and a timestamp.

### Look and feel: "ocean night" cartoon theme

The UI has a playful underwater-cartoon-at-night style. All characters and art are original:

- **Palette:** deep navy water, glowing teal, purple and pink accents, sunny yellow buttons and
  starfish-pink highlights.
- **Fonts:** Luckiest Guy for chunky headings, which get a navy outline and drop shadow, and
  Sniglet for body text.
- **Shapes:** pill-shaped buttons, rounded panels with a glowing frame, and wave dividers.
- **Menus:** glowing "sky flowers" that gently bob and sway. These are animated by
  `UI/FloatingDecor.cs` from a fixed set of elements, so nothing is created at runtime.
- **HUD:**
  - pink segmented health bar, time left, score and kills
  - hit marker when your bullets land, and a kill feed ("ORK BLASTED! +25")
  - a "HERE THEY COME!" banner at round start
  - a red edge vignette on damage, which pulses at low health
- **Scene:** sandy arena, cyan player bullets and coral enemy bullets.
- **Underwater environment:** the arena is an island in a lagoon of animated water, using the
  PolyOne URP water shader on a generated ring mesh. Seven low-poly fish and a shark circle the
  arena at different heights and speeds. The fish models have no animations, so
  `Environment/FishSwimmer.cs` moves them along looping paths with bobbing and a body sway. They
  have no colliders, so bullets pass through them. The environment is part of the placed arena, so
  it is anchored to the floor and hidden in the menus. It is built by `Editor/EnvironmentBuilder.cs`.
- **Player:** a bodyguard in a black suit and sunglasses, about 22 cm tall, armed with an assault rifle. He is animated
  with the Toony Tiny People pistol idle, run and death animations, which work on him because both use
  Unity's Humanoid rig. An upper-body aim layer means he can run and shoot at the same time.
- **Generated art:** all UI sprites (pills, panels, sky flowers, waves, vignette, joystick
  ring, hit marker) are drawn in code by `Editor/UIAssets.cs`. The scene colours are applied by
  `Editor/OceanTheme.cs`.

---

## Project layout

```
Assets/
  Scripts/
    AR/         ARPlacementController (tap-to-place, anchoring, plane detection off)
    Core/       GameManager, EventBus, GameEvents, Singleton
    States/     Game state classes
    Player/     PlayerController, PlayerShooter, PlayerHealth, PlayerInputReader
    Enemies/    Enemy (abstract), MeleeEnemy, ShooterEnemy, EnemyAnimator, EnemyFactory, EnemySpawner
    Combat/     Projectile, ProjectilePool, IDamageable, HitFlash, MuzzleFlash
    Pooling/    ObjectPool<T>, IPoolable
    Audio/      AudioManager
    UI/         UIManager, UIScreen and the five screens
    Data/       DifficultySettings, Leaderboard, SessionStats
    Editor/     Scene/UI/prefab builders, enemy art builder, theme, sound generator, build script
  Tests/PlayMode/   End-to-end game loop test
  Prefabs/  Art/  Audio/  Data/  Shaders/  Scenes/
Tools/GeneratePlaneTexture.ps1   Regenerates the name texture for the plane tracker
```

The scene, UI, prefabs and sounds can all be regenerated from the **Tools ▸ AR Survival** menu.
The setup is repeatable, and nothing in the scene depends on manual wiring.

**Rebuild Enemies, Theme and UI** rebuilds the enemy prefabs from the imported art. It:

- upgrades the Ork's Built-in materials to URP Lit
- creates Animator Controllers (Idle, Move, Attack, Hit, Death)
- scales each model to arena size and hides its built-in weapons
- finds each gun's muzzle and grip from its mesh and attaches the gun to the right hand bone

---

## Building

1. Open the project in **Unity 6000.4.5f1** with Android Build Support (SDK, NDK, OpenJDK).
2. Open `Assets/Scenes/GameScene.unity`.
3. Choose **Tools ▸ AR Survival ▸ Build Android APK**. The APK is written to `Builds/AR-Survival-Shooter.apk`.
   Alternatively, use **File ▸ Build Profiles ▸ Android ▸ Build**.

Player settings: IL2CPP, ARM64, OpenGLES3 only, minimum API 25, portrait. ARCore is required.

## Tests

Run from **Window ▸ General ▸ Test Runner ▸ PlayMode**, or headless:

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml
```

`GameLoopTests` plays a full round without an AR device. It checks:

- the state flow and the screen shown for each state
- that enemies spawn on the plane
- that pooled bullets kill enemies and add score
- that enemies damage the player
- game over with enemy and bullet wipe
- that the leaderboard saves the session
- that pools never grow during play
- restart and quit to menu

## Credits

| Asset | Source |
|---|---|
| Shooter enemy model and animations | **OrkDestroyer2**, Unity Asset Store (free) |
| Melee enemy model and animations | **PBR Velociraptor** (mobile 5K version), Unity Asset Store |
| Assault rifle (player) and pistol (Ork) | Free asset packs, in `Assets/Tools (Prefabs)` |
| Ambient fish and shark | **Fish - PolyPack** by Alstra Infinite, Unity Asset Store (free) |
| Lagoon water shader | **Free Pack - Water Shader URP** by PolyOne Studio, Unity Asset Store (free) |
| Background music | "Underwater" by **Moodmode**, [Pixabay](https://pixabay.com/) (Pixabay Content License) |
| Fonts | [Luckiest Guy](https://fonts.google.com/specimen/Luckiest+Guy) (Apache License 2.0) and [Sniglet](https://fonts.google.com/specimen/Sniglet) (SIL Open Font License). License files are in `Assets/Art/Fonts` |
| Player model | **Bodyguards** pack (Bodyguard 01), animated with the **Toony Tiny People** pistol animations (Unity Asset Store, free) |
| App icon, arena, UI sprites, plane-tracker texture, all audio | Created for this project and generated by the tools in this repository |
