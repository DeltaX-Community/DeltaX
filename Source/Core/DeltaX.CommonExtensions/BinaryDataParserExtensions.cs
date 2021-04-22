namespace DeltaX.CommonExtensions
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class BinaryDataParserExtensions
    {
        /// <summary>
        /// Configure bytes endian and data type
        /// Endian (first character):
        ///   - ! byte swap (swap pairs of two bytes: based on network order)
        ///   - < little-endian
        ///   - > big-endian
        ///   
        /// Dtata type(second character)
        ///   - s string (length third character) only for string
        ///   
        ///   - c chart
        ///   - b signed char
        ///   
        ///   - h short
        ///   - H unsigend short
        ///   - i int
        ///   - I unsigned int
        ///   - q long long 64 bits
        ///   - Q unsigned long long
        ///   
        ///   - f float
        ///   - d double
        ///    
        /// Examples: 
        /// 
        ///   - !>i  : byte swap signed int32 big endian
        ///   - I    : unsigned int32 little indian
        ///   - f    : float (32) little endian
        ///   - !h   : signed int16 byte swap
        ///   - !s10 : byte swap string 10 elements
        ///   
        /// </summary>
        /// <param name="format"></param>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static object Parser(this byte[] data, string format, int index)
        {
            var byteSwap = format.Contains('!');
            var isBigEndian = format.Contains('>');
            format = format.TrimStart('!', '<', '>');
            var type = format[0];
            var changeEndian = isBigEndian == BitConverter.IsLittleEndian;

            switch (type)
            {
                case 's':
                    {
                        var len = int.Parse(format.Split(type)[1]);
                        var res = data.SubArray(index, len, byteSwap);
                        return UTF8Encoding.ASCII.GetString(res);
                    }
                case 'b':
                    {
                        return (short)(sbyte)data[index];
                    }
                case 'B':
                    {
                        return (ushort)(byte)data[index];
                    }
                case 'h':
                    {
                        var res = data.SubArray(index, 2, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToInt16(res);
                    }
                case 'H':
                    {
                        var res = data.SubArray(index, 2, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToUInt16(res);
                    }
                case 'i':
                    {
                        var res = data.SubArray(index, 4, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToInt32(res);
                    }
                case 'I':
                    {
                        var res = data.SubArray(index, 4, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToUInt32(res);
                    }
                case 'q':
                    {
                        var res = data.SubArray(index, 8, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToInt64(res);
                    }
                case 'Q':
                    {
                        var res = SubArray(data, index, 8, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToUInt64(res);
                    }
                case 'f':
                    {
                        var res = data.SubArray(index, 4, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToSingle(res);
                    }
                case 'd':
                    {
                        var res = data.SubArray(index, 8, byteSwap);
                        if (changeEndian)
                        {
                            Array.Reverse(res);
                        }
                        return BitConverter.ToDouble(res);
                    }
            }

            throw new ArgumentException("Bad Format parametter");
        }

        /// <summary>
        /// Parse binary to object with format pattern for index detection:
        /// 
        /// Example:
        ///     [2]I    : Unsigned Int 32 bits start on data index 2 
        ///     [6]h    : Unsigned Short Int 16 bits start on data index 4 
        ///     [8]!>f  : Byte Shap big endian float 32 bits start on data index 8 
        ///     !>f     : Byte Shap big endian float 32 bits start on data index 0 
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="formatPattern"></param>
        /// <returns></returns>
        public static object Parser(this byte[] data, string formatPattern)
        {
            Regex regex = new Regex(@"(\[(?<index>\d+)\])?(?<format>[<>!\w]+)",
               RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            var res = regex.Match(formatPattern);
            if (!res.Success)
            {
                return null;
            }

            var index = int.Parse(res.Groups["index"]?.Value ?? "0");
            var format = res.Groups["format"].Value;
            return data.Parser(format, index);
        }

        public static byte[] SubArray(this byte[] data, int index, int len, bool byteSwap)
        {
            var res = new byte[len];
            Array.Copy(data, index, res, 0, res.Length);
            if (byteSwap)
            {
                for (int idx = 0; idx < res.Length; idx += 2)
                {
                    Array.Reverse(res, idx, 2);
                }
            }
            return res;
        }
    }
}