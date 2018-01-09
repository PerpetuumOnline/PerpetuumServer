using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Converters
{
    public static class ConverterExtensions
    {
        public static Converter<TIn, TOut> ToDelegate<TIn, TOut>(this IConverter<TIn, TOut> converter)
        {
            return converter.Convert;
        }

        public static IEnumerable<TOut> ConvertAll<T, TOut>(this IEnumerable<T> enumerable,IConverter<T, TOut> converter)
        {
            return enumerable.Select(converter.Convert);
        }

        public static IEnumerable<TOut> ConvertAll<T, TOut>(this IEnumerable<T> enumerable,Converter<T, TOut> converter)
        {
            return enumerable.Select(e => converter(e));
        }
    }
}
