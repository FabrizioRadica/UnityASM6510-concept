// Author: Fabrizio Radica
// Version: 1.0
// Description: Typed access to Video RAM (40x25 text screen).

namespace Mini6510
{
    public class VideoRAM
    {
        public const int COLS = 40;
        public const int ROWS = 25;

        private readonly Memory _mem;

        public VideoRAM(Memory mem) { _mem = mem; }

        public byte GetChar(int col, int row)
            => _mem.Read(Memory.VIDEO_RAM_START + row * COLS + col);

        public void SetChar(int col, int row, byte charCode)
            => _mem.Write(Memory.VIDEO_RAM_START + row * COLS + col, charCode);

        public void Clear(byte charCode = 0x20)
        {
            for (int i = 0; i < COLS * ROWS; i++)
                _mem.Write(Memory.VIDEO_RAM_START + i, charCode);
        }
    }
}
