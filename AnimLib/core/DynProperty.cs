namespace AnimLib;

internal record DynProperty {
    internal object _value;

    public int Id { get; internal set; }
    public string Name { get; internal set; }
    public object Value { 
        get {
            return this._value;
        }
        set {
            World.current.SetDynProperty(Id, value);
        }
    }

    internal DynProperty(string name, object initialValue) {
        this.Id = World.current.CreateDynProperty(initialValue);
        this.Name = name;
        this._value = initialValue;
    }
}

