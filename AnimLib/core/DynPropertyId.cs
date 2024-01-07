/// <summary>
/// A handle to a dynamic property.
/// </summary>
public record struct DynPropertyId(int Id) {
    /// <summary>
    /// Compare two IDs.
    /// </summary>
    public readonly bool Equals(DynPropertyId other) {
        return Id == other.Id;
    }

    /// <summary>
    /// Get hash code.
    /// </summary>
    public override int GetHashCode() {
        return Id;
    }
}

