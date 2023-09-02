namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Represents a locale entry in a Free Realms .dat file.
    /// </summary>
    public class LocaleEntry : IEquatable<LocaleEntry?>
    {
        public int Id { get; set; } = -1;
        public uint Hash { get; set; }
        public LocaleTag Tag { get; set; }
        public string Text { get; set; } = "";

        /// <summary>
        /// Determines whether this instance and another specified object, which
        /// must also be a <see cref="LocaleEntry"/> object, have the same value.
        /// </summary>
        /// <returns><see langword="true"/> if <paramref name="other"/> is a <see cref="LocaleEntry"/> and its
        /// value is the same as this instance; otherwise, <see langword="false"/>. If <paramref name="other"/>
        /// is <see langword="null"/>, this method returns <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => Equals(obj as LocaleEntry);

        /// <summary>
        /// Determines whether this instance and another specified
        /// <see cref="LocaleEntry"/> object have the same value.
        /// </summary>
        /// <returns><see langword="true"/> if the value of the <paramref name="other"/> parameter is the same
        /// as the value of this instance; otherwise, <see langword="false"/>. If <paramref name="other"/>
        /// is <see langword="null"/>, this method returns <see langword="false"/>.</returns>
        public bool Equals(LocaleEntry? other) => other is not null
                                                  && Id == other.Id
                                                  && Hash == other.Hash
                                                  && Tag == other.Tag
                                                  && Text == other.Text;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(Id, Hash, Tag, Text);

        /// <summary>
        /// Returns a string representation of this locale entry.
        /// </summary>
        public override string ToString() => $"{Hash}\t{Tag}\t{Text}";

        public static bool operator ==(LocaleEntry? left, LocaleEntry? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(LocaleEntry? left, LocaleEntry? right)
        {
            return !(left == right);
        }
    }
}
