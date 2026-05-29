// Author: Fabrizio Radica
// Version: 1.0
// Description: Sprite-to-sprite collision result reader (collision detection runs inside SpriteSystem).

namespace Mini6510
{
    public static class CollisionSystem
    {
        // Returns true if sprite index i collided this frame
        public static bool SpriteCollided(Memory mem, int spriteIndex)
        {
            byte reg = mem.Read(Memory.SPRITE_COLLISION);
            return (reg & (1 << spriteIndex)) != 0;
        }

        // Returns the full collision bitmask
        public static byte GetCollisionMask(Memory mem)
            => mem.Read(Memory.SPRITE_COLLISION);
    }
}
