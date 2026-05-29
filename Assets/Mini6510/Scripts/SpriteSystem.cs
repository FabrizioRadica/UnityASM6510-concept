// Author: Fabrizio Radica
// Version: 1.0
// Description: 8 hardware-style sprites using original C64 memory addresses with collision detection.

using UnityEngine;

namespace Mini6510
{
    public class SpriteSystem
    {
        public const int SPRITE_COUNT    = 8;
        public const int SPRITE_WIDTH    = 24; // pixels
        public const int SPRITE_HEIGHT   = 21; // pixels
        public const int SPRITE_BYTES    = 63; // 21 rows × 3 bytes

        private readonly Memory   _mem;
        private readonly Texture2D[] _textures = new Texture2D[SPRITE_COUNT];
        private bool _texturesDirty = true;

        // Collision register: bit N set if sprite N collided with another sprite this frame
        private byte _collisionReg;

        public SpriteSystem(Memory mem)
        {
            _mem = mem;
            for (int i = 0; i < SPRITE_COUNT; i++)
            {
                _textures[i] = new Texture2D(SPRITE_WIDTH, SPRITE_HEIGHT, TextureFormat.RGBA32, false);
                _textures[i].filterMode = FilterMode.Point;
            }

            mem.OnSpriteWrite += (addr, val) => _texturesDirty = true;
        }

        // Call once per frame before Draw()
        public void Tick()
        {
            if (_texturesDirty)
            {
                RebuildTextures();
                _texturesDirty = false;
            }
            DetectCollisions();
            _mem.Write(Memory.SPRITE_COLLISION, _collisionReg);
        }

        public void Draw(Rect screenRect)
        {
            byte enableFlags = _mem.Read(Memory.SPRITE_ENABLE);
            byte xMSB        = _mem.Read(Memory.SPRITE_X_MSB);
            byte xExpand     = _mem.Read(Memory.SPRITE_X_EXP);
            byte yExpand     = _mem.Read(Memory.SPRITE_Y_EXP);

            float scaleX = screenRect.width  / 320f;
            float scaleY = screenRect.height / 200f;

            for (int i = 0; i < SPRITE_COUNT; i++)
            {
                if ((enableFlags & (1 << i)) == 0) continue;

                int rawX = _mem.Read(Memory.SPRITE_POS_START + i * 2);
                if ((xMSB & (1 << i)) != 0) rawX += 256;
                int rawY = _mem.Read(Memory.SPRITE_POS_START + i * 2 + 1);

                bool xExp = (xExpand & (1 << i)) != 0;
                bool yExp = (yExpand & (1 << i)) != 0;

                float pw = SPRITE_WIDTH  * (xExp ? 2f : 1f) * scaleX;
                float ph = SPRITE_HEIGHT * (yExp ? 2f : 1f) * scaleY;
                float px = screenRect.x + rawX * scaleX;
                float py = screenRect.y + rawY * scaleY;

                GUI.DrawTexture(new Rect(px, py, pw, ph), _textures[i]);
            }
        }

        private void RebuildTextures()
        {
            for (int i = 0; i < SPRITE_COUNT; i++)
            {
                // Sprite pointer at $07F8 + i → block number (multiply by 64 to get RAM address)
                byte ptr      = _mem.Read(Memory.SPRITE_PTR_START + i);
                int  dataAddr = ptr * 64;
                byte color    = _mem.Read(Memory.SPRITE_COLOR_START + i);
                Color32 c     = C64Palette.Get(color);
                Color32 trans = new Color32(0, 0, 0, 0);

                var pixels = new Color32[SPRITE_WIDTH * SPRITE_HEIGHT];

                for (int row = 0; row < SPRITE_HEIGHT; row++)
                {
                    for (int byteIdx = 0; byteIdx < 3; byteIdx++)
                    {
                        byte b = _mem.Read(dataAddr + row * 3 + byteIdx);
                        for (int bit = 7; bit >= 0; bit--)
                        {
                            int col = byteIdx * 8 + (7 - bit);
                            // Flip vertically so row 0 is at top in Unity GUI coordinates
                            int pixIdx = (SPRITE_HEIGHT - 1 - row) * SPRITE_WIDTH + col;
                            pixels[pixIdx] = (b & (1 << bit)) != 0 ? c : trans;
                        }
                    }
                }

                _textures[i].SetPixels32(pixels);
                _textures[i].Apply();
            }
        }

        private void DetectCollisions()
        {
            byte enableFlags = _mem.Read(Memory.SPRITE_ENABLE);
            _collisionReg    = 0;

            for (int a = 0; a < SPRITE_COUNT; a++)
            {
                if ((enableFlags & (1 << a)) == 0) continue;
                Rect ra = GetSpriteRect(a);

                for (int b = a + 1; b < SPRITE_COUNT; b++)
                {
                    if ((enableFlags & (1 << b)) == 0) continue;
                    Rect rb = GetSpriteRect(b);

                    if (ra.Overlaps(rb))
                    {
                        _collisionReg |= (byte)(1 << a);
                        _collisionReg |= (byte)(1 << b);
                    }
                }
            }
        }

        private Rect GetSpriteRect(int i)
        {
            byte xMSB = _mem.Read(Memory.SPRITE_X_MSB);
            byte xExp = _mem.Read(Memory.SPRITE_X_EXP);
            byte yExp = _mem.Read(Memory.SPRITE_Y_EXP);

            int rawX = _mem.Read(Memory.SPRITE_POS_START + i * 2);
            if ((xMSB & (1 << i)) != 0) rawX += 256;
            int rawY = _mem.Read(Memory.SPRITE_POS_START + i * 2 + 1);

            float w = SPRITE_WIDTH  * ((xExp & (1 << i)) != 0 ? 2f : 1f);
            float h = SPRITE_HEIGHT * ((yExp & (1 << i)) != 0 ? 2f : 1f);
            return new Rect(rawX, rawY, w, h);
        }

        public void Dispose()
        {
            foreach (var t in _textures)
                if (t != null) Object.Destroy(t);
        }
    }
}
