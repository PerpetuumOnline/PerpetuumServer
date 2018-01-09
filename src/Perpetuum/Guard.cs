using System;

namespace Perpetuum
{
    public static class Guard
    {
        public static void ThrowIfZero(this int source, ErrorCodes error)
        {
            source.ThrowIfZero(() => PerpetuumException.Create(error));
        }

        public static void ThrowIfZero(this int source, Func<Exception> exceptionFactory)
        {
            source.ThrowIfEqual(0, exceptionFactory);
        }

        public static void ThrowIfZero(long source, Func<Exception> exceptionFactory)
        {
            source.ThrowIfEqual(0, exceptionFactory);
        }

        public static double ThrowIfZero(this double source, Func<Exception> exceptionFactory)
        {
            (Math.Abs(source) < double.Epsilon).ThrowIfTrue(exceptionFactory);
            return source;
        }

        public static void ThrowIfTrue([UsedImplicitly] this bool value, Func<Exception> exceptionFactory)
        {
            if (value)
                throw exceptionFactory();
        }

        public static void ThrowIfFalse([UsedImplicitly] this bool value, Func<Exception> exceptionFactory)
        {
            if (!value)
                throw exceptionFactory();
        }

        public static T ThrowIfNotType<T>(this object value, ErrorCodes error)
        {
            (value is T).ThrowIfFalse(error);
            return (T)value;
        }

        public static void ThrowIfType<T>(this object actual, ErrorCodes error)
        {
            (actual is T).ThrowIfTrue(error);
        }

        public static T ThrowIfGreater<T>(this T source, T comparer, ErrorCodes error, [InstantHandle] Action<PerpetuumException> exceptionAction = null) where T : IComparable<T>
        {
            (source.CompareTo(comparer) > 0).ThrowIfTrue(error, exceptionAction);
            return source;
        }

        public static T ThrowIfGreaterOrEqual<T>(this T source, T comparer, ErrorCodes error, [InstantHandle] Action<PerpetuumException> exceptionAction = null) where T : IComparable<T>
        {
            (source.CompareTo(comparer) >= 0).ThrowIfTrue(error, exceptionAction);
            return source;
        }

        public static T ThrowIfLess<T>(this T source, T comparer, ErrorCodes error, [InstantHandle] Action<PerpetuumException> exceptionAction = null) where T : IComparable<T>
        {
            ThrowIfTrue(source.CompareTo(comparer) < 0, error, exceptionAction);
            return source;
        }

        public static T ThrowIfLessOrEqual<T>(this T source, T comparer, ErrorCodes error, [InstantHandle] Action<PerpetuumException> exceptionAction = null) where T : IComparable<T>
        {
            ThrowIfTrue(source.CompareTo(comparer) <= 0, error, exceptionAction);
            return source;
        }

        public static T ThrowIfEqual<T>(this T actual, T expected, ErrorCodes error, [InstantHandle] Action<PerpetuumException> exceptionAction = null)
        {
            ThrowIfTrue(Equals(actual, expected), error, exceptionAction);
            return actual;
        }

        public static T ThrowIfNotEqual<T>(this T actual, T expected, ErrorCodes error, [InstantHandle]Action<PerpetuumException> exceptionAction = null)
        {
            ThrowIfFalse(Equals(actual, expected), error, exceptionAction);
            return actual;
        }

        public static ErrorCodes ThrowIfError(this ErrorCodes error, [InstantHandle] Action<PerpetuumException> exceptionAction = null)
        {
            return error.ThrowIfNotEqual(ErrorCodes.NoError, () =>
            {
                var gex = PerpetuumException.Create(error);
                exceptionAction?.Invoke(gex);
                return gex;
            });
        }

        public static void ThrowIfNotNull(this object o, ErrorCodes error, [InstantHandle] Action<PerpetuumException> exceptionAction)
        {
            (o != null).ThrowIfTrue(error, exceptionAction);
        }

        public static void ThrowIfNotNull(this object o, ErrorCodes error)
        {
            o.ThrowIfNotEqual(null, error);
        }

        [NotNull]
        public static T ThrowIfNull<T>(this T source, ErrorCodes error, [InstantHandle]Action<PerpetuumException> exceptionAction = null)
        {
            return source.ThrowIfNull(() =>
            {
                var gex = PerpetuumException.Create(error);
                exceptionAction?.Invoke(gex);
                return gex;
            });
        }

        public static void ThrowIfFalse(this bool value, ErrorCodes error, [InstantHandle]Action<PerpetuumException> exceptionAction = null)
        {
            value.ThrowIfFalse(() =>
            {
                var gex = PerpetuumException.Create(error);
                exceptionAction?.Invoke(gex);
                return gex;
            });
        }

        public static void ThrowIfTrue(this bool value, ErrorCodes error, [InstantHandle]Action<PerpetuumException> exceptionAction = null)
        {
            value.ThrowIfTrue(() =>
            {
                var gex = PerpetuumException.Create(error);
                exceptionAction?.Invoke(gex);
                return gex;
            });
        }

        public static T ThrowIfNull<T>([CanBeNull] this T source, Func<Exception> exceptionFactory)
        {
            if (Equals(source, null))
                throw exceptionFactory();

            return source;
        }

        public static T ThrowIfNotEqual<T>(this T source, T comparer, Func<Exception> exceptionFactory)
        {
            Equals(source, comparer).ThrowIfFalse(exceptionFactory);
            return source;
        }

        public static T ThrowIfEqual<T>(this T source, T comparer, Func<Exception> exceptionFactory)
        {
            Equals(source, comparer).ThrowIfTrue(exceptionFactory);
            return source;
        }

        public static string ThrowIfNullOrEmpty(this string text, ErrorCodes error)
        {
            string.IsNullOrEmpty(text).ThrowIfTrue(error);
            return text;
        }
    }
}
