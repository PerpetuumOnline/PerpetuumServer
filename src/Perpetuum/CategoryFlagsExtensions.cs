using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum
{
    public static class CategoryFlagsExtensions
    {
        private static readonly IDictionary<string, CategoryFlags> _uniqueCategoryFlags;

        static CategoryFlagsExtensions()
        {
            _uniqueCategoryFlags = Database.CreateCache<string, CategoryFlags>("categoryflags", "name", r => r.GetValue<CategoryFlags>("value"), r => r.GetValue<bool>("isunique"));
        }

        public static CategoryFlags GetCategoryFlagsMask(this CategoryFlags categoryFlags)
        {
            var mask = 0xffffffffffffffffL;

            while (((ulong)categoryFlags & mask) > 0)
            {
                mask <<= 8;
            }

            return (CategoryFlags)(~mask);
        }

        public static bool IsAny(this CategoryFlags source, IEnumerable<CategoryFlags> targets)
        {
            return targets.Any(target => IsCategory(source, target));
        }

        public static bool IsCategory(this CategoryFlags sourceCategoryFlags, CategoryFlags targetCategoryFlags)
        {
            var mask = GetCategoryFlagsMask(targetCategoryFlags);
            return (sourceCategoryFlags & mask) == targetCategoryFlags;
        }

        /// <summary>
        /// Returns the category flags above the current one
        /// </summary>
        public static IEnumerable<CategoryFlags> GetCategoryFlagsTree(this CategoryFlags categoryFlags)
        {
            var mask = (long)GetCategoryFlagsMask(categoryFlags);

            while (mask > 0)
            {
                yield return (CategoryFlags)((long)categoryFlags & mask);
                mask >>= 8;
            }

            yield return CategoryFlags.undefined;
        }

        public static bool IsCategoryExists(this CategoryFlags categoryFlags)
        {
            return Enum.IsDefined(typeof(CategoryFlags), categoryFlags);
        }

        /// <summary>
        /// Returns true if the current flag is in a tree that is set to unique
        /// This function is used to determine if a module is unique -> only one can be fit on a robot.
        /// </summary>
        public static bool IsUniqueCategoryFlags(this CategoryFlags categoryFlag, out CategoryFlags uniqueCategoryFlag)
        {
            foreach (var cf in GetCategoryFlagsTree(categoryFlag).Reverse().Where(cf => _uniqueCategoryFlags.Values.Contains(cf)))
            {
                uniqueCategoryFlag = cf;
                return true;
            }

            uniqueCategoryFlag = CategoryFlags.undefined;
            return false;
        }
    }
}
