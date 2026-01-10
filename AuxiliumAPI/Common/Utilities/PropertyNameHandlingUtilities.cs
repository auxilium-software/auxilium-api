
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;


namespace AuxiliumAPI.Common.Utilities
{
    public static class PropertyNameHandlingUtilities
    {
        public static string NormalizeKey(string displayName)
        {
            // remove accents/diacritics
            var normalizedString = displayName.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var cleaned = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // replace non-alphanumeric with underscores
            var snake = Regex.Replace(cleaned, @"[^a-zA-Z0-9]+", "_");

            // remove leading/trailing underscores
            snake = snake.Trim('_');

            // conver the whole string to lowercase
            snake = snake.ToLower();

            // collapse multiple underscores
            snake = Regex.Replace(snake, @"_+", "_");

            return string.IsNullOrEmpty(snake) ? "untitled" : snake;
        }

        public static (string storageKey, string displayName) HandlePropertyName(string rawName)
        {
            // check if it's already snake_case
            if (rawName.Equals(rawName, StringComparison.CurrentCultureIgnoreCase) && !rawName.Contains(' '))
            {
                // already normalized, generate display name
                var displayName = rawName.Replace('_', ' ');
                displayName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(displayName);
                return (rawName, displayName);
            }
            else
            {
                // pretty name provided, normalize for storage
                var storageKey = NormalizeKey(rawName);
                return (storageKey, rawName);
            }
        }
    }
}
