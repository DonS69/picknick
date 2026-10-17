# Picknick

2D cartoon co-op puzzle game: tiny creatures trapped in a giant house, each with a swappable ability.
Unity + C#, online co-op over Steam. The design and build plan live in [`docs/prototype-plan.md`](docs/prototype-plan.md).

This repository currently covers **plan steps 1–4**: project setup, creatures that run, jump and stack on each other, the three abilities (Magnet, Feather, Strong) with badge swapping, a graybox sandbox, and Room 1 "The Kitchen Counter" in graybox.

## Open the project

1. Install **Unity 6 LTS** (6000.x) through Unity Hub. Add the 2D module if Hub offers it.
2. In Unity Hub: **Add > Add project from disk** and pick this folder. When Hub asks for an editor version, choose your Unity 6 LTS.
3. On first open Unity imports packages. If it asks to **enable the new Input System backends and restart**, click **Yes**.
4. Menu **Picknick > Create Sandbox Scene** (or **Create Room 1 Scene (Kitchen)**), then press **Play**.
5. Commit the `.meta` files and `ProjectSettings/` Unity generated on first open. They are needed by everyone else who opens the project.

## Sandbox controls

| Device | Move | Jump | Aim | Ability | Swap badge | Other |
|---|---|---|---|---|---|---|
| Keyboard + mouse | A/D or arrows | Space / W / Up | Mouse | E or left click | Q | Tab switches creature (white marker) |
| Gamepad (one per creature) | Left stick / d-pad | South (A / Cross) | Right stick | West (X / Square) or RT | North (Y / Triangle) | |

Starting badges: creature 1 Strong, 2 Feather, 3 and 4 Magnet. Swapping trades badges with the nearest teammate, so the room always has the same set of abilities.

| Ability | What it does |
|---|---|
| Magnet (mass 1) | Hold to pull the blue metal or a teammate you aim at. Standing on the ground you plant your feet and the target comes to you (a Strong teammate is a slow heave). In the air, or on fixed metal, you fly to it instead (grappling hook). Works while being carried. |
| Feather (mass 0.5) | Hold Jump in the air to glide. Ability grabs or drops a nearby teammate; carrying makes you sink faster. |
| Strong (mass 2) | The only one who can push the dark Heavy can. Ability picks up a teammate; press again to throw where you aim. Light teammates fly farther. |

A carried creature can wriggle free by pressing Jump.

Things to try: get all four onto the high ledge (stack, use the crate, or get thrown). Cross the pit to the far ledge (glide, grapple the anchor above the pit, or get pulled across by a Magnet). Reach the high shelf at the far right (Strong throw, or grapple its anchor).

Tune movement by creating **Assets > Create > Picknick > Creature Tuning**, assigning it to the `Sandbox` object's `Tuning` field, and changing values in Play mode.

## Online play (test on one PC)

The room is simulated by the player who hosts; everyone else sends their input and sees what the host sends back.

1. **Window > Multiplayer > Multiplayer Play Mode**, tick **Player 2** (and 3, 4 if you like). Extra game windows open.
2. Press **Play**. In the main window click **Host game** (bottom right). In each extra window click **Join game** (address `127.0.0.1`).
3. Each window controls its own creature. **Tab** switches to a creature nobody is controlling.

To play across two PCs on the same network, the host shares their local IP address (for example `192.168.1.20`) and the others type it before Join. Over the internet this needs port forwarding until Steam is added (plan step 6).

## Room 1: The Kitchen Counter

Everyone has to end up in the mouse hole on top of the fridge, at the same time. Falling sends you back to your last checkpoint.

- **A, the can:** the counter is 3 high. Strong pushes the heavy can over, or you stack, or Strong throws people up. Getting Strong up last is the real puzzle.
- **B, the sink gap:** 4 wide, just too far to jump. Feather glides (carrying someone barely makes it), Magnets pull teammates across, or grapple the faucet.
- **C, the cupboard door:** opens only while Strong stands right next to it. Someone on the other side must pull the spoon lever with a Magnet to latch it, or Strong is stuck.
- **D, the fridge:** pull the key off its hook with a Magnet (it's high: stack on the cookie box, get carried or thrown). Reach the fridge top via the fridge magnet or throws, then bring the key to the hatch to open the mouse hole.

## Layout

```
Assets/Picknick/
  Scripts/              Picknick.Runtime assembly
    Creature/           CreatureController, CreatureInput, CreatureTuning, FallRespawn
    CameraRig/          SharedCamera: keeps every creature on screen
    Sandbox/            SandboxBootstrap (builds the test level), LocalInputRouter (test-only input)
    Abilities/          Magnet, Feather, Strong (step 3)
    Puzzle/             Zone, Checkpoint, ExitZone, HoldDoor, MagnetLever, HookedItem, Hatch
    Rooms/              KitchenRoom (Room 1 graybox, built at runtime)
    Net/                NetSession (host/join, input, snapshots), SyncedBody
  Editor/               Picknick.Editor assembly: menu items
  Scenes/ Prefabs/ Rooms/ Settings/
```

## Notes

- **Rendering:** the built-in 2D renderer is used for now. URP and 2D lighting can be added in the art pass (plan step 8).
- **Input:** `CreatureController` never reads devices; it gets a `CreatureInput` each frame. Today `LocalInputRouter` fills it from keyboard and gamepads. Later the owning network client fills it, so the controller stays the same.
- **Physics:** creatures have zero friction so they don't stick to walls or each other. Riding on a moving creature or crate is handled by adding the velocity of whatever you stand on. With no input in the air, momentum is kept, so throws and Magnet pulls (step 3) aren't cancelled.
- **Git LFS:** art and audio files are routed through LFS (`.gitattributes`). Run `git lfs install` once on your machine.
