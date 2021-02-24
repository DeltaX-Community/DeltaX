using System;
using System.Linq;
using System.Text;

public static class DataParser
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
    public static object Parser(string format, byte[] data, int index)
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
                    var res = SubArray(data, index, len, byteSwap);
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
                    var res = SubArray(data, index, 2, byteSwap);
                    if (changeEndian)
                    {
                        Array.Reverse(res);
                    }
                    return BitConverter.ToInt16(res);
                }
            case 'H':
                {
                    var res = SubArray(data, index, 2, byteSwap);
                    if (changeEndian)
                    {
                        Array.Reverse(res);
                    }
                    return BitConverter.ToUInt16(res);
                }
            case 'i':
                {
                    var res = SubArray(data, index, 4, byteSwap);
                    if (changeEndian)
                    {
                        Array.Reverse(res);
                    }
                    return BitConverter.ToInt32(res);
                }
            case 'I':
                {
                    var res = SubArray(data, index, 4, byteSwap);
                    if (changeEndian)
                    {
                        Array.Reverse(res);
                    }
                    return BitConverter.ToUInt32(res);
                }
            case 'q':
                {
                    var res = SubArray(data, index, 8, byteSwap);
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
                    var res = SubArray(data, index, 4, byteSwap);
                    if (changeEndian)
                    {
                        Array.Reverse(res);
                    }
                    return BitConverter.ToSingle(res);
                }
            case 'd':
                {
                    var res = SubArray(data, index, 8, byteSwap);
                    if (changeEndian)
                    {
                        Array.Reverse(res);
                    }
                    return BitConverter.ToDouble(res);
                }
        }

        throw new ArgumentException("Bad Format parametter");
    }

    public static byte[] SubArray(byte[] data, int index, int len, bool byteSwap)
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