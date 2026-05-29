# Mini6510 — Fantasy 6510 Runtime for Unity

**A modular MOS 6510 runtime inspired by the Commodore 64, built entirely in Unity 6+.**

> Write real Assembly code. Run it inside Unity. Experience the magic of retro computing with modern rendering.

---

## The Idea

I grew up with the Commodore 64. The way programmers squeezed incredible demos, games, and effects out of that machine — through direct memory manipulation, raster tricks, SID music, and hardware sprites — has always fascinated me. That programming model, where every pixel and sound was under total programmer control through memory-mapped registers, is something no modern framework can replicate in spirit.

**Mini6510 is not a Commodore 64 emulator.** It is a *Fantasy Computer* — a custom virtual machine that borrows the soul of the C64:

- The **MOS 6510 CPU** instruction set
- The **C64 memory map** layout (preserved as closely as possible)
- The **programming model** — everything controlled through memory-mapped registers
- The **visual language** — character-based text mode, hardware sprites, copper bars, raster effects

But instead of emulating the VIC-II chip, the SID chip, or the CIA timers at hardware level, **Unity does the heavy lifting**. Unity renders the screen, Unity generates the audio, Unity reads the gamepad. The Assembly programmer never knows the difference — they still write to `$D020` to change the border color, `$D400` to play a sound, and `$DC00` to read the joystick.

The result is a platform where you can write MOS 6510 Assembly programs — demos, games, visualizers — and run them inside a modern Unity application, targeting any platform Unity supports: PC, Mac, Linux, consoles, mobile.

**The goal:** bring the C64 demo scene programming model into the 21st century, without giving up a single byte of that original aesthetic.

---

## Features

### CPU
- Complete official **MOS 6510 instruction set** (56 mnemonics, all addressing modes)
- No illegal opcodes
- Accurate cycle counting per instruction
- Page-crossing cycle penalties
- 6502 indirect JMP page-wrap bug reproduced

### Memory
- Full **64KB flat address space**
- C64-compatible memory map (original addresses preserved)
- Memory-mapped register dispatch — writes to hardware registers trigger Unity subsystems automatically

### Video
- **40×25 character text mode** (Video RAM at `$0400`)
- **8×8 pixel character rendering** — pixels drawn directly from Character Set RAM
- **Custom Character Set** support — modify `$2000–$2FFF` at runtime
- **Original C64 16-color palette**
- **Border** and **Background** color registers (`$D020`, `$D021`)
- Rendered entirely via Unity `OnGUI` — no Tilemap, no Canvas, no TextMeshPro

### Raster Effects
- Lightweight **copper bar system** — up to 16 simultaneous bars
- Per-bar: Y position, height, color, vertical speed, active flag
- Bars animate every frame, synchronized with CPU execution
- Configured via `$D100–$D1FF`

### Sprites
- **8 hardware-style sprites** — original C64 addresses
- Sprite pointers at `$07F8–$07FF`
- Enable flags, per-sprite color, X/Y expand
- **Sprite–sprite collision detection** via `$FF10`
- Rendering synchronized to frame updates

### Audio
- **SID-compatible programming model** — original register addresses
- 3 voices: Triangle, Sawtooth, Pulse, Noise waveforms
- Full **ADSR envelope** per voice
- Pulse width control
- Master volume at `$D418`
- Unity `AudioClip` PCM backend — no SID cycle emulation

### Input
- Joystick Port 1 (`$DC01`) and Port 2 (`$DC00`)
- C64 active-low bit convention (bit 0=Up, 1=Down, 2=Left, 3=Right, 4=Fire)
- Supports **keyboard**, **Xbox controller**, **PlayStation controller**, **generic gamepad**
- Assembly reads input exclusively through memory registers

### Assembler
- Built-in **two-pass assembler** — load `.asm` source directly from `StreamingAssets`
- Supports: all addressing modes, labels, forward references, `.BYTE`, `.WORD`, `.ORG`, `LABEL = VALUE` equates
- No external build step — assemble and run at Unity startup

---

## Memory Map

```
$0000–$03FF   General RAM (zero page + stack at $0100)
$0400–$07E7   Video RAM (40×25 text screen)
$07F8–$07FF   Sprite Pointers
$0800–$0BE7   Internal Color Buffer
$1000–$1FFF   Assembly Program RAM (default load address)
$2000–$2FFF   Character Set RAM
$D000–$D00F   Sprite X/Y Positions (pairs per sprite)
$D010         Sprite X MSB (bit N = sprite N X > 255)
$D015         Sprite Enable (bit mask)
$D017         Sprite Y Expand (bit mask)
$D01D         Sprite X Expand (bit mask)
$D020         Border Color
$D021         Background Color
$D027–$D02E   Sprite Colors (one per sprite)
$D100–$D1FF   Raster Effect Registers (copper bars)
$D400–$D418   SID-Compatible Audio Registers
$D800–$DBE7   Color RAM
$DC00         Joystick Port 2
$DC01         Joystick Port 1
$FF00         Frame Counter (low byte, increments every frame)
$FF01         VSync Flag (1 during CPU execution window, 0 otherwise)
$FF10         Sprite Collision Register
```

---

## Architecture

```
Mini6510.cs              ← Unity MonoBehaviour, main orchestrator
├── Memory.cs            ← 64KB address space + register dispatch callbacks
├── CPU6510.cs           ← MOS 6510 core (fetch / decode / execute)
├── OpcodeTable.cs       ← Opcode → AddressMode + cycle count lookup
├── AssemblerLoader.cs   ← Two-pass assembler
├── ProgramLoader.cs     ← StreamingAssets file loader
├── CharacterSet.cs      ← C64 ROM charset + Character Set RAM
├── VideoRAM.cs          ← 40×25 text screen accessor
├── ColorRAM.cs          ← C64 palette + Color RAM accessor
├── RasterSystem.cs      ← Copper bar animation system
├── SpriteSystem.cs      ← 8 hardware sprites + collision detection
├── CollisionSystem.cs   ← Collision register reader
├── AudioSystem.cs       ← SID-compatible Unity audio backend
├── InputSystem6510.cs   ← Gamepad/keyboard → joystick registers
└── Renderer6510.cs      ← OnGUI pixel renderer
```

Rendering order (each frame):
1. Background
2. Raster Effects (Copper Bars)
3. Character Layer
4. Sprites
5. Border

---

## Project Structure

```
Assets/
├── Mini6510/
│   ├── Mini6510.asmdef          ← Unity Assembly Definition
│   └── Scripts/
│       ├── Mini6510.cs
│       ├── CPU6510.cs
│       ├── OpcodeTable.cs
│       ├── Memory.cs
│       ├── VideoRAM.cs
│       ├── ColorRAM.cs
│       ├── CharacterSet.cs
│       ├── SpriteSystem.cs
│       ├── RasterSystem.cs
│       ├── AudioSystem.cs
│       ├── InputSystem6510.cs
│       ├── Renderer6510.cs
│       ├── AssemblerLoader.cs
│       ├── ProgramLoader.cs
│       └── CollisionSystem.cs
└── StreamingAssets/
    ├── demo.asm                 ← Full feature demo
    └── color_cycle.asm          ← Color cycling demo
```

---

## Getting Started

### Requirements
- Unity 6 or newer
- Unity Input System package (`com.unity.inputsystem`)

### Setup
1. Copy the `Mini6510/` folder into your Unity project's `Assets/` directory.
2. Copy your `.asm` source files into `Assets/StreamingAssets/`.
3. Create an empty GameObject in your scene.
4. Attach the `Mini6510` component to it.
5. Set the `Program File` field to your Assembly filename (e.g. `demo.asm`).
6. Press Play.

The assembler runs at startup, loads your program into RAM at `$1000`, and begins execution automatically.

---

## Writing Assembly Programs

Programs are plain text `.asm` files placed in `StreamingAssets/`. They are assembled at runtime — no external toolchain needed.

### Syntax

```asm
; This is a comment
        .ORG $1000          ; set load address

LABEL:
        LDA #$06            ; immediate
        STA $D020           ; absolute
        LDA $00,X           ; zero page X
        BNE LABEL           ; branch (forward and backward)

MY_CONST = $1234            ; equate (label = value)

DATA:
        .BYTE $01,$02,$03   ; inline bytes
        .WORD $D020         ; inline word (little-endian)
```

### Frame Synchronization

```asm
WAIT_VSYNC:
        LDA $FF01
        BEQ WAIT_VSYNC      ; spin until vsync flag is set
```

Always wait for vsync before updating the screen to avoid tearing.

### Changing Colors

```asm
LDA #$02    ; Red (C64 color index)
STA $D020   ; → border color
LDA #$00    ; Black
STA $D021   ; → background color
```

### Playing a Sound

```asm
LDA #$0F   : STA $D418   ; master volume = 15
LDA #$66   : STA $D400   ; freq lo (C note ~262Hz)
LDA #$11   : STA $D401   ; freq hi
LDA #$41   : STA $D404   ; pulse waveform + gate on
```

### Reading the Joystick

```asm
LDA $DC00           ; read port 2
AND #$01            ; bit 0 = UP (0 = pressed)
BNE NOT_UP
; up is pressed
NOT_UP:
```

### Copper Bars (Raster Effects)

Each bar occupies 8 bytes starting at `$D100 + index × 8`:

| Offset | Meaning |
|--------|---------|
| +0 | Y position (0–199) |
| +1 | Height in scan lines |
| +2 | Color index (0–15) |
| +3 | Speed (signed, pixels/frame) |
| +4 | Active (1 = on) |

```asm
LDA #$40  : STA $D100   ; Y = 64
LDA #$10  : STA $D101   ; height = 16
LDA #$05  : STA $D102   ; green
LDA #$01  : STA $D103   ; speed +1 (downward)
LDA #$01  : STA $D104   ; active
```

---

## C64 Color Palette

| Index | Color | Index | Color |
|-------|-------|-------|-------|
| 0 | Black | 8 | Orange |
| 1 | White | 9 | Brown |
| 2 | Red | 10 | Light Red |
| 3 | Cyan | 11 | Dark Gray |
| 4 | Purple | 12 | Medium Gray |
| 5 | Green | 13 | Light Green |
| 6 | Blue | 14 | Light Blue |
| 7 | Yellow | 15 | Light Gray |

---

## Included Demos

### `demo.asm`
The full feature showcase:
- **"RADICA 6510 UNITY DEMO"** title in yellow
- Horizontal **scrolling text** on the bottom row
- 4 animated **copper bars** (cyan, green, purple, orange)
- 1 **bouncing sprite** (diamond shape)
- **Joystick control** — move the sprite with keyboard or gamepad
- **SID music** — 4-note cycling melody on voice 1

### `color_cycle.asm`
A minimal, elegant demo:
- **"RADICA DEMO"** centered on screen
- Border color, background color, and text color each cycle independently through the full C64 palette
- Three different cycling speeds give a constantly shifting color composition

---

## Design Principles

- **Not an emulator.** VIC-II, SID, CIA, BASIC ROM and KERNAL ROM are not emulated at hardware level. Unity handles rendering, audio and input natively.
- **Portable Assembly.** Existing C64 Assembly code should work with minimal changes, as long as it targets the standard memory-mapped registers and does not depend on KERNAL/BASIC ROM routines.
- **Modular.** Every subsystem is independent and replaceable. You can swap in a different renderer, extend the audio system, or add new memory-mapped registers without touching the CPU core.
- **Production ready.** No pseudocode. No placeholders. No TODOs.

---

## Roadmap Ideas

- Multicolor character mode
- Bitmap graphics mode
- IRQ / NMI interrupt support
- Binary `.prg` file loader (skip the assembler)
- PETSCII font lookup table expansion
- In-Unity debugger / memory viewer

---

## Author

**Fabrizio Radica**
Creative Developer & Designer

- Email: [fabrizio@radicadesign.com](mailto:fabrizio@radicadesign.com)
- Website: [www.radicadesign.com](https://www.radicadesign.com)
- GitHub: [github.com/Radica65010](https://github.com/Radica65010)

---

## License

This project is released for personal and educational use.
Contact [fabrizio@radicadesign.com](mailto:fabrizio@radicadesign.com) for commercial licensing inquiries.

---

*Mini6510 — because some ideas are too good to leave in 1985.*
