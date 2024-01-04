using System;

namespace AnimLib;

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

/// <summary>
/// A dynamic property that can be animated or evaluated.
/// </summary>
public record DynProperty {
    internal object? _value;

    /// <summary>
    /// The unique ID of this property.
    /// </summary>
    public DynPropertyId Id { get; internal set; }

    /// <summary>
    /// The name of this property.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// The current value.
    /// </summary>
    public object? Value { 
        get {
            return this._value;
        }
        set {
            World.current.SetDynProperty(Id, value);
        }
    }

    internal DynProperty(string name, object? initialValue) {
        this.Id = World.current.CreateDynProperty(initialValue);
        this.Name = name;
        this._value = initialValue;
    }

    /// <summary>
    /// Create dynamic property that's only used for implicit conversions.
    /// </summary>
    protected DynProperty(object? initialValue) {
        Name = "__invalid__";
        _value = initialValue;
    }
}

/// <summary>
/// A dynamic property that can be animated or evaluated.
/// </summary>
public record DynProperty<T> : DynProperty {
    /// <summary>
    /// The current value.
    /// </summary>
    public new T? Value {
        get {
            return (T?)base.Value;
        }
        set {
            base.Value = value;
        }
    }

    internal DynProperty(string name, T initialValue) 
        : base(name, initialValue) {
    }

    internal DynProperty(T initial) : base(initial) {
    }

    /// <summary>
    /// Implicit conversion for assignment.
    /// </summary>
    public static implicit operator DynProperty<T>(T value) {
        var p = new DynProperty<T>(value);
        return p;
    }

    /// <summary>
    /// Implicit conversion for assignment.
    /// </summary>
    public static implicit operator T?(DynProperty<T> value) {
        return value.Value;
    }
}
