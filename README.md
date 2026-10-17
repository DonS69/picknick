# Picknick

2D cartoon co-op puzzle game: tiny creatures trapped in a giant house, each with a swappable ability.
Unity + C#, online co-op over Steam. The design and build plan live in [`docs/prototype-plan.md`](docs/prototype-plan.md).

This repository currently covers **plan steps 1–6** plus a start menu: creatures that run, jump and stack on each other, the three abilities (Magnet, Feather, Strong) with badge swapping, three graybox rooms, and online co-op over Steam or LAN.

## Open the project

1. Install **Unity 6 LTS** (6000.x) through Unity Hub. Add the 2D module if Hub offers it.
2. In Unity Hub: **Add > Add project from disk** and pick this folder. When Hub asks for an editor version, choose your Unity 6 LTS.
3. On first open Unity imports packages. If it asks to **enable the new Input System backends and restart**, click **Yes**.
4. Menu **Picknick > Create All Scenes**. It (re)creates the menu, the three rooms and the sandbox, puts them in Build Settings with the menu first, and opens the menu. Press **Play**.
5. Commit the `.meta` files and `ProjectSettings/` Unity generated on first open. They are needed by everyone else who opens the project.

## Sandbox controls

| Device | Move | Jump | Aim | Ability | Swap badge | Other |
|---|---|---|---|---|---|---|
| Keyboard + mouse | A/D or arrows | Space / W / Up | Mouse | E or left click | Q | Tab switches creature (white marker) |
| Gamepad (one per creature) | Left stick / d-pad | South (A / Cross) | Right stick | West (X / Square) or RT | North (Y / Triangle) | |

**Esc** (or Start on a gamepad) opens the in-room menu: restart the room, skip to the next one, or go back to the main menu.

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
2. Press **Play** in the Menu scene. In the main window pick a room and click **Host** next to "LAN / this PC". In each extra window click **Join** (address `127.0.0.1`).
3. Each window controls its own creature. **Tab** switches to a creature nobody is controlling.

To play across two PCs on the same network, the host shares their local IP address (for example `192.168.1.20`) and the others type it before Join. Over the internet this needs port forwarding until Steam is added (plan step 6).

## Online play over Steam

Uses Steam's test app (App ID 480, "Spacewar") until Picknick has its own Steam app. Steam must be running and logged in. Steam can't connect an account to itself, so testing needs a second PC with a different Steam account.

1. Make a build to share: **File > Build Profiles > Windows > Build**, zip the output folder and send it to your friend (`steam_appid.txt` is copied in automatically).
2. Host: press Play (or run the build), pick a room in the menu and click **Host on Steam**. Click **Copy** next to "Your code" and send the code to your friend, or click **Invite Steam friends** (the Steam overlay only works in builds, not in the editor).
3. Friend: run the build, paste the code and click **Join friend's code**, or accept the Steam invite while the game is open.

Online, the host picks the rooms (Next room, Restart, Skip); everyone else follows automatically. "End the game for everyone" (host) or "Leave the game" (guest) in the Esc menu goes back to the main menu.

Steam connects you peer to peer through its relays, so no port forwarding is needed. Vendored packages: `Packages/com.rlabrecque.steamworks.net` (Steamworks.NET 2025.165.0) and `Packages/com.community.netcode.transport.steamnetworkingsockets` (community Steam transport for Netcode, asmdef references updated for Netcode 2.x).

## Room 1: The Kitchen Counter

Everyone has to end up in the mouse hole on top of the fridge, at the same time. Falling sends you back to your last checkpoint.

- **A, the can:** the counter is 3 high. Strong pushes the heavy can over, or you stack, or Strong throws people up. Getting Strong up last is the real puzzle.
- **B, the sink gap:** 4 wide, just too far to jump. Feather glides (carrying someone barely makes it), Magnets pull teammates across, or grapple the faucet.
- **C, the cupboard door:** opens only while Strong stands right next to it. Someone on the other side must pull the spoon lever with a Magnet to latch it, or Strong is stuck.
- **D, the fridge:** pull the key off its hook with a Magnet (it's high: stack on the cookie box, get carried or thrown). Reach the fridge top via the fridge magnet or throws, then bring the key to the hatch to open the mouse hole.

## Room 2: The Bathroom

Everyone has to reach the air vent on top of the washing machine.

- **A, the soap:** the gate opens only while something sits on the red floor button. Strong pushes the heavy soap onto it. A creature on the button works too, but then that creature is stuck behind.
- **B, the bathtub:** a step, then the tub rim, then 5 units of water (touching it sends you back). Feather glides, Magnets grapple the shower head and pull the others across.
- **C, the laundry lift:** the lift rises while its button is pressed. A Magnet knocks the metal bucket off its hook and someone pushes it onto the button, so nobody has to stay down. (Or: a Magnet on top pulls the last one up the shaft.)

## Room 3: The Bookshelf

Everyone has to reach the mouse hole at the end of the mantelpiece.

- **A, the bookshelf:** head-jump onto the lower shelf; reach the top board by grappling the coat hook (Magnets) or being thrown (Strong throws Feather). Magnets at the edge pull up whoever is left; two Magnets pull Strong much faster.
- **B, the fish tank:** a 5-wide gap over water. Glide, grapple the lamp, or get pulled across.
- **C, the mirror gate:** opens only while both buttons are pressed. Strong pushes the clock onto one, someone sits on the other, and a Magnet behind the gate pulls the lever to latch it open for the button-sitters.

New pieces for these rooms: `PressurePlate` (any creature or loose object holds it down), `Lift` (rises while its button is pressed), `KillZone` (water) and `HoldDoor.plates`.

## Layout

```
Assets/Picknick/
  Scripts/              Picknick.Runtime assembly
    Creature/           CreatureController, CreatureInput, CreatureTuning, FallRespawn
    CameraRig/          SharedCamera: keeps every creature on screen
    Sandbox/            SandboxBootstrap (builds the test level), LocalInputRouter (test-only input)
    Abilities/          Magnet, Feather, Strong (step 3)
    Puzzle/             Zone, Checkpoint, ExitZone, HoldDoor, MagnetLever, HookedItem, Hatch, PressurePlate, Lift, KillZone
    Rooms/              RoomBase (shared room UI and helpers), KitchenRoom, BathroomRoom, BookshelfRoom (built at runtime)
    Game/               GameFlow (survives scene loads: room order, online session), MainMenu
    Net/                NetSession (host/join over Steam or LAN, input, snapshots, follows the host's room), SyncedBody, SteamService
  Editor/               Picknick.Editor assembly: menu items
  Scenes/ Prefabs/ Rooms/ Settings/
```

## Notes

- **Rendering:** the built-in 2D renderer is used for now. URP and 2D lighting can be added in the art pass (plan step 8).
- **Input:** `CreatureController` never reads devices; it gets a `CreatureInput` each frame. Today `LocalInputRouter` fills it from keyboard and gamepads. Later the owning network client fills it, so the controller stays the same.
- **Physics:** creatures have zero friction so they don't stick to walls or each other. Riding on a moving creature or crate is handled by adding the velocity of whatever you stand on. With no input in the air, momentum is kept, so throws and Magnet pulls (step 3) aren't cancelled. Releasing Jump early only shortens your own jumps, never a throw or a pull.
- **Git LFS:** art and audio files are routed through LFS (`.gitattributes`). Run `git lfs install` once on your machine.
