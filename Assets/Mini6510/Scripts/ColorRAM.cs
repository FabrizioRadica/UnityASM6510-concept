// Author: Fabrizio Radica
// Version: 1.0
// Description: C64 color palette and Color RAM helpers.

using UnityEngine;

namespace Mini6510
{
    public static class C64Palette
    {
        public static readonly Color32[] Colors = new Color32[16]
        {
            new Color32(  0,   0,   0, 255), // 0  Black
            new Color32(255, 255, 255, 255), // 1  White
            new Color32(136,   0,   0, 255), // 2  Red
            new Color32(170, 255, 238, 255), // 3  Cyan
            new Color32(204,  68, 204, 255), // 4  Purple
            new Color32(  0, 204,  85, 255), // 5  Green
            new Color32(  0,   0, 170, 255), // 6  Blue
            new Color32(238, 238, 119, 255), // 7  Yellow
            new Color32(221, 136,  85, 255), // 8  Orange
            new Color32(102,  68,   0, 255), // 9  Brown
            new Color32(255, 119, 119, 255), // 10 Light Red
            new Color32( 51,  51,  51, 255), // 11 Dark Gray
            new Color32(119, 119, 119, 255), // 12 Medium Gray
            new Color32(170, 255, 102, 255), // 13 Light Green
            new Color32(  0, 136, 255, 255), // 14 Light Blue
            new Color32(187, 187, 187, 255), // 15 Light Gray
        };

        public static Color32 Get(byte index) => Colors[index & 0x0F];
    }

    public class ColorRAM
    {
        private readonly Memory _mem;

        public ColorRAM(Memory mem) { _mem = mem; }

        public Color32 GetForeground(int col, int row)
        {
            int addr = Memory.COLOR_RAM_START + row * 40 + col;
            return C64Palette.Get(_mem.Read(addr));
        }

        public Color32 GetBackground() => C64Palette.Get(_mem.Read(Memory.BG_COLOR));
        public Color32 GetBorder()     => C64Palette.Get(_mem.Read(Memory.BORDER_COLOR));
    }
}
