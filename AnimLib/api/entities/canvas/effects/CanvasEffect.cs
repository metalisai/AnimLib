using System.Collections.Generic;

namespace AnimLib;

/// <summary>
/// An effect applied to a canvas after rendering.
/// </summary>
public class CanvasEffect {
    internal Dictionary<string, DynProperty> properties = new ();
}
