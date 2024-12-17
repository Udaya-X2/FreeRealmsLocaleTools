namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Provides extension methods to aid locale parsing.
/// </summary>
internal static class LocaleExtensions
{
    /// <summary>
    /// Invokes a transform function on each element of a sequence and returns the maximum
    /// <see langword="int"/> value, or the default value when the sequence is empty.
    /// </summary>
    /// <returns>
    /// The maximum value in the sequence, or <paramref name="defaultValue"/> when the sequence is empty.
    /// </returns>
    public static int MaxOrDefault<T>(this IEnumerable<T> source, Func<T, int> selector, int defaultValue = 0)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        else
        {
            return source.Any() ? source.Max(selector) : defaultValue;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified tag starts with 'm'; otherwise <see langword="false"/>.
    /// </summary>
    public static bool IsMtag(this LocaleTag tag) => tag is LocaleTag.mcdt or LocaleTag.mcdn or LocaleTag.mgdt;
}
