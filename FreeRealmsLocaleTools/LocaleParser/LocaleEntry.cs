namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Represents a locale entry in a Free Realms .dat file.
    /// </summary>
    public class LocaleEntry : IComparable<LocaleEntry>
    {
        public uint? Id { get; set; }
        public uint Hash { get; init; }
        public LocaleTag Tag { get; init; }
        public string Text { get; init; } = "";

        /// <summary>
        /// Compares this instance to the specified locale entry and returns an indication of their relative ID values.
        /// </summary>
        /// <returns><inheritdoc/></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public int CompareTo(LocaleEntry? other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (Id == null) throw new InvalidOperationException("Calling object's ID has not been initialized.");
            if (other.Id == null) throw new InvalidOperationException("Passed object's ID has not been initialized.");
            if (Id < other.Id) return -1;
            if (Id > other.Id) return 1;
            return 0;
        }

        /// <summary>
        /// Returns a string representation of this locale entry.
        /// </summary>
        public override string ToString() => $"ID: {Id}, Tag: {Tag}, Text: {Text}";
    }
}
