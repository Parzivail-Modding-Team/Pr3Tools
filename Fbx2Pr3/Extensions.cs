using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fbx2Pr3
{
    static class Extensions
    {
        /// <summary>
        /// Writes a null-terminated string to the stream
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="s">The string to write</param>
        public static void WriteNtString(this BinaryWriter stream, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            stream.Write(bytes);
            stream.Write((byte)0);
        }
    }
}
