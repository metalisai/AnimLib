namespace AnimLib;

internal class RenderBufferState {
    public int Width;
    public int Height;
    public int BackendHandle;

    public RenderBufferState Clone() {
        return new RenderBufferState() {
            Width = Width,
            Height = Height,
            BackendHandle = BackendHandle,
        };
    }
}

/// <summary>
/// The buffer description for a render buffer.
/// </summary>
public class RenderBuffer {
    /// <summary>
    /// The width of the buffer.
    /// </summary>
    public int Width {
        get {
            return state.Width;
        }
    }
    /// <summary>
    /// The height of the buffer.
    /// </summary>
    public int Height {
        get {
            return state.Height;
        }
    }

    internal RenderBufferState state;

    /// <summary>
    /// Creates a new render buffer with given width and height.
    /// </summary>
    public RenderBuffer(int width, int height) {
        this.state = new RenderBufferState() {
            Width = width,
            Height = height,
        };

        state.BackendHandle = World.current.CreateRenderBuffer(width, height);
    }

    internal RenderBuffer(int width, int height, bool main) {
        this.state = new RenderBufferState() {
            Width = width,
            Height = height,
        };
        state.BackendHandle = World.current.CreateRenderBuffer(width, height, main);
    }
}
