// Author: Fabrizio Radica
// Version: 1.0
// Description: Complete MOS 6510 opcode table — no illegal opcodes.

using System.Collections.Generic;

namespace Mini6510
{
    public enum AddressMode
    {
        Implied, Accumulator, Immediate,
        ZeroPage, ZeroPageX, ZeroPageY,
        Absolute, AbsoluteX, AbsoluteY,
        Indirect, IndirectX, IndirectY,
        Relative
    }

    public struct OpcodeInfo
    {
        public string    Mnemonic;
        public AddressMode Mode;
        public int       Cycles;

        public OpcodeInfo(string mnemonic, AddressMode mode, int cycles)
        {
            Mnemonic = mnemonic;
            Mode     = mode;
            Cycles   = cycles;
        }
    }

    public static class OpcodeTable
    {
        public static readonly Dictionary<byte, OpcodeInfo> Table = new Dictionary<byte, OpcodeInfo>
        {
            // BRK / NOP / RTI / RTS
            { 0x00, new OpcodeInfo("BRK", AddressMode.Implied,     7) },
            { 0xEA, new OpcodeInfo("NOP", AddressMode.Implied,     2) },
            { 0x40, new OpcodeInfo("RTI", AddressMode.Implied,     6) },
            { 0x60, new OpcodeInfo("RTS", AddressMode.Implied,     6) },

            // LDA
            { 0xA9, new OpcodeInfo("LDA", AddressMode.Immediate,   2) },
            { 0xA5, new OpcodeInfo("LDA", AddressMode.ZeroPage,    3) },
            { 0xB5, new OpcodeInfo("LDA", AddressMode.ZeroPageX,   4) },
            { 0xAD, new OpcodeInfo("LDA", AddressMode.Absolute,    4) },
            { 0xBD, new OpcodeInfo("LDA", AddressMode.AbsoluteX,   4) },
            { 0xB9, new OpcodeInfo("LDA", AddressMode.AbsoluteY,   4) },
            { 0xA1, new OpcodeInfo("LDA", AddressMode.IndirectX,   6) },
            { 0xB1, new OpcodeInfo("LDA", AddressMode.IndirectY,   5) },

            // LDX
            { 0xA2, new OpcodeInfo("LDX", AddressMode.Immediate,   2) },
            { 0xA6, new OpcodeInfo("LDX", AddressMode.ZeroPage,    3) },
            { 0xB6, new OpcodeInfo("LDX", AddressMode.ZeroPageY,   4) },
            { 0xAE, new OpcodeInfo("LDX", AddressMode.Absolute,    4) },
            { 0xBE, new OpcodeInfo("LDX", AddressMode.AbsoluteY,   4) },

            // LDY
            { 0xA0, new OpcodeInfo("LDY", AddressMode.Immediate,   2) },
            { 0xA4, new OpcodeInfo("LDY", AddressMode.ZeroPage,    3) },
            { 0xB4, new OpcodeInfo("LDY", AddressMode.ZeroPageX,   4) },
            { 0xAC, new OpcodeInfo("LDY", AddressMode.Absolute,    4) },
            { 0xBC, new OpcodeInfo("LDY", AddressMode.AbsoluteX,   4) },

            // STA
            { 0x85, new OpcodeInfo("STA", AddressMode.ZeroPage,    3) },
            { 0x95, new OpcodeInfo("STA", AddressMode.ZeroPageX,   4) },
            { 0x8D, new OpcodeInfo("STA", AddressMode.Absolute,    4) },
            { 0x9D, new OpcodeInfo("STA", AddressMode.AbsoluteX,   5) },
            { 0x99, new OpcodeInfo("STA", AddressMode.AbsoluteY,   5) },
            { 0x81, new OpcodeInfo("STA", AddressMode.IndirectX,   6) },
            { 0x91, new OpcodeInfo("STA", AddressMode.IndirectY,   6) },

            // STX
            { 0x86, new OpcodeInfo("STX", AddressMode.ZeroPage,    3) },
            { 0x96, new OpcodeInfo("STX", AddressMode.ZeroPageY,   4) },
            { 0x8E, new OpcodeInfo("STX", AddressMode.Absolute,    4) },

            // STY
            { 0x84, new OpcodeInfo("STY", AddressMode.ZeroPage,    3) },
            { 0x94, new OpcodeInfo("STY", AddressMode.ZeroPageX,   4) },
            { 0x8C, new OpcodeInfo("STY", AddressMode.Absolute,    4) },

            // TAX / TAY / TXA / TYA / TSX / TXS
            { 0xAA, new OpcodeInfo("TAX", AddressMode.Implied,     2) },
            { 0xA8, new OpcodeInfo("TAY", AddressMode.Implied,     2) },
            { 0x8A, new OpcodeInfo("TXA", AddressMode.Implied,     2) },
            { 0x98, new OpcodeInfo("TYA", AddressMode.Implied,     2) },
            { 0xBA, new OpcodeInfo("TSX", AddressMode.Implied,     2) },
            { 0x9A, new OpcodeInfo("TXS", AddressMode.Implied,     2) },

            // PHA / PLA / PHP / PLP
            { 0x48, new OpcodeInfo("PHA", AddressMode.Implied,     3) },
            { 0x68, new OpcodeInfo("PLA", AddressMode.Implied,     4) },
            { 0x08, new OpcodeInfo("PHP", AddressMode.Implied,     3) },
            { 0x28, new OpcodeInfo("PLP", AddressMode.Implied,     4) },

            // ADC
            { 0x69, new OpcodeInfo("ADC", AddressMode.Immediate,   2) },
            { 0x65, new OpcodeInfo("ADC", AddressMode.ZeroPage,    3) },
            { 0x75, new OpcodeInfo("ADC", AddressMode.ZeroPageX,   4) },
            { 0x6D, new OpcodeInfo("ADC", AddressMode.Absolute,    4) },
            { 0x7D, new OpcodeInfo("ADC", AddressMode.AbsoluteX,   4) },
            { 0x79, new OpcodeInfo("ADC", AddressMode.AbsoluteY,   4) },
            { 0x61, new OpcodeInfo("ADC", AddressMode.IndirectX,   6) },
            { 0x71, new OpcodeInfo("ADC", AddressMode.IndirectY,   5) },

            // SBC
            { 0xE9, new OpcodeInfo("SBC", AddressMode.Immediate,   2) },
            { 0xE5, new OpcodeInfo("SBC", AddressMode.ZeroPage,    3) },
            { 0xF5, new OpcodeInfo("SBC", AddressMode.ZeroPageX,   4) },
            { 0xED, new OpcodeInfo("SBC", AddressMode.Absolute,    4) },
            { 0xFD, new OpcodeInfo("SBC", AddressMode.AbsoluteX,   4) },
            { 0xF9, new OpcodeInfo("SBC", AddressMode.AbsoluteY,   4) },
            { 0xE1, new OpcodeInfo("SBC", AddressMode.IndirectX,   6) },
            { 0xF1, new OpcodeInfo("SBC", AddressMode.IndirectY,   5) },

            // INC / DEC
            { 0xE6, new OpcodeInfo("INC", AddressMode.ZeroPage,    5) },
            { 0xF6, new OpcodeInfo("INC", AddressMode.ZeroPageX,   6) },
            { 0xEE, new OpcodeInfo("INC", AddressMode.Absolute,    6) },
            { 0xFE, new OpcodeInfo("INC", AddressMode.AbsoluteX,   7) },
            { 0xC6, new OpcodeInfo("DEC", AddressMode.ZeroPage,    5) },
            { 0xD6, new OpcodeInfo("DEC", AddressMode.ZeroPageX,   6) },
            { 0xCE, new OpcodeInfo("DEC", AddressMode.Absolute,    6) },
            { 0xDE, new OpcodeInfo("DEC", AddressMode.AbsoluteX,   7) },

            // INX / INY / DEX / DEY
            { 0xE8, new OpcodeInfo("INX", AddressMode.Implied,     2) },
            { 0xC8, new OpcodeInfo("INY", AddressMode.Implied,     2) },
            { 0xCA, new OpcodeInfo("DEX", AddressMode.Implied,     2) },
            { 0x88, new OpcodeInfo("DEY", AddressMode.Implied,     2) },

            // AND
            { 0x29, new OpcodeInfo("AND", AddressMode.Immediate,   2) },
            { 0x25, new OpcodeInfo("AND", AddressMode.ZeroPage,    3) },
            { 0x35, new OpcodeInfo("AND", AddressMode.ZeroPageX,   4) },
            { 0x2D, new OpcodeInfo("AND", AddressMode.Absolute,    4) },
            { 0x3D, new OpcodeInfo("AND", AddressMode.AbsoluteX,   4) },
            { 0x39, new OpcodeInfo("AND", AddressMode.AbsoluteY,   4) },
            { 0x21, new OpcodeInfo("AND", AddressMode.IndirectX,   6) },
            { 0x31, new OpcodeInfo("AND", AddressMode.IndirectY,   5) },

            // ORA
            { 0x09, new OpcodeInfo("ORA", AddressMode.Immediate,   2) },
            { 0x05, new OpcodeInfo("ORA", AddressMode.ZeroPage,    3) },
            { 0x15, new OpcodeInfo("ORA", AddressMode.ZeroPageX,   4) },
            { 0x0D, new OpcodeInfo("ORA", AddressMode.Absolute,    4) },
            { 0x1D, new OpcodeInfo("ORA", AddressMode.AbsoluteX,   4) },
            { 0x19, new OpcodeInfo("ORA", AddressMode.AbsoluteY,   4) },
            { 0x01, new OpcodeInfo("ORA", AddressMode.IndirectX,   6) },
            { 0x11, new OpcodeInfo("ORA", AddressMode.IndirectY,   5) },

            // EOR
            { 0x49, new OpcodeInfo("EOR", AddressMode.Immediate,   2) },
            { 0x45, new OpcodeInfo("EOR", AddressMode.ZeroPage,    3) },
            { 0x55, new OpcodeInfo("EOR", AddressMode.ZeroPageX,   4) },
            { 0x4D, new OpcodeInfo("EOR", AddressMode.Absolute,    4) },
            { 0x5D, new OpcodeInfo("EOR", AddressMode.AbsoluteX,   4) },
            { 0x59, new OpcodeInfo("EOR", AddressMode.AbsoluteY,   4) },
            { 0x41, new OpcodeInfo("EOR", AddressMode.IndirectX,   6) },
            { 0x51, new OpcodeInfo("EOR", AddressMode.IndirectY,   5) },

            // BIT
            { 0x24, new OpcodeInfo("BIT", AddressMode.ZeroPage,    3) },
            { 0x2C, new OpcodeInfo("BIT", AddressMode.Absolute,    4) },

            // ASL
            { 0x0A, new OpcodeInfo("ASL", AddressMode.Accumulator, 2) },
            { 0x06, new OpcodeInfo("ASL", AddressMode.ZeroPage,    5) },
            { 0x16, new OpcodeInfo("ASL", AddressMode.ZeroPageX,   6) },
            { 0x0E, new OpcodeInfo("ASL", AddressMode.Absolute,    6) },
            { 0x1E, new OpcodeInfo("ASL", AddressMode.AbsoluteX,   7) },

            // LSR
            { 0x4A, new OpcodeInfo("LSR", AddressMode.Accumulator, 2) },
            { 0x46, new OpcodeInfo("LSR", AddressMode.ZeroPage,    5) },
            { 0x56, new OpcodeInfo("LSR", AddressMode.ZeroPageX,   6) },
            { 0x4E, new OpcodeInfo("LSR", AddressMode.Absolute,    6) },
            { 0x5E, new OpcodeInfo("LSR", AddressMode.AbsoluteX,   7) },

            // ROL
            { 0x2A, new OpcodeInfo("ROL", AddressMode.Accumulator, 2) },
            { 0x26, new OpcodeInfo("ROL", AddressMode.ZeroPage,    5) },
            { 0x36, new OpcodeInfo("ROL", AddressMode.ZeroPageX,   6) },
            { 0x2E, new OpcodeInfo("ROL", AddressMode.Absolute,    6) },
            { 0x3E, new OpcodeInfo("ROL", AddressMode.AbsoluteX,   7) },

            // ROR
            { 0x6A, new OpcodeInfo("ROR", AddressMode.Accumulator, 2) },
            { 0x66, new OpcodeInfo("ROR", AddressMode.ZeroPage,    5) },
            { 0x76, new OpcodeInfo("ROR", AddressMode.ZeroPageX,   6) },
            { 0x6E, new OpcodeInfo("ROR", AddressMode.Absolute,    6) },
            { 0x7E, new OpcodeInfo("ROR", AddressMode.AbsoluteX,   7) },

            // CMP
            { 0xC9, new OpcodeInfo("CMP", AddressMode.Immediate,   2) },
            { 0xC5, new OpcodeInfo("CMP", AddressMode.ZeroPage,    3) },
            { 0xD5, new OpcodeInfo("CMP", AddressMode.ZeroPageX,   4) },
            { 0xCD, new OpcodeInfo("CMP", AddressMode.Absolute,    4) },
            { 0xDD, new OpcodeInfo("CMP", AddressMode.AbsoluteX,   4) },
            { 0xD9, new OpcodeInfo("CMP", AddressMode.AbsoluteY,   4) },
            { 0xC1, new OpcodeInfo("CMP", AddressMode.IndirectX,   6) },
            { 0xD1, new OpcodeInfo("CMP", AddressMode.IndirectY,   5) },

            // CPX
            { 0xE0, new OpcodeInfo("CPX", AddressMode.Immediate,   2) },
            { 0xE4, new OpcodeInfo("CPX", AddressMode.ZeroPage,    3) },
            { 0xEC, new OpcodeInfo("CPX", AddressMode.Absolute,    4) },

            // CPY
            { 0xC0, new OpcodeInfo("CPY", AddressMode.Immediate,   2) },
            { 0xC4, new OpcodeInfo("CPY", AddressMode.ZeroPage,    3) },
            { 0xCC, new OpcodeInfo("CPY", AddressMode.Absolute,    4) },

            // JMP / JSR
            { 0x4C, new OpcodeInfo("JMP", AddressMode.Absolute,    3) },
            { 0x6C, new OpcodeInfo("JMP", AddressMode.Indirect,    5) },
            { 0x20, new OpcodeInfo("JSR", AddressMode.Absolute,    6) },

            // Branches
            { 0x90, new OpcodeInfo("BCC", AddressMode.Relative,    2) },
            { 0xB0, new OpcodeInfo("BCS", AddressMode.Relative,    2) },
            { 0xF0, new OpcodeInfo("BEQ", AddressMode.Relative,    2) },
            { 0xD0, new OpcodeInfo("BNE", AddressMode.Relative,    2) },
            { 0x30, new OpcodeInfo("BMI", AddressMode.Relative,    2) },
            { 0x10, new OpcodeInfo("BPL", AddressMode.Relative,    2) },
            { 0x50, new OpcodeInfo("BVC", AddressMode.Relative,    2) },
            { 0x70, new OpcodeInfo("BVS", AddressMode.Relative,    2) },

            // Flag ops
            { 0x18, new OpcodeInfo("CLC", AddressMode.Implied,     2) },
            { 0x38, new OpcodeInfo("SEC", AddressMode.Implied,     2) },
            { 0xD8, new OpcodeInfo("CLD", AddressMode.Implied,     2) },
            { 0xF8, new OpcodeInfo("SED", AddressMode.Implied,     2) },
            { 0x58, new OpcodeInfo("CLI", AddressMode.Implied,     2) },
            { 0x78, new OpcodeInfo("SEI", AddressMode.Implied,     2) },
            { 0xB8, new OpcodeInfo("CLV", AddressMode.Implied,     2) },
        };
    }
}
