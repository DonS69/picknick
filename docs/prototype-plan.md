# Picknick — First Prototype Plan

**Scope (Don's MVP):** one room, four players, three abilities (Magnet, Feather, Strong). Unity + C#, online co-op over Steam.
**Goal of the prototype:** prove that the physics is funny and that the puzzle is easy to understand but hard to coordinate. Everything else (Portal, 8 players, 60 rooms, server list backend) waits until four friends have laughed at this one room.

---

## 1. Design rules for every room

1. **Readable at a glance.** The goal (exit) and every interactive object are visible from the start. Metal objects have one colour (blue-grey with a bolt icon), heavy objects another (dark wood/stone with a weight icon). Players should be able to say "you pull that, I push this" within ten seconds.
2. **Hard to coordinate, not hard to figure out.** The difficulty is timing and trust, not hidden logic.
3. **Everyone must reach the exit.** The room is cleared only when all four creatures are inside the exit. This is what forces "go back and help".
4. **Failure is funny and cheap.** Falling resets you to the last checkpoint of your section in under a second, with a squeak. No lives, no game over.
5. **Players are physical.** Creatures can stand on each other's heads, get bumped, get pulled, get thrown. Most of the comedy comes from this.

### Ability swapping (default chosen for the prototype)

The concept says players can change ability any time, but "certain puzzles require specific combinations". If swapping were free and unlimited, one player could solve everything alone. Proposed rule:

- Each room has a fixed set of **ability badges** (Room 1: 2× Magnet, 1× Feather, 1× Strong).
- Every player wears exactly one badge. You change ability by **swapping badges with a teammate** (stand next to them, both press Swap) or by dropping yours at a **badge jar** and taking another one from it.
- This keeps "change any time" while forcing talk ("I need Strong, who has it?") and creates sacrifice moments (giving up Feather means you can't cross back).

This is a design default, easy to change: it is one `AbilityBadge` component and a per-room config.

---

## 2. Room 1: "The Kitchen Counter"

The four creatures wake up on the kitchen floor. The exit is a mouse hole in the wall behind the fridge, reached only from the top of the counter. The room scrolls horizontally with a shared camera that zooms out to keep all players on screen (PICO PARK style).

Starting badges: Player 1 Strong, Player 2 Feather, Players 3–4 Magnet.

### Section A — "The Can" (teaches Strong, stacking)
- A ledge twice as high as a creature can jump.
- A heavy tin can sits nearby. Only Strong can push it.
- **Solution:** Strong pushes the can under the ledge. Creatures jump on the can, then on each other's heads, then up. Strong is last and needs a teammate standing on the ledge edge to jump onto.
- **Funny moment:** the can rolls if pushed too hard and squashes someone back to the checkpoint.

### Section B — "The Sink Gap" (teaches Feather + Magnet)
- A wide gap (the sink) between two counter sections, too far to jump. A metal faucet handle hangs above the far side.
- **Solution:** Feather glides across carrying one player (say a Magnet). On the far side, the Magnet pulls the remaining players across one by one (teammates are pullable). A pulled teammate flies in an arc and falls short unless Feather catches them mid-air or the Magnet times the release.
- **Alternative found by players:** Magnet on the near side latches onto the faucet and pulls *themselves* across (see "mass rule" below). Leaving room for alternatives makes players feel clever.
- **Sacrifice moment:** with only one Feather, the Feather may have to cross three times.

### Section C — "The Cupboard Door" (teaches holding and handing over)
- A heavy cupboard door blocks the path. Strong can hold it open, but the moment Strong lets go it slams shut.
- Behind the door is a metal spoon lever. Pulled down, it latches the door open.
- **Solution:** Strong holds the door, everyone else runs through. A Magnet on the other side pulls the spoon lever down to latch the door, freeing Strong. If the Magnet runs ahead without doing it, Strong is stuck. That's the coordination test.

### Section D — "The Cookie Jar Finale" (all three combined)
- The mouse hole is on top of the fridge, too high for stacking alone.
- A metal key hangs from a hook high above a gap; it opens the fridge-top hatch.
- **Solution (intended):**
  1. Strong throws the Feather up high; Feather glides to the hook level.
  2. Feather carries a Magnet near the key; the Magnet pulls the key off the hook while being carried (Feather sinks faster because of the extra weight, so there's a time window).
  3. The key drops to the floor below; someone carries it to the hatch.
  4. Remaining players stack on a cookie box Strong pushed into place, and Magnets pull the last players up.
- Section D should feel chaotic and take several attempts with a group of friends.

**Target playtime:** 8–15 minutes for a first-time group of four.

### Graybox layout (left to right)

```
 [Start]  A: ledge + can   |  B: sink gap + faucet  |  C: cupboard door + spoon lever  |  D: fridge, key hook, hatch, [Exit]
 floor ------------------- counter ~~~~ sink ~~~~ counter ----------------------------- fridge top
```

Build this in grey boxes first. Art only after the room is fun.

---

## 3. Abilities

All three are components on the same `Creature` prefab; a badge enables one of them. Starting numbers below are tuning values in a ScriptableObject, not final.

Base creature: Rigidbody2D, mass 1, move speed 6 u/s, jump ~1.2 creature-heights. Creatures are solid to each other (you can stand on heads, get bumped).

### 🧲 Magnet
- **Hold** action button: a beam in the aim direction (gamepad stick / mouse), range 6 units, cone ~15°.
- Pulls the first magnetic target it hits: metal objects **and teammates**.
- **Mass rule (the core comedy):** force is applied to both ends. If the target is lighter than the Magnet, the target flies toward you. If the target is heavier or fixed (faucet, hook, fridge), **you** fly toward it. This makes the Magnet a grappling hook for free, and pulling Strong while Strong is anchored yanks the Magnet across the room.
- Release to drop. No repel in the prototype (candidate for later).

### 🪶 Feather
- **Hold jump in the air** to glide: gravity scale drops to ~0.2, horizontal speed is kept.
- **Grab** (action button near a teammate): carry one teammate. While carrying, glide still works but sinks ~3× faster, so carry distance is limited.
- Feather is light (mass 0.5): it gets pulled faster by Magnets and thrown farther by Strong.

### 💪 Strong
- Can push objects tagged **Heavy** (normal creatures bounce off them).
- **Hold** heavy doors and levers open while standing at them.
- **Pick up and throw a teammate** (action button: grab, release: throw in aim direction). Throw strength scales inversely with the teammate's mass.
- Strong is heavy (mass 2): makes a great anchor for a Magnet and a stable bottom of a stack.

### Combo table (what the room teaches and what players will discover)

| Combo | What happens | Used in |
|---|---|---|
| Strong + Feather | Strong throws Feather, Feather glides very far | D |
| Feather + Magnet | Feather carries Magnet, Magnet pulls things mid-air | B, D |
| Magnet + Feather (as target) | Feather player gets yanked across fast, often overshoots | B (funny) |
| Strong + Magnet | Strong stands still as an anchor; Magnet pulling Strong flies to Strong | Discoverable |
| Magnet + fixed metal | Magnet grapples to it | B alternative |
| Anyone + anyone | Stacking on heads | A, D |

Portal is out of scope for the prototype. Note for later: it adds networking complexity (teleporting authority) and deserves its own room design pass.

---

## 4. Networking: the minimal path

### Stack
- **Netcode for GameObjects (NGO)** for replication (NetworkObject, NetworkVariable, RPCs).
- **Facepunch.Steamworks** for Steam API (lobbies, friends, invites) plus **FacepunchTransport** from Unity's Community Network Transports, which runs NGO over Steam Networking Sockets. Steam handles NAT traversal and relays, so no port forwarding and no servers to run.
- Alternative that's equally valid: Mirror + FizzySteamworks. Pick one and don't switch; NGO is the default here because it's Unity's own and well documented.

### Topology
- **Player-hosted (listen server).** One player creates a Steam lobby and is the host; up to 3 friends join via invite or lobby code.
- **Host owns the world:** all puzzle objects (can, door, key, levers), badges, checkpoints, the "everyone at exit" check.
- **Each client owns its own creature's movement** (client-authoritative with NetworkTransform in owner mode). It's co-op, so cheating doesn't matter, and it makes your own character feel instant.
- **Cross-player forces go through the owner.** When a Magnet pulls a teammate or Strong throws one, the acting client asks the host, and the host sends an RPC to the *target's owner*, who applies the force locally. This is the one tricky part and should be built early (step 5 below).
- Physics objects (can, key) are simulated on the host and interpolated on clients. Pushing feels slightly delayed for clients at high ping. Acceptable for the prototype; revisit if playtests complain.

### Steam setup
- Use Steam's test App ID **480** (Spacewar) until you buy your own app on Steamworks (Steam Direct fee, $100).
- Lobby flow: Create lobby → friends join through the Steam overlay invite → host presses Start → NGO starts host/clients over FacepunchTransport using the lobby owner's SteamID.
- The **public server list / backend API / PostgreSQL** from the concept are not needed for the prototype. Steam's lobby list (`RequestLobbyList` with filters) already gives you a browser of public lobbies. Add a backend only if you later need something Steam can't do.

### Develop without Steam first
Build gameplay with NGO's default **Unity Transport on localhost**, using **ParrelSync** (or Unity 6 Multiplayer Play Mode) to run 2–4 editor instances on one machine. Swap the transport component to FacepunchTransport once the room works. The transport is one component, so this costs almost nothing and makes iteration far faster.

### Local co-op (later)
Not in the prototype. Design input with Unity's Input System and `PlayerInput` per creature from day one so local players can be added later without rewriting controls.

---

## 5. Build plan (in order)

Each step ends with something playable. Don't start the next step until the current one feels right.

1. **Project setup.** Unity 6 LTS, 2D URP template, Input System, Git + Git LFS. Folder layout: `Scripts/{Creature,Abilities,Puzzle,Net}`, `Prefabs`, `Rooms`.
2. **One creature, offline.** Movement, jump, stacking on other creatures (test with 4 local dummies). Tune until jumping on heads feels good. This is the foundation of the comedy.
3. **The three abilities, offline.** Magnet (with the mass rule), Feather glide + carry, Strong push + throw. Use a sandbox scene with boxes, metal blocks and a gap. Iterate on feel here. If abilities aren't fun in the sandbox, no puzzle will save them.
4. **Room 1 graybox, offline.** Build sections A–D with grey boxes, checkpoints, and the exit check. Use couch play with 4 controllers (local hack is fine at this stage) to test the puzzle logic.
5. **Networking on localhost.** NGO + Unity Transport, ParrelSync with 4 instances. Replicate creatures, then puzzle objects, then badges and swapping. Build cross-player forces (Magnet pulling a teammate, Strong throwing, Feather carrying) here.
6. **Steam.** Facepunch.Steamworks + FacepunchTransport, App ID 480, create/join lobby via invite. Test with real friends on different networks.
7. **Playtest with four real friends.** Watch, don't explain. Note where they laugh, where they're stuck silently, where one player solves it alone. Fix the room, re-test. Repeat at least three times.
8. **Juice.** Cartoon art for the kitchen, squash-and-stretch, squeaks, screen shake on the can falling, a celebration on exit. Only now.

**Prototype is done when:** four friends on different networks can join over Steam, clear the kitchen in 8–15 minutes, and want to play it again.

### Risks to watch
- **Physics feel over the network.** Stacking and carrying across clients is the hardest part. If it jitters, consider making the carried player's transform follow the carrier (parenting) instead of simulating both.
- **One player solving everything.** If playtests show it, tighten the badge counts or add sections that need two simultaneous actions.
- **Scope creep.** Portal, 8 players, more rooms and the backend all wait until step 7 passes.

---

## 6. Next decisions for Don
- Keep the badge-swapping rule, or allow free swapping any time?
- NGO (default) or Mirror?
- Create a Git repository and a Unity project scaffold (steps 1–2) next?
