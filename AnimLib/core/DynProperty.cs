using System;
using System.Security.Cryptography.X509Certificates;
using MathNet.Numerics.Optimization;
using SharpFont.MultipleMasters;

namespace AnimLib;

/// <summary>
/// A dynamic property that can be animated or evaluated.
/// </summary>
public record DynProperty {
    internal object? _value;

    /// <summary>
    /// An invalid property setting which will be ignored.
    /// </summary>
    public static readonly DynProperty Invalid = new DynProperty(typeof(object));

    /// <summary>
    /// The unique ID of this property.
    /// </summary>
    public DynPropertyId Id { get; internal set; }

    /// <summary>
    /// The name of this property.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// The expected type of this property.
    /// </summary>
    public Type ExpectedType { get; internal set; } = typeof(object);

    internal Func<object?>? Evaluator { get; set; }

    internal static Func<DynPropertyId, object?>? _evaluationContext;
    public static Func<DynPropertyId, object?> EvaluationContext
    {
        get
        {
            if (_evaluationContext == null)
            {
                return World.current.GetDynProperty;
            }
            else
            {
                return _evaluationContext;
            }
        }
    }

    /// <summary>
    /// The current value.
    /// </summary>
    public object? Value
    {
        get
        {
            if (Evaluator != null)
            {
                return Evaluator();
            }
            return this._value;
        }
        set
        {
#if DEBUG
            if (value != null && !ExpectedType.IsAssignableFrom(value.GetType()))
            {
                throw new Exception($"Expected {ExpectedType} but got {value.GetType()}");
            }
#endif
            World.current.SetDynProperty(this.Id, value);
            _value = value;
        }
    }

    internal DynProperty(string name, object? initialValue, Type expectedType) {
        this.Id = World.current.CreateDynProperty(initialValue, this);
        this.Name = name;
        this._value = initialValue;
        this.ExpectedType = expectedType;
    }

    /// <summary>
    /// Create empty dynamic property. Setting it will be a no-op.
    /// </summary>
    protected DynProperty(Type expectedType) {
        Name = "__invalid__";
        Id = new DynPropertyId(0);
        ExpectedType = expectedType;
    }

    /// <summary>
    /// Create dynamic property that's only used for implicit conversions.
    /// Setting it will be a no-op.
    /// </summary>
    protected DynProperty(object? initialValue, Type expectedType) : this(expectedType) {
        _value = initialValue;
    }
}

/// <summary>
/// A dynamic property that can be animated or evaluated.
/// </summary>
public record DynProperty<T> : DynProperty {
    /// <summary>
    /// An invalid property setting which will be ignored.
    /// </summary>
    public static readonly new DynProperty<T> Invalid = new DynProperty<T>(default(T));

    /// <summary>
    /// Creates a empty property that's only used to store a value.
    /// Setting it will not affect world state.
    /// </summary>
    public static DynProperty<T> CreateEmpty(T initialValue) {
        return new DynProperty<T>(initialValue);
    }

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
        : base(name, initialValue, typeof(T)) {
    }

    private DynProperty(T? initial) : base(initial, typeof(T)) {
    }

    /*/// <summary>
    /// Implicit conversion for assignment.
    /// </summary>
    public static implicit operator DynProperty<T>(T value) {
        var p = new DynProperty<T>(value);
        return p;
    }*/

    /// <summary>
    /// Implicit conversion for assignment.
    /// </summary>
    public static implicit operator T?(DynProperty<T> value)
    {
        return value.Value;
    }
}
