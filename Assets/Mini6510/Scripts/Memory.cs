// Author: Fabrizio Radica
// Version: 1.0
// Description: 64KB flat memory with memory-mapped register dispatch for the Mini6510 runtime.

using System;
using UnityEngine;

namespace Mini6510
{
    public class Memory
    {
        // 64KB address space
        private readonly byte[] _ram = new byte[0x10000];

        // Memory-mapped register ranges
        public const int VIDEO_RAM_START   = 0x0400;
        public const int VIDEO_RAM_END     = 0x07E7;
        public const int SPRITE_PTR_START  = 0x07F8;
        public const int SPRITE_PTR_END    = 0x07FF;
        public const int COLOR_BUF_START   = 0x0800;
        public const int COLOR_BUF_END     = 0x0BE7;
        public const int PROGRAM_RAM_START = 0x1000;
        public const int CHARSET_START     = 0x2000;
        public const int CHARSET_END       = 0x2FFF;
        public const int SPRITE_POS_START  = 0xD000;
        public const int SPRITE_POS_END    = 0xD00F;
        public const int SPRITE_X_MSB      = 0xD010;
        public const int SPRITE_ENABLE     = 0xD015;
        public const int SPRITE_Y_EXP      = 0xD017;
        public const int SPRITE_X_EXP      = 0xD01D;
        public const int BORDER_COLOR      = 0xD020;
        public const int BG_COLOR          = 0xD021;
        public const int SPRITE_COLOR_START= 0xD027;
        public const int SPRITE_COLOR_END  = 0xD02E;
        public const int RASTER_START      = 0xD100;
        public const int RASTER_END        = 0xD1FF;
        public const int SID_START         = 0xD400;
        public const int SID_END           = 0xD418;
        public const int COLOR_RAM_START   = 0xD800;
        public const int COLOR_RAM_END     = 0xDBE7;
        public const int JOY_PORT2         = 0xDC00;
        public const int JOY_PORT1         = 0xDC01;
        public const int FRAME_COUNTER     = 0xFF00;
        public const int VSYNC_FLAG        = 0xFF01;
        public const int SPRITE_COLLISION  = 0xFF10;

        // Callbacks triggered on write to specific register ranges
        public Action<int, byte> OnSIDWrite;
        public Action<int, byte> OnSpriteWrite;
        public Action<int, byte> OnRasterWrite;
        public Action<byte>      OnBorderColorWrite;
        public Action<byte>      OnBgColorWrite;

        public byte Read(int address)
        {
            return _ram[address & 0xFFFF];
        }

        public void Write(int address, byte value)
        {
            address &= 0xFFFF;
            _ram[address] = value;

            if (address >= SID_START && address <= SID_END)
                OnSIDWrite?.Invoke(address, value);
            else if (address >= SPRITE_POS_START && address <= SPRITE_COLOR_END)
                OnSpriteWrite?.Invoke(address, value);
            else if (address >= RASTER_START && address <= RASTER_END)
                OnRasterWrite?.Invoke(address, value);
            else if (address == BORDER_COLOR)
                OnBorderColorWrite?.Invoke(value);
            else if (address == BG_COLOR)
                OnBgColorWrite?.Invoke(value);
        }

        public void WriteWord(int address, ushort value)
        {
            Write(address,     (byte)(value & 0xFF));
            Write(address + 1, (byte)(value >> 8));
        }

        public ushort ReadWord(int address)
        {
            return (ushort)(Read(address) | (Read(address + 1) << 8));
        }

        public void Clear() => Array.Clear(_ram, 0, _ram.Length);

        public byte[] GetRawBuffer() => _ram;
    }
}
