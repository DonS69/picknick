# Picknick

2D cartoon co-op puzzle game: tiny creatures trapped in a giant house, each with a swappable ability.
Unity + C#, online co-op over Steam. The design and build plan live in [`docs/prototype-plan.md`](docs/prototype-plan.md).

This repository currently covers **plan steps 1–2**: project setup and one creature that runs, jumps and stacks on other creatures, in a graybox sandbox with four creatures.

## Open the project

1. Install **Unity 6 LTS** (6000.x) through Unity Hub. Add the 2D module if Hub offers it.
2. In Unity Hub: **Add > Add project from disk** and pick this folder. When Hub asks for an editor version, choose your Unity 6 LTS.
3. On first open Unity imports packages. If it asks to **enable the new Input System backends and restart**, click **Yes**.
4. Menu **Picknick > Create Sandbox Scene**, then press **Play**.
5. Commit the `.meta` files and `ProjectSettings/` Unity generated on first open. They are needed by everyone else who opens the project.

## Sandbox controls

| Device | Move | Jump | Other |
|---|---|---|---|
| Keyboard | A/D or arrows | Space / W / Up | Tab switches which creature you control (white marker) |
| Gamepad (one per creature) | Left stick / d-pad | South button (A / Cross) | |

The sandbox goal is to get all four creatures onto the high ledge. A single creature can't jump it from another's head, but two stacked can, and so can one standing on the crate. The far ledge across the pit is deliberately just out of jump range (a job for Feather later).

Tune movement by creating **Assets > Create > Picknick > Creature Tuning**, assigning it to the `Sandbox` object's `Tuning` field, and changing values in Play mode.

## Layout

```
Assets/Picknick/
  Scripts/              Picknick.Runtime assembly
    Creature/           CreatureController, CreatureInput, CreatureTuning, FallRespawn
    CameraRig/          SharedCamera: keeps every creature on screen
    Sandbox/            SandboxBootstrap (builds the test level), LocalInputRouter (test-only input)
    Abilities/          Magnet, Feather, Strong (step 3)
    Puzzle/             Doors, levers, badges, checkpoints (step 4)
    Net/                Netcode + Steam (steps 5–6)
  Editor/               Picknick.Editor assembly: menu items
  Scenes/ Prefabs/ Rooms/ Settings/
```

## Notes

- **Rendering:** the built-in 2D renderer is used for now. URP and 2D lighting can be added in the art pass (plan step 8).
- **Input:** `CreatureController` never reads devices; it gets a `CreatureInput` each frame. Today `LocalInputRouter` fills it from keyboard and gamepads. Later the owning network client fills it, so the controller stays the same.
- **Physics:** creatures have zero friction so they don't stick to walls or each other. Riding on a moving creature or crate is handled by adding the velocity of whatever you stand on. With no input in the air, momentum is kept, so throws and Magnet pulls (step 3) aren't cancelled.
- **Git LFS:** art and audio files are routed through LFS (`.gitattributes`). Run `git lfs install` once on your machine.
