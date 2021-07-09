namespace DeltaX.CommonExtensions
{
    using System;


    public static class CommonExtensions
    {
        public static byte[] GetBytes(this string str)
        {
            return System.Text.Encoding.ASCII.GetBytes(str);
        }

        public static string GetString(this byte[] source)
        {
            return System.Text.Encoding.ASCII.GetString(source);
        }

        public static bool IsNumericType(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }


        private static readonly string[] _SizeSuffixesDefault = { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };

        /// <summary>
        /// Convierte un valor Numerico a una representacion legible
        /// 
        /// 10240       => 10.0 K
        /// 10695475    => 10.2 M
        /// 
        /// </summary>
        /// <param name="value">Varlor a convertir</param>
        /// <param name="decimalPlaces">Decimales representativos </param>
        /// <param name="roundSize">Factor de multiplicacion</param>
        /// <param name="sizeSuffixes">Arreglo de sufijos</param>
        /// <returns></returns>
        public static string SizeSuffix(double value, int decimalPlaces = 1, int roundSize = 1024, string[] sizeSuffixes = null)
        {
            if (sizeSuffixes == null)
                sizeSuffixes = _SizeSuffixesDefault;

            if (double.IsInfinity(value))
            {
                return "Infinity ";
            }

            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException("decimalPlaces");
            }
            if (value < 0)
            {
                return "-" + SizeSuffix(-value);
            }
            if (value == 0)
            {
                return String.Format("{0:n" + decimalPlaces + "} " + sizeSuffixes[0], 0);
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, roundSize);

            mag = mag > 0 ? mag : 0;

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= roundSize;
            }

            return String.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize, sizeSuffixes[mag]);
        }
    }
}
