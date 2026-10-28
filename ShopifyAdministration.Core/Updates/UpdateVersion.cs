using System;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ShopifyAdministration.Core.Updates
{
    /// <summary>Compares two-to-four numeric version components and SemVer-style prereleases deterministically.</summary>
    public sealed class UpdateVersion : IComparable<UpdateVersion>
    {
        private readonly int[] parts;
        private readonly string[] preview;
        private UpdateVersion(int[] parts, string[] preview) { this.parts = parts; this.preview = preview; }
        /// <summary>Gets a canonical version key independent of v prefixes, build metadata and omitted zero components.</summary>
        public string Key => string.Join(".", parts.Select(part => part.ToString(CultureInfo.InvariantCulture))) + (preview.Length == 0 ? "" : "-" + string.Join(".", preview.Select(NormalizeIdentifier)));
        /// <summary>Gets whether the version is a prerelease.</summary>
        public bool IsPrerelease => preview.Length != 0;
        /// <summary>Parses a bounded numeric version without lexical comparison fallbacks.</summary>
        public static bool TryParse(string value, out UpdateVersion version)
        {
            version = null;
            if (value == null || value.Length > 128) return false;
            var match = Regex.Match(value, @"^[vV]?(\d+(?:\.\d+){1,3})(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$");
            if (!match.Success) return false;
            var numbers = match.Groups[1].Value.Split('.'); var parts = new int[4];
            for (var index = 0; index < numbers.Length; index++) if (!int.TryParse(numbers[index], NumberStyles.None, CultureInfo.InvariantCulture, out parts[index])) return false;
            version = new UpdateVersion(parts, match.Groups[2].Success ? match.Groups[2].Value.Split('.') : Array.Empty<string>());
            return true;
        }
        /// <inheritdoc />
        public int CompareTo(UpdateVersion other)
        {
            if (other == null) return 1;
            for (var index = 0; index < parts.Length; index++) { var order = parts[index].CompareTo(other.parts[index]); if (order != 0) return order; }
            if (preview.Length == 0 || other.preview.Length == 0) return other.preview.Length == 0 ? (preview.Length == 0 ? 0 : -1) : 1;
            for (var index = 0; index < Math.Min(preview.Length, other.preview.Length); index++)
            {
                var left = NormalizeIdentifier(preview[index]); var right = NormalizeIdentifier(other.preview[index]);
                var leftNumber = left.All(char.IsDigit); var rightNumber = right.All(char.IsDigit);
                var order = leftNumber && rightNumber ? (left.Length != right.Length ? left.Length.CompareTo(right.Length) : string.CompareOrdinal(left, right))
                    : leftNumber != rightNumber ? (leftNumber ? -1 : 1) : string.CompareOrdinal(left, right);
                if (order != 0) return order;
            }
            return preview.Length.CompareTo(other.preview.Length);
        }
        private static string NormalizeIdentifier(string value) => value.All(char.IsDigit) ? (value.TrimStart('0').Length == 0 ? "0" : value.TrimStart('0')) : value;
    }
}
