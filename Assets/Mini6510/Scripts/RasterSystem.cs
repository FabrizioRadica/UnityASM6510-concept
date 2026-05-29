// Author: Fabrizio Radica
// Version: 1.0
// Description: Lightweight raster effect system — copper bars animated per-frame, video synchronized.

using UnityEngine;

namespace Mini6510
{
    // $D100–$D1FF layout (per bar, 8 bytes each → max 32 bars):
    //   +0  Y position (0–199)
    //   +1  Height in scan lines
    //   +2  Color index (C64 palette)
    //   +3  Speed (signed, scroll per frame, stored as two's complement byte)
    //   +4  Active flag (0 = off, 1 = on)
    //   +5–+7 reserved

    public class RasterSystem
    {
        public const int MAX_BARS      = 16;
        public const int BYTES_PER_BAR = 8;

        private readonly Memory _mem;

        // Screen dimensions used for the Y position to pixel mapping (set by Renderer)
        public float ScreenHeight  { get; set; } = 200f;
        public float ScreenOffsetY { get; set; } = 0f;

        public RasterSystem(Memory mem) { _mem = mem; }

        // Called every frame by Mini6510 before rendering
        public void Tick()
        {
            for (int i = 0; i < MAX_BARS; i++)
            {
                int  baseAddr = Memory.RASTER_START + i * BYTES_PER_BAR;
                byte active   = _mem.Read(baseAddr + 4);
                if (active == 0) continue;

                byte y     = _mem.Read(baseAddr);
                sbyte speed = (sbyte)_mem.Read(baseAddr + 3);
                int newY   = (y + speed + 200) % 200;
                _mem.Write(baseAddr, (byte)newY);
            }
        }

        // Draw all active bars onto the screen rect using UnityGUI
        public void Draw(Rect screenRect)
        {
            float scaleY = screenRect.height / ScreenHeight;

            for (int i = 0; i < MAX_BARS; i++)
            {
                int  baseAddr = Memory.RASTER_START + i * BYTES_PER_BAR;
                byte active   = _mem.Read(baseAddr + 4);
                if (active == 0) continue;

                byte y      = _mem.Read(baseAddr);
                byte height = _mem.Read(baseAddr + 1);
                byte color  = _mem.Read(baseAddr + 2);
                if (height == 0) continue;

                float pixY = screenRect.y + y * scaleY;
                float pixH = height * scaleY;

                Color32 c = C64Palette.Get(color);
                GUI.color = c;
                GUI.DrawTexture(new Rect(screenRect.x, pixY, screenRect.width, pixH), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        // Initialize a bar from code (convenience for demo setup)
        public void SetBar(int index, byte y, byte height, byte color, sbyte speed, bool active)
        {
            if (index < 0 || index >= MAX_BARS) return;
            int b = Memory.RASTER_START + index * BYTES_PER_BAR;
            _mem.Write(b + 0, y);
            _mem.Write(b + 1, height);
            _mem.Write(b + 2, color);
            _mem.Write(b + 3, (byte)speed);
            _mem.Write(b + 4, (byte)(active ? 1 : 0));
        }
    }
}
