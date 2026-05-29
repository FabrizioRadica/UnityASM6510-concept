// Author: Fabrizio Radica
// Version: 1.0
// Description: Loads Assembly source files from StreamingAssets and assembles them into memory.

using System.IO;
using UnityEngine;

namespace Mini6510
{
    public static class ProgramLoader
    {
        public static int LoadFromStreamingAssets(string filename, Memory mem,
            int loadAddress = Memory.PROGRAM_RAM_START)
        {
            string path = Path.Combine(Application.streamingAssetsPath, filename);

            if (!File.Exists(path))
            {
                Debug.LogError($"[ProgramLoader] File not found: {path}");
                return -1;
            }

            string source = File.ReadAllText(path);
            return AssemblerLoader.Assemble(source, mem, loadAddress);
        }
    }
}
