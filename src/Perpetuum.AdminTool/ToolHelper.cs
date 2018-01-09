using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Perpetuum.AdminTool
{
    public static class ToolHelper
    {
        public static string ToSha1(this string input)
        {
            if (input.IsNullOrEmpty()) return "";
            var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }

        public static Visibility ToControlVisibility(this bool visible)
        {
            return visible ? Visibility.Visible : Visibility.Hidden;
        }

        public static string ToGui(this AccessLevel accessLevel)
        {
            switch (accessLevel)
            {
                case AccessLevel.normal:
                    return AccessLevel.normal.ToString().SplitCamel();
                case AccessLevel.gameAdmin:
                    return AccessLevel.gameAdmin.ToString().SplitCamel();
                case AccessLevel.toolAdmin:
                    return AccessLevel.toolAdmin.ToString().SplitCamel();
                case AccessLevel.owner:
                    return AccessLevel.owner.ToString().SplitCamel();
                default:
                    return "n/a";
            }
        }

        public static void SetVisible(this FrameworkElement control, bool visible)
        {
            control.Visibility = visible.ToControlVisibility();
        }

        public static void FillComboBoxWithAccessLevel(this ComboBox comboBox)
        {
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.Items.Add(new KeyValuePair<AccessLevel, string>(AccessLevel.normal, "normal"));
            comboBox.Items.Add(new KeyValuePair<AccessLevel, string>(AccessLevel.gameAdmin, "game admin"));
            comboBox.Items.Add(new KeyValuePair<AccessLevel, string>(AccessLevel.toolAdmin, "tool admin"));
        }

        public static void FillComboBoxForBanLength(this ComboBox comboBox)
        {
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.Items.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.FromMinutes(2), "two minutes"));
            comboBox.Items.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.FromHours(1), "one hour"));
            comboBox.Items.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.FromDays(1), "one day"));
        }

        public static string SplitCamel(this string camelText, bool toLower = true)
        {
            var rx = new Regex("[A-Z]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

            var result = rx.Replace(camelText, match => " " + match.Value);
            return toLower ? result.ToLower() : result;
        }

        public static string ToCompact(this DateTime date, bool includeSeconds = true)
        {
            var res = $"{date.Year}-{date.Month}-{date.Day} {date.Hour}:{date.Minute}";
            return includeSeconds ? $"{res}:{date.Second}" : res;
        }

         
    }
}