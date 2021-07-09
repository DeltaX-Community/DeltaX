namespace DeltaX.CommonExtensions
{
    using System;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;

    public static class Assert
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Check(bool condition, Action onFail)
        {
            if (onFail != null && !condition)
            {
                onFail.Invoke();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Check(bool condition, string paramName, string message = null)
        {
            if (!condition)
            {
                throw new ArgumentNullException(paramName, message);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull(object value, string paramName, string message = null)
        {
            if (value is string valStr)
            {
                Check(!string.IsNullOrEmpty(valStr), () => throw new ArgumentNullException(paramName, message));
            }
            else
            {
                Check(value != null, () => throw new ArgumentNullException(paramName, message));
            }
        }
    }
}
