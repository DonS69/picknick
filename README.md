# Picknick

2D cartoon co-op puzzle game: tiny creatures trapped in a giant house, each with a swappable ability.
Unity + C#, online co-op over Steam. The design and build plan live in [`docs/prototype-plan.md`](docs/prototype-plan.md).

This repository currently covers **plan steps 1–3**: project setup, creatures that run, jump and stack on each other, and the three abilities (Magnet, Feather, Strong) with badge swapping, in a graybox sandbox with four creatures.

## Open the project

1. Install **Unity 6 LTS** (6000.x) through Unity Hub. Add the 2D module if Hub offers it.
2. In Unity Hub: **Add > Add project from disk** and pick this folder. When Hub asks for an editor version, choose your Unity 6 LTS.
3. On first open Unity imports packages. If it asks to **enable the new Input System backends and restart**, click **Yes**.
4. Menu **Picknick > Create Sandbox Scene**, then press **Play**.
5. Commit the `.meta` files and `ProjectSettings/` Unity generated on first open. They are needed by everyone else who opens the project.

## Sandbox controls

| Device | Move | Jump | Aim | Ability | Swap badge | Other |
|---|---|---|---|---|---|---|
| Keyboard + mouse | A/D or arrows | Space / W / Up | Mouse | E or left click | Q | Tab switches creature (white marker) |
| Gamepad (one per creature) | Left stick / d-pad | South (A / Cross) | Right stick | West (X / Square) or RT | North (Y / Triangle) | |

Starting badges: creature 1 Strong, 2 Feather, 3 and 4 Magnet. Swapping trades badges with the nearest teammate, so the room always has the same set of abilities.

| Ability | What it does |
|---|---|
| Magnet (mass 1) | Hold to pull the blue metal or a teammate you aim at. A lighter target flies to you; a heavier or fixed one pulls you to it (grappling hook). Works while being carried. |
| Feather (mass 0.5) | Hold Jump in the air to glide. Ability grabs or drops a nearby teammate; carrying makes you sink faster. |
| Strong (mass 2) | The only one who can push the dark Heavy can. Ability picks up a teammate; press again to throw where you aim. Light teammates fly farther. |

A carried creature can wriggle free by pressing Jump.

Things to try: get all four onto the high ledge (stack, use the crate, or get thrown). Cross the pit to the far ledge (glide, grapple the anchor above the pit, or get pulled across by a Magnet). Reach the high shelf at the far right (Strong throw, or grapple its anchor).

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
