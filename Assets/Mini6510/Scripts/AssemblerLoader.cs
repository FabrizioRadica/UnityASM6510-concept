// Author: Fabrizio Radica
// Version: 1.0
// Description: Two-pass assembler that parses MOS 6510 Assembly source and writes
//              machine code into Mini6510 Memory starting at the load address.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Mini6510
{
    public static class AssemblerLoader
    {
        // Tokens used during parsing
        private struct Line
        {
            public string Label;
            public string Mnemonic;
            public string Operand;
            public int    SourceLine;
        }

        public static int Assemble(string source, Memory mem, int loadAddress = Memory.PROGRAM_RAM_START)
        {
            var lines     = ParseLines(source);
            var symbols   = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var patches   = new List<(int addr, string label, bool isRelative, int instrEnd)>();
            var machineCode = new List<byte>();
            int pc        = loadAddress;

            // ── Pass 1: resolve labels, emit known bytes ──────────────────────────
            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line.Label))
                    symbols[line.Label] = pc;

                if (string.IsNullOrEmpty(line.Mnemonic))
                    continue;

                // Assembler directives
                if (line.Mnemonic.Equals(".EQU", StringComparison.OrdinalIgnoreCase))
                {
                    // Value already stored as label; resolve it now
                    if (!string.IsNullOrEmpty(line.Label))
                        symbols[line.Label] = ParseWord(line.Operand);
                    continue;
                }

                if (line.Mnemonic.Equals(".BYTE", StringComparison.OrdinalIgnoreCase) ||
                    line.Mnemonic.Equals("!BYTE", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var byteStr in line.Operand.Split(','))
                    {
                        machineCode.Add(ParseByte(byteStr.Trim()));
                        pc++;
                    }
                    continue;
                }

                if (line.Mnemonic.Equals(".WORD", StringComparison.OrdinalIgnoreCase) ||
                    line.Mnemonic.Equals("!WORD", StringComparison.OrdinalIgnoreCase))
                {
                    int w = ParseWord(line.Operand.Trim());
                    machineCode.Add((byte)(w & 0xFF));
                    machineCode.Add((byte)(w >> 8));
                    pc += 2;
                    continue;
                }

                if (line.Mnemonic.Equals(".ORG", StringComparison.OrdinalIgnoreCase) ||
                    line.Mnemonic.Equals("*=",   StringComparison.OrdinalIgnoreCase))
                {
                    // Fill gap with NOPs if needed
                    int newPC = ParseWord(line.Operand.Trim());
                    while (pc < newPC) { machineCode.Add(0xEA); pc++; }
                    pc = newPC;
                    continue;
                }

                // Normal instruction
                EmitInstruction(line, machineCode, symbols, patches, ref pc);
            }

            // ── Pass 2: patch forward-references ─────────────────────────────────
            foreach (var patch in patches)
            {
                if (!symbols.TryGetValue(patch.label, out int target))
                {
                    Debug.LogError($"[Assembler] Undefined label: {patch.label}");
                    continue;
                }

                int idx = patch.addr - loadAddress;
                if (idx < 0 || idx >= machineCode.Count) continue;

                if (patch.isRelative)
                {
                    int offset = target - patch.instrEnd;
                    if (offset < -128 || offset > 127)
                        Debug.LogError($"[Assembler] Branch out of range to {patch.label}");
                    machineCode[idx] = (byte)(sbyte)offset;
                }
                else
                {
                    machineCode[idx]     = (byte)(target & 0xFF);
                    if (idx + 1 < machineCode.Count)
                        machineCode[idx + 1] = (byte)(target >> 8);
                }
            }

            // ── Write to memory ───────────────────────────────────────────────────
            for (int i = 0; i < machineCode.Count; i++)
                mem.Write(loadAddress + i, machineCode[i]);

            Debug.Log($"[Assembler] Assembled {machineCode.Count} bytes at ${loadAddress:X4}");
            return loadAddress;
        }

        // ── Instruction emitter ───────────────────────────────────────────────────

        private static void EmitInstruction(Line line, List<byte> code,
            Dictionary<string, int> symbols,
            List<(int, string, bool, int)> patches,
            ref int pc)
        {
            string mn  = line.Mnemonic.ToUpper();
            string op  = line.Operand ?? "";
            bool isBranch = IsBranchMnemonic(mn);

            // Try to find a matching addressing mode
            foreach (var kv in OpcodeTable.Table)
            {
                var info = kv.Value;
                if (info.Mnemonic != mn) continue;

                if (TryMatchMode(op, info.Mode, isBranch, symbols, patches, code, ref pc, kv.Key))
                    return;
            }

            Debug.LogError($"[Assembler] Line {line.SourceLine}: cannot encode '{mn} {op}'");
        }

        private static bool TryMatchMode(string operand, AddressMode mode, bool isBranch,
            Dictionary<string, int> symbols,
            List<(int, string, bool, int)> patches,
            List<byte> code, ref int pc, byte opcode)
        {
            operand = operand.Trim();

            switch (mode)
            {
                case AddressMode.Implied:
                    if (string.IsNullOrEmpty(operand)) { Emit1(code, ref pc, opcode); return true; }
                    return false;

                case AddressMode.Accumulator:
                    if (operand == "A" || string.IsNullOrEmpty(operand)) { Emit1(code, ref pc, opcode); return true; }
                    return false;

                case AddressMode.Immediate:
                    if (operand.StartsWith("#"))
                    {
                        byte imm = ParseByte(operand.Substring(1));
                        Emit2(code, ref pc, opcode, imm);
                        return true;
                    }
                    return false;

                case AddressMode.ZeroPage:
                    if (IsZeroPageExpr(operand) && !operand.Contains(","))
                    {
                        byte zp = ParseByte(operand);
                        Emit2(code, ref pc, opcode, zp);
                        return true;
                    }
                    return false;

                case AddressMode.ZeroPageX:
                    if (IsZeroPageExpr(StripCommaX(operand, out bool okX)) && okX)
                    {
                        byte zp = ParseByte(StripCommaX(operand, out _));
                        Emit2(code, ref pc, opcode, zp);
                        return true;
                    }
                    return false;

                case AddressMode.ZeroPageY:
                    if (IsZeroPageExpr(StripCommaY(operand, out bool okY)) && okY)
                    {
                        byte zp = ParseByte(StripCommaY(operand, out _));
                        Emit2(code, ref pc, opcode, zp);
                        return true;
                    }
                    return false;

                case AddressMode.Absolute:
                    if (!operand.StartsWith("#") && !operand.Contains("(") && !operand.Contains(","))
                    {
                        int target = ResolveWordExpr(operand, symbols);
                        if (target < 0)
                        {
                            // Forward reference
                            code.Add(opcode); code.Add(0); code.Add(0);
                            patches.Add((pc + 1, operand, false, 0));
                            pc += 3;
                        }
                        else
                        {
                            Emit3(code, ref pc, opcode, (ushort)target);
                        }
                        return true;
                    }
                    return false;

                case AddressMode.AbsoluteX:
                {
                    string stripped = StripCommaX(operand, out bool ok);
                    if (ok && !stripped.StartsWith("#") && !stripped.Contains("("))
                    {
                        int t = ResolveWordExpr(stripped, symbols);
                        if (t < 0) { code.Add(opcode); code.Add(0); code.Add(0); patches.Add((pc+1, stripped, false, 0)); pc += 3; }
                        else Emit3(code, ref pc, opcode, (ushort)t);
                        return true;
                    }
                    return false;
                }

                case AddressMode.AbsoluteY:
                {
                    string stripped = StripCommaY(operand, out bool ok);
                    if (ok && !stripped.StartsWith("#") && !stripped.Contains("("))
                    {
                        int t = ResolveWordExpr(stripped, symbols);
                        if (t < 0) { code.Add(opcode); code.Add(0); code.Add(0); patches.Add((pc+1, stripped, false, 0)); pc += 3; }
                        else Emit3(code, ref pc, opcode, (ushort)t);
                        return true;
                    }
                    return false;
                }

                case AddressMode.Indirect:
                {
                    var m = Regex.Match(operand, @"^\(([^,)]+)\)$");
                    if (m.Success)
                    {
                        int t = ResolveWordExpr(m.Groups[1].Value.Trim(), symbols);
                        if (t < 0) { code.Add(opcode); code.Add(0); code.Add(0); patches.Add((pc+1, m.Groups[1].Value.Trim(), false, 0)); pc += 3; }
                        else Emit3(code, ref pc, opcode, (ushort)t);
                        return true;
                    }
                    return false;
                }

                case AddressMode.IndirectX:
                {
                    var m = Regex.Match(operand, @"^\(([^,)]+),\s*X\)$", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        byte zp = ParseByte(m.Groups[1].Value.Trim());
                        Emit2(code, ref pc, opcode, zp);
                        return true;
                    }
                    return false;
                }

                case AddressMode.IndirectY:
                {
                    var m = Regex.Match(operand, @"^\(([^,)]+)\),\s*Y$", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        byte zp = ParseByte(m.Groups[1].Value.Trim());
                        Emit2(code, ref pc, opcode, zp);
                        return true;
                    }
                    return false;
                }

                case AddressMode.Relative:
                {
                    int instrEnd = pc + 2;
                    int t = ResolveWordExpr(operand, symbols);
                    if (t < 0)
                    {
                        code.Add(opcode); code.Add(0);
                        patches.Add((pc + 1, operand, true, instrEnd));
                        pc += 2;
                    }
                    else
                    {
                        int offset = t - instrEnd;
                        if (offset < -128 || offset > 127)
                            Debug.LogError($"[Assembler] Branch out of range to {operand}");
                        Emit2(code, ref pc, opcode, (byte)(sbyte)offset);
                    }
                    return true;
                }
            }
            return false;
        }

        // ── Emit helpers ──────────────────────────────────────────────────────────

        private static void Emit1(List<byte> c, ref int pc, byte op)
            { c.Add(op); pc++; }

        private static void Emit2(List<byte> c, ref int pc, byte op, byte b)
            { c.Add(op); c.Add(b); pc += 2; }

        private static void Emit3(List<byte> c, ref int pc, byte op, ushort w)
            { c.Add(op); c.Add((byte)(w & 0xFF)); c.Add((byte)(w >> 8)); pc += 3; }

        // ── Parsing helpers ───────────────────────────────────────────────────────

        private static List<Line> ParseLines(string source)
        {
            var result = new List<Line>();
            int lineNo = 0;

            foreach (var raw in source.Split('\n'))
            {
                lineNo++;
                // Strip comments
                string text = Regex.Replace(raw, @";.*$", "").Trim();
                if (string.IsNullOrEmpty(text)) continue;

                string label    = "";
                string mnemonic = "";
                string operand  = "";

                // Equate: LABEL = VALUE  (no colon)
                var equateMatch = Regex.Match(text, @"^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+)$");
                if (equateMatch.Success)
                {
                    label    = equateMatch.Groups[1].Value;
                    mnemonic = ".EQU";
                    operand  = equateMatch.Groups[2].Value.Trim();
                    result.Add(new Line { Label = label, Mnemonic = mnemonic, Operand = operand, SourceLine = lineNo });
                    continue;
                }

                // Label at start
                var labelMatch = Regex.Match(text, @"^([A-Za-z_][A-Za-z0-9_]*):");
                if (labelMatch.Success)
                {
                    label = labelMatch.Groups[1].Value;
                    text  = text.Substring(labelMatch.Length).Trim();
                }

                if (!string.IsNullOrEmpty(text))
                {
                    var parts = text.Split(new char[]{' ','\t'}, 2);
                    mnemonic  = parts[0].Trim();
                    operand   = parts.Length > 1 ? parts[1].Trim() : "";
                }

                result.Add(new Line { Label = label, Mnemonic = mnemonic, Operand = operand, SourceLine = lineNo });
            }

            return result;
        }

        private static byte ParseByte(string s)
        {
            s = s.Trim();
            if (s.StartsWith("$")) return Convert.ToByte(s.Substring(1), 16);
            if (s.StartsWith("%")) return Convert.ToByte(s.Substring(1), 2);
            return byte.Parse(s);
        }

        private static int ParseWord(string s)
        {
            s = s.Trim();
            if (s.StartsWith("$")) return Convert.ToInt32(s.Substring(1), 16);
            if (s.StartsWith("%")) return Convert.ToInt32(s.Substring(1), 2);
            return int.Parse(s);
        }

        private static int ResolveWordExpr(string s, Dictionary<string, int> symbols)
        {
            s = s.Trim();
            if (s.StartsWith("$") || s.StartsWith("%") || char.IsDigit(s[0]))
                return ParseWord(s);
            if (symbols.TryGetValue(s, out int v)) return v;
            return -1; // forward reference
        }

        private static bool IsZeroPageExpr(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s.StartsWith("$") && s.Length <= 3) return true;
            if (s.StartsWith("%") && s.Length <= 9) return true;
            if (byte.TryParse(s, out _)) return true;
            return false;
        }

        private static string StripCommaX(string s, out bool ok)
        {
            ok = false;
            var m = Regex.Match(s, @"^(.*),\s*X$", RegexOptions.IgnoreCase);
            if (m.Success) { ok = true; return m.Groups[1].Value.Trim(); }
            return s;
        }

        private static string StripCommaY(string s, out bool ok)
        {
            ok = false;
            var m = Regex.Match(s, @"^(.*),\s*Y$", RegexOptions.IgnoreCase);
            if (m.Success) { ok = true; return m.Groups[1].Value.Trim(); }
            return s;
        }

        private static bool IsBranchMnemonic(string mn)
        {
            switch (mn)
            {
                case "BCC": case "BCS": case "BEQ": case "BNE":
                case "BMI": case "BPL": case "BVC": case "BVS":
                    return true;
            }
            return false;
        }
    }
}
