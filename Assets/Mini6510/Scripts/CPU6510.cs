// Author: Fabrizio Radica
// Version: 1.0
// Description: MOS 6510 CPU core — complete official instruction set, no illegal opcodes.

using System;
using UnityEngine;

namespace Mini6510
{
    public class CPU6510
    {
        // Registers
        public byte  A  { get; private set; }
        public byte  X  { get; private set; }
        public byte  Y  { get; private set; }
        public byte  SP { get; private set; }
        public ushort PC { get; private set; }

        // Status flags
        public bool FlagC { get; private set; } // Carry
        public bool FlagZ { get; private set; } // Zero
        public bool FlagI { get; private set; } // Interrupt disable
        public bool FlagD { get; private set; } // Decimal (ignored, not emulated)
        public bool FlagB { get; private set; } // Break
        public bool FlagV { get; private set; } // Overflow
        public bool FlagN { get; private set; } // Negative

        private Memory _mem;
        public bool Halted { get; private set; }

        public CPU6510(Memory mem)
        {
            _mem = mem;
            Reset();
        }

        public void Reset()
        {
            A  = 0; X = 0; Y = 0;
            SP = 0xFF;
            PC = Memory.PROGRAM_RAM_START;
            FlagI = true;
            Halted = false;
        }

        public void SetPC(ushort addr) => PC = addr;

        // Execute one instruction, return cycles consumed
        public int Step()
        {
            if (Halted) return 0;

            byte opcode = Fetch();

            if (!OpcodeTable.Table.TryGetValue(opcode, out OpcodeInfo info))
            {
                Debug.LogWarning($"[CPU6510] Unknown opcode 0x{opcode:X2} at PC=0x{(PC-1):X4}");
                return 2;
            }

            int cycles = info.Cycles;
            ExecuteOpcode(info.Mnemonic, info.Mode, ref cycles);
            return cycles;
        }

        // ── Fetch helpers ────────────────────────────────────────────────────────

        private byte Fetch()
        {
            byte v = _mem.Read(PC);
            PC++;
            return v;
        }

        private ushort FetchWord()
        {
            byte lo = Fetch();
            byte hi = Fetch();
            return (ushort)((hi << 8) | lo);
        }

        // ── Address resolution ───────────────────────────────────────────────────

        private int ResolveAddress(AddressMode mode, ref int cycles)
        {
            switch (mode)
            {
                case AddressMode.ZeroPage:   return Fetch();
                case AddressMode.ZeroPageX:  return (Fetch() + X) & 0xFF;
                case AddressMode.ZeroPageY:  return (Fetch() + Y) & 0xFF;
                case AddressMode.Absolute:   return FetchWord();
                case AddressMode.AbsoluteX:
                {
                    int base_ = FetchWord();
                    int addr  = (base_ + X) & 0xFFFF;
                    if ((base_ & 0xFF00) != (addr & 0xFF00)) cycles++;
                    return addr;
                }
                case AddressMode.AbsoluteY:
                {
                    int base_ = FetchWord();
                    int addr  = (base_ + Y) & 0xFFFF;
                    if ((base_ & 0xFF00) != (addr & 0xFF00)) cycles++;
                    return addr;
                }
                case AddressMode.Indirect:
                {
                    int ptr = FetchWord();
                    // 6502 page-wrap bug reproduced
                    int lo  = _mem.Read(ptr);
                    int hi  = _mem.Read((ptr & 0xFF00) | ((ptr + 1) & 0x00FF));
                    return (hi << 8) | lo;
                }
                case AddressMode.IndirectX:
                {
                    int zp = (Fetch() + X) & 0xFF;
                    return _mem.ReadWord(zp);
                }
                case AddressMode.IndirectY:
                {
                    int zp   = Fetch();
                    int base_ = _mem.ReadWord(zp);
                    int addr  = (base_ + Y) & 0xFFFF;
                    if ((base_ & 0xFF00) != (addr & 0xFF00)) cycles++;
                    return addr;
                }
                default: return 0;
            }
        }

        private byte ReadOperand(AddressMode mode, ref int cycles)
        {
            if (mode == AddressMode.Immediate)   return Fetch();
            if (mode == AddressMode.Accumulator) return A;
            return _mem.Read(ResolveAddress(mode, ref cycles));
        }

        // ── Stack helpers ────────────────────────────────────────────────────────

        private void Push(byte v) { _mem.Write(0x0100 + SP, v); SP--; }
        private byte Pop()        { SP++; return _mem.Read(0x0100 + SP); }

        private void PushWord(ushort v)
        {
            Push((byte)(v >> 8));
            Push((byte)(v & 0xFF));
        }

        private ushort PopWord()
        {
            byte lo = Pop();
            byte hi = Pop();
            return (ushort)((hi << 8) | lo);
        }

        // ── Flag helpers ─────────────────────────────────────────────────────────

        private byte GetP()
        {
            byte p = 0x20; // bit 5 always set
            if (FlagN) p |= 0x80;
            if (FlagV) p |= 0x40;
            if (FlagB) p |= 0x10;
            if (FlagD) p |= 0x08;
            if (FlagI) p |= 0x04;
            if (FlagZ) p |= 0x02;
            if (FlagC) p |= 0x01;
            return p;
        }

        private void SetP(byte p)
        {
            FlagN = (p & 0x80) != 0;
            FlagV = (p & 0x40) != 0;
            FlagB = (p & 0x10) != 0;
            FlagD = (p & 0x08) != 0;
            FlagI = (p & 0x04) != 0;
            FlagZ = (p & 0x02) != 0;
            FlagC = (p & 0x01) != 0;
        }

        private void SetNZ(byte v) { FlagN = (v & 0x80) != 0; FlagZ = v == 0; }

        // ── Branch helper ────────────────────────────────────────────────────────

        private void Branch(bool cond, ref int cycles)
        {
            sbyte offset = (sbyte)Fetch();
            if (!cond) return;
            cycles++;
            int oldPC = PC;
            PC = (ushort)(PC + offset);
            if ((oldPC & 0xFF00) != (PC & 0xFF00)) cycles++;
        }

        // ── Main dispatch ────────────────────────────────────────────────────────

        private void ExecuteOpcode(string mnemonic, AddressMode mode, ref int cycles)
        {
            int addr;
            byte v;

            switch (mnemonic)
            {
                // Load/Store
                case "LDA": A = ReadOperand(mode, ref cycles); SetNZ(A); break;
                case "LDX": X = ReadOperand(mode, ref cycles); SetNZ(X); break;
                case "LDY": Y = ReadOperand(mode, ref cycles); SetNZ(Y); break;

                case "STA":
                    addr = ResolveAddress(mode, ref cycles);
                    _mem.Write(addr, A);
                    break;
                case "STX":
                    addr = ResolveAddress(mode, ref cycles);
                    _mem.Write(addr, X);
                    break;
                case "STY":
                    addr = ResolveAddress(mode, ref cycles);
                    _mem.Write(addr, Y);
                    break;

                // Transfers
                case "TAX": X = A; SetNZ(X); break;
                case "TAY": Y = A; SetNZ(Y); break;
                case "TXA": A = X; SetNZ(A); break;
                case "TYA": A = Y; SetNZ(A); break;
                case "TSX": X = SP; SetNZ(X); break;
                case "TXS": SP = X; break;

                // Stack
                case "PHA": Push(A); break;
                case "PLA": A = Pop(); SetNZ(A); break;
                case "PHP": Push(GetP()); break;
                case "PLP": SetP(Pop()); break;

                // ADC
                case "ADC":
                {
                    v = ReadOperand(mode, ref cycles);
                    int r = A + v + (FlagC ? 1 : 0);
                    FlagV = (~(A ^ v) & (A ^ r) & 0x80) != 0;
                    FlagC = r > 0xFF;
                    A     = (byte)r;
                    SetNZ(A);
                    break;
                }

                // SBC
                case "SBC":
                {
                    v = ReadOperand(mode, ref cycles);
                    int r = A - v - (FlagC ? 0 : 1);
                    FlagV = ((A ^ v) & (A ^ r) & 0x80) != 0;
                    FlagC = r >= 0;
                    A     = (byte)r;
                    SetNZ(A);
                    break;
                }

                // INC / DEC
                case "INC":
                    addr = ResolveAddress(mode, ref cycles);
                    v    = (byte)(_mem.Read(addr) + 1);
                    _mem.Write(addr, v);
                    SetNZ(v);
                    break;
                case "DEC":
                    addr = ResolveAddress(mode, ref cycles);
                    v    = (byte)(_mem.Read(addr) - 1);
                    _mem.Write(addr, v);
                    SetNZ(v);
                    break;

                case "INX": X++; SetNZ(X); break;
                case "INY": Y++; SetNZ(Y); break;
                case "DEX": X--; SetNZ(X); break;
                case "DEY": Y--; SetNZ(Y); break;

                // Logical
                case "AND": A &= ReadOperand(mode, ref cycles); SetNZ(A); break;
                case "ORA": A |= ReadOperand(mode, ref cycles); SetNZ(A); break;
                case "EOR": A ^= ReadOperand(mode, ref cycles); SetNZ(A); break;

                case "BIT":
                {
                    v = ReadOperand(mode, ref cycles);
                    FlagZ = (A & v) == 0;
                    FlagN = (v & 0x80) != 0;
                    FlagV = (v & 0x40) != 0;
                    break;
                }

                // Shifts
                case "ASL":
                    if (mode == AddressMode.Accumulator)
                    {
                        FlagC = (A & 0x80) != 0;
                        A   <<= 1;
                        SetNZ(A);
                    }
                    else
                    {
                        addr = ResolveAddress(mode, ref cycles);
                        v    = _mem.Read(addr);
                        FlagC = (v & 0x80) != 0;
                        v   <<= 1;
                        _mem.Write(addr, v);
                        SetNZ(v);
                    }
                    break;

                case "LSR":
                    if (mode == AddressMode.Accumulator)
                    {
                        FlagC = (A & 0x01) != 0;
                        A   >>= 1;
                        SetNZ(A);
                    }
                    else
                    {
                        addr = ResolveAddress(mode, ref cycles);
                        v    = _mem.Read(addr);
                        FlagC = (v & 0x01) != 0;
                        v   >>= 1;
                        _mem.Write(addr, v);
                        SetNZ(v);
                    }
                    break;

                case "ROL":
                    if (mode == AddressMode.Accumulator)
                    {
                        bool oldC = FlagC;
                        FlagC = (A & 0x80) != 0;
                        A     = (byte)((A << 1) | (oldC ? 1 : 0));
                        SetNZ(A);
                    }
                    else
                    {
                        addr      = ResolveAddress(mode, ref cycles);
                        v         = _mem.Read(addr);
                        bool oldC = FlagC;
                        FlagC     = (v & 0x80) != 0;
                        v         = (byte)((v << 1) | (oldC ? 1 : 0));
                        _mem.Write(addr, v);
                        SetNZ(v);
                    }
                    break;

                case "ROR":
                    if (mode == AddressMode.Accumulator)
                    {
                        bool oldC = FlagC;
                        FlagC = (A & 0x01) != 0;
                        A     = (byte)((A >> 1) | (oldC ? 0x80 : 0));
                        SetNZ(A);
                    }
                    else
                    {
                        addr      = ResolveAddress(mode, ref cycles);
                        v         = _mem.Read(addr);
                        bool oldC = FlagC;
                        FlagC     = (v & 0x01) != 0;
                        v         = (byte)((v >> 1) | (oldC ? 0x80 : 0));
                        _mem.Write(addr, v);
                        SetNZ(v);
                    }
                    break;

                // Compare
                case "CMP":
                    v     = ReadOperand(mode, ref cycles);
                    FlagC = A >= v;
                    SetNZ((byte)(A - v));
                    break;
                case "CPX":
                    v     = ReadOperand(mode, ref cycles);
                    FlagC = X >= v;
                    SetNZ((byte)(X - v));
                    break;
                case "CPY":
                    v     = ReadOperand(mode, ref cycles);
                    FlagC = Y >= v;
                    SetNZ((byte)(Y - v));
                    break;

                // Jump/Call
                case "JMP":
                    PC = (ushort)ResolveAddress(mode, ref cycles);
                    break;
                case "JSR":
                    addr = ResolveAddress(mode, ref cycles);
                    PushWord((ushort)(PC - 1));
                    PC   = (ushort)addr;
                    break;
                case "RTS":
                    PC = (ushort)(PopWord() + 1);
                    break;
                case "RTI":
                    SetP(Pop());
                    PC = PopWord();
                    break;

                // Branches
                case "BCC": Branch(!FlagC, ref cycles); break;
                case "BCS": Branch( FlagC, ref cycles); break;
                case "BEQ": Branch( FlagZ, ref cycles); break;
                case "BNE": Branch(!FlagZ, ref cycles); break;
                case "BMI": Branch( FlagN, ref cycles); break;
                case "BPL": Branch(!FlagN, ref cycles); break;
                case "BVC": Branch(!FlagV, ref cycles); break;
                case "BVS": Branch( FlagV, ref cycles); break;

                // Flags
                case "CLC": FlagC = false; break;
                case "SEC": FlagC = true;  break;
                case "CLD": FlagD = false; break;
                case "SED": FlagD = true;  break;
                case "CLI": FlagI = false; break;
                case "SEI": FlagI = true;  break;
                case "CLV": FlagV = false; break;

                case "NOP": break;

                case "BRK":
                    Halted = true;
                    break;
            }
        }
    }
}
