using System.IO;

namespace ModAPI.Common
{
    public static class LAAUtils
    {
        private const long POINTER_TO_PE_HEADER = 0x3C;
        private const long CHARACTERISTICS_OFFSET_FROM_PE_HEADER = 0x16;
        private const ushort IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20;

        /// <summary>
        /// Returns true if the specified path is a LAA (Large Address Aware, aka 4GB patched) executable.
        /// The specified path must be a valid Windows executable, otherwise behavior is undefined.
        /// </summary>
        public static bool IsLAA(string path)
        {
            // Based on https://stackoverflow.com/a/9056757

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    // Locate PE header
                    reader.BaseStream.Position = POINTER_TO_PE_HEADER;
                    var peHeaderPosition = reader.ReadInt32();

                    // Locate Characteristics field
                    reader.BaseStream.Position = peHeaderPosition + CHARACTERISTICS_OFFSET_FROM_PE_HEADER;

                    // Check if the LAA flag is set
                    var characteristics = reader.ReadUInt16();
                    return (characteristics & IMAGE_FILE_LARGE_ADDRESS_AWARE) != 0;
                }
            }
        }

    }
}