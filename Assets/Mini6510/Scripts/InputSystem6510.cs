// Author: Fabrizio Radica
// Version: 1.0
// Description: Joystick input mapped to C64 addresses $DC00/$DC01.
//              Supports keyboard, Xbox, PlayStation, and generic gamepads.
//              Bit 0=Up, 1=Down, 2=Left, 3=Right, 4=Fire (0=pressed, C64 convention).

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Mini6510
{
    public class InputSystem6510
    {
        private readonly Memory _mem;

        // Deadzone for analog sticks
        private const float DEADZONE = 0.3f;

        public InputSystem6510(Memory mem) { _mem = mem; }

        public void Tick()
        {
            _mem.Write(Memory.JOY_PORT2, ReadJoystick(playerIndex: 0));
            _mem.Write(Memory.JOY_PORT1, ReadJoystick(playerIndex: 1));
        }

        private byte ReadJoystick(int playerIndex)
        {
            // All bits high = nothing pressed (C64 active-low convention)
            byte result = 0xFF;

            // ── Keyboard (port 2 only on player 0) ───────────────────────────────
            if (playerIndex == 0)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    result &= 0xFE;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  result &= 0xFD;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  result &= 0xFB;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) result &= 0xF7;
                    if (kb.spaceKey.isPressed || kb.leftCtrlKey.isPressed) result &= 0xEF;
                }
            }

            // ── Gamepad ───────────────────────────────────────────────────────────
            var gamepads = Gamepad.all;
            if (playerIndex < gamepads.Count)
            {
                var gp = gamepads[playerIndex];

                // D-pad
                if (gp.dpad.up.isPressed)    result &= 0xFE;
                if (gp.dpad.down.isPressed)  result &= 0xFD;
                if (gp.dpad.left.isPressed)  result &= 0xFB;
                if (gp.dpad.right.isPressed) result &= 0xF7;

                // Left stick
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.y >  DEADZONE) result &= 0xFE;
                if (stick.y < -DEADZONE) result &= 0xFD;
                if (stick.x < -DEADZONE) result &= 0xFB;
                if (stick.x >  DEADZONE) result &= 0xF7;

                // Fire: South button (A on Xbox, Cross on PS)
                if (gp.buttonSouth.isPressed) result &= 0xEF;
            }

            return result;
        }
    }
}
