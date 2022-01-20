using System.IO;
using System.Text;

namespace UCrashLogDet
{
    public static class BinaryReaderExtensions
    {
        public static string ReadCString(this BinaryReader reader)
        {
            StringBuilder sb = new StringBuilder();
            char c;
            while ((c = reader.ReadChar()) != 0)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }
        public static string ReadLString(this BinaryReader reader, int length)
        {
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        }
        public static void Align4(this BinaryReader reader)
        {
            long pad = 4 - (reader.BaseStream.Position % 4);
            if (pad != 4) reader.BaseStream.Position += pad;
        }
    }
}
