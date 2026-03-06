using System;
using System.IO;
using System.Text;
using Ude;

namespace Simple_Text_Editor.Infrastructure
{

    public static class EncodingDetector
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private static readonly Encoding Win1251 = Encoding.GetEncoding(1251);

        public static Encoding DetectEncoding(string path)
        {
            using var stream = File.OpenRead(path);
            return DetectEncoding(stream);
        }

        public static Encoding DetectEncoding(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(stream));

            if (stream.CanSeek && stream.Length == 0)
                return Utf8NoBom;

            byte[] bytes = ReadAllBytes(stream);

            if (bytes.Length == 0)
                return Utf8NoBom;

            var bomEncoding = DetectBom(bytes);
            if (bomEncoding != null)
                return bomEncoding;

            if (LooksLikeUtf8(bytes))
                return Utf8NoBom;

            var udeEncoding = DetectWithUde(bytes);
            if (udeEncoding != null)
                return udeEncoding;

            return Win1251;
        }

        private static Encoding? DetectBom(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length >= 3 &&
                bytes[0] == 0xEF &&
                bytes[1] == 0xBB &&
                bytes[2] == 0xBF)
            {
                return Encoding.UTF8; // UTF-8 with BOM
            }

            if (bytes.Length >= 4 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xFE &&
                bytes[2] == 0x00 &&
                bytes[3] == 0x00)
            {
                return Encoding.UTF32; // UTF-32 LE
            }

            if (bytes.Length >= 4 &&
                bytes[0] == 0x00 &&
                bytes[1] == 0x00 &&
                bytes[2] == 0xFE &&
                bytes[3] == 0xFF)
            {
                return new UTF32Encoding(bigEndian: true, byteOrderMark: true); // UTF-32 BE
            }

            if (bytes.Length >= 2 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xFE)
            {
                return Encoding.Unicode; // UTF-16 LE
            }

            if (bytes.Length >= 2 &&
                bytes[0] == 0xFE &&
                bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode; // UTF-16 BE
            }

            return null;
        }

        private static Encoding? DetectWithUde(byte[] bytes)
        {
            var detector = new CharsetDetector();
            detector.Feed(bytes, 0, bytes.Length);
            detector.DataEnd();

            if (string.IsNullOrWhiteSpace(detector.Charset))
                return null;

            string charset = detector.Charset.Trim();

            try
            {

                if (charset.Equals("US-ASCII", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (charset.Equals("windows-1251", StringComparison.OrdinalIgnoreCase))
                    return Win1251;

                if (charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
                    return Utf8NoBom;

                return Encoding.GetEncoding(charset);
            }
            catch
            {
                return null;
            }
        }

        private static bool LooksLikeUtf8(ReadOnlySpan<byte> bytes)
        {
            int continuationBytes = 0;
            bool hasMultiByteSequence = false;

            foreach (byte b in bytes)
            {
                if (continuationBytes == 0)
                {
                    if ((b & 0b1000_0000) == 0)
                    {
                        continue;
                    }

                    if ((b & 0b1110_0000) == 0b1100_0000)
                    {
                        continuationBytes = 1;
                        hasMultiByteSequence = true;
                        continue;
                    }

                    if ((b & 0b1111_0000) == 0b1110_0000)
                    {
                        continuationBytes = 2;
                        hasMultiByteSequence = true;
                        continue;
                    }

                    if ((b & 0b1111_1000) == 0b1111_0000)
                    {
                        continuationBytes = 3;
                        hasMultiByteSequence = true;
                        continue;
                    }

                    return false;
                }
                else
                {
                    if ((b & 0b1100_0000) != 0b1000_0000)
                        return false;

                    continuationBytes--;
                }
            }

            return continuationBytes == 0 && hasMultiByteSequence;
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            if (stream.CanSeek)
                stream.Position = 0;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            if (stream.CanSeek)
                stream.Position = 0;

            return ms.ToArray();
        }
    }
}