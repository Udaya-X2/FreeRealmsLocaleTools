﻿namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Provides extension methods to aid locale parsing.
/// </summary>
internal static class LocaleExtensions
{
    /// <summary>
    /// Invokes a transform function on each element of a sequence and returns the maximum
    /// <see langword="int"/> value, or the default value when the sequence is empty.
    /// </summary>
    /// <param name="source">A sequence of values.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="defaultValue">The default value to return when the sequence is empty.</param>
    /// <returns>
    /// The maximum value in the sequence, or <paramref name="defaultValue"/> when the sequence is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static int MaxOrDefault<T>(this IEnumerable<T> source, Func<T, int> selector, int defaultValue = 0)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        return source.Any() ? source.Max(selector) : defaultValue;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified tag starts with 'm'; otherwise <see langword="false"/>.
    /// </summary>
    public static bool IsMtag(this LocaleTag tag) => tag is LocaleTag.mcdt or LocaleTag.mcdn or LocaleTag.mgdt;
}
