// Author: Fabrizio Radica
// Version: 1.0
// Description: Main MonoBehaviour — orchestrates all Mini6510 subsystems.
//              Attach to a GameObject in your scene and assign the demo filename.

using UnityEngine;

namespace Mini6510
{
    public class Mini6510 : MonoBehaviour
    {
        [Header("Program")]
        [Tooltip("Assembly source filename inside StreamingAssets/")]
        public string ProgramFile = "demo.asm";

        [Header("Execution")]
        [Tooltip("CPU cycles to execute per frame")]
        public int CyclesPerFrame = 20000;

        // Subsystems
        private Memory          _mem;
        private CPU6510         _cpu;
        private VideoRAM        _video;
        private ColorRAM        _colorRAM;
        private CharacterSet    _charset;
        private RasterSystem    _raster;
        private SpriteSystem    _sprites;
        private InputSystem6510 _input;
        private AudioSystem     _audio;
        private Renderer6510    _renderer;

        private ulong _frameCount;

        private void Awake()
        {
            _mem      = new Memory();
            _cpu      = new CPU6510(_mem);
            _video    = new VideoRAM(_mem);
            _colorRAM = new ColorRAM(_mem);
            _charset  = new CharacterSet(_mem);
            _raster   = new RasterSystem(_mem);
            _sprites  = new SpriteSystem(_mem);
            _input    = new InputSystem6510(_mem);
            _renderer = new Renderer6510(_mem, _video, _colorRAM, _charset, _raster, _sprites);

            // AudioSystem needs MonoBehaviour component attachment
            _audio = gameObject.AddComponent<AudioSystem>();
            _audio.Initialize(_mem);

            // Default palette
            _mem.Write(Memory.BORDER_COLOR, 6); // Blue
            _mem.Write(Memory.BG_COLOR,     0); // Black

            LoadAndRun();
        }

        private void LoadAndRun()
        {
            int entryPoint = ProgramLoader.LoadFromStreamingAssets(ProgramFile, _mem);
            if (entryPoint >= 0)
                _cpu.SetPC((ushort)entryPoint);
        }

        private void Update()
        {
            _input.Tick();
            _raster.Tick();

            // Update frame counter and vsync flag
            _frameCount++;
            _mem.Write(Memory.FRAME_COUNTER, (byte)(_frameCount & 0xFF));
            _mem.Write(Memory.VSYNC_FLAG,    1);

            // Execute CPU cycles for this frame
            int cycles = 0;
            while (cycles < CyclesPerFrame && !_cpu.Halted)
                cycles += _cpu.Step();

            _mem.Write(Memory.VSYNC_FLAG, 0);

            _sprites.Tick();
        }

        private void OnGUI()
        {
            _renderer.Draw();
        }

        private void OnDestroy()
        {
            _sprites.Dispose();
        }
    }
}
