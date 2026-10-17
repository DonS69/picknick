using System.Collections.Generic;
using Picknick.Creature;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Picknick.Sandbox
{
    /// <summary>
    /// Test-only input: the keyboard drives one selected creature (Tab to switch),
    /// and each connected gamepad drives the creature with the same index.
    /// Replaced by per-player input and networking in later steps.
    /// </summary>
    public class LocalInputRouter : MonoBehaviour
    {
        public readonly List<CreatureController> Creatures = new List<CreatureController>();
        public int keyboardIndex;

        public CreatureController KeyboardCreature =>
            Creatures.Count > 0 ? Creatures[keyboardIndex % Creatures.Count] : null;

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame && Creatures.Count > 0)
                keyboardIndex = (keyboardIndex + 1) % Creatures.Count;

            var pads = Gamepad.all;
            for (int i = 0; i < Creatures.Count; i++)
            {
                CreatureInput input = default;
                if (i < pads.Count)
                    Merge(ref input, ReadGamepad(pads[i]));
                if (kb != null && i == keyboardIndex % Creatures.Count)
                    Merge(ref input, ReadKeyboard(kb, Mouse.current, Creatures[i].transform.position));
                Creatures[i].SetInput(input);
            }
        }

        public static CreatureInput ReadKeyboard(Keyboard kb, Mouse mouse, Vector3 creaturePosition)
        {
            float move = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += 1f;

            // Aim with the mouse: direction from the creature to the cursor.
            Vector2 aim = new Vector2(move, 0f);
            Camera cam = Camera.main;
            if (mouse != null && cam != null)
            {
                Vector3 cursor = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                aim = (Vector2)(cursor - creaturePosition);
            }
            bool mouseHeld = mouse != null && mouse.leftButton.isPressed;
            bool mousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

            return new CreatureInput
            {
                Move = move,
                JumpHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed,
                JumpPressed = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame,
                ActionHeld = kb.eKey.isPressed || mouseHeld,
                ActionPressed = kb.eKey.wasPressedThisFrame || mousePressed,
                SwapPressed = kb.qKey.wasPressedThisFrame,
                Aim = aim,
            };
        }

        public static CreatureInput ReadGamepad(Gamepad pad)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            float move = Mathf.Abs(stick.x) > 0.2f ? Mathf.Sign(stick.x) * Mathf.InverseLerp(0.2f, 1f, Mathf.Abs(stick.x)) : 0f;
            if (pad.dpad.left.isPressed) move = -1f;
            if (pad.dpad.right.isPressed) move = 1f;

            return new CreatureInput
            {
                Move = move,
                JumpHeld = pad.buttonSouth.isPressed,
                JumpPressed = pad.buttonSouth.wasPressedThisFrame,
                ActionHeld = pad.buttonWest.isPressed || pad.rightTrigger.isPressed,
                ActionPressed = pad.buttonWest.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame,
                SwapPressed = pad.buttonNorth.wasPressedThisFrame,
                Aim = pad.rightStick.ReadValue(),
            };
        }

        public static void Merge(ref CreatureInput into, CreatureInput from)
        {
            into.Move = Mathf.Clamp(into.Move + from.Move, -1f, 1f);
            into.JumpHeld |= from.JumpHeld;
            into.JumpPressed |= from.JumpPressed;
            into.ActionHeld |= from.ActionHeld;
            into.ActionPressed |= from.ActionPressed;
            into.SwapPressed |= from.SwapPressed;
            if (from.Aim.sqrMagnitude > into.Aim.sqrMagnitude) into.Aim = from.Aim;
        }
    }
}
