using System;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace AnimLib;

internal class ImguiContext {

    [StructLayout(LayoutKind.Sequential)]
    public struct ImDrawVert {
        public System.Numerics.Vector2 pos;
        public System.Numerics.Vector2 uv;
        public uint col;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IRect {
        public int x, y, w, h;
    }

    public struct ImDrawCmd {
        public int texture;
        public uint elemCount;
        public uint vOffset;
        public uint idxOffset;
        public (float, float, float, float) clipRect;
    }

    public record DrawList {
        public ImDrawVert[] vertices;
        public ushort[] indices;
        public ImDrawCmd[] commands;
    }

    enum AnimlibCallbackId : int {
        AnimlibCallbackId_None = 0,
        AnimlibCallbackId_Menuitem = 1,
        AnimlibCallbackId_DrawMenu = 2,
        AnimlibCallbackId_Play = 3,
        AnimlibCallbackId_Seek = 5,
    }

    public enum ImGuiStyleVar : int
    {
        // Enum name --------------------- // Member in ImGuiStyle structure (see ImGuiStyle for descriptions)
        Alpha,               // float     Alpha
        DisabledAlpha,       // float     DisabledAlpha
        WindowPadding,       // ImVec2    WindowPadding
        WindowRounding,      // float     WindowRounding
        WindowBorderSize,    // float     WindowBorderSize
        WindowMinSize,       // ImVec2    WindowMinSize
        WindowTitleAlign,    // ImVec2    WindowTitleAlign
        ChildRounding,       // float     ChildRounding
        ChildBorderSize,     // float     ChildBorderSize
        PopupRounding,       // float     PopupRounding
        PopupBorderSize,     // float     PopupBorderSize
        FramePadding,        // ImVec2    FramePadding
        FrameRounding,       // float     FrameRounding
        FrameBorderSize,     // float     FrameBorderSize
        ItemSpacing,         // ImVec2    ItemSpacing
        ItemInnerSpacing,    // ImVec2    ItemInnerSpacing
        IndentSpacing,       // float     IndentSpacing
        CellPadding,         // ImVec2    CellPadding
        ScrollbarSize,       // float     ScrollbarSize
        ScrollbarRounding,   // float     ScrollbarRounding
        GrabMinSize,         // float     GrabMinSize
        GrabRounding,        // float     GrabRounding
        TabRounding,         // float     TabRounding
        TabBarBorderSize,    // float     TabBarBorderSize
        ButtonTextAlign,     // ImVec2    ButtonTextAlign
        SelectableTextAlign, // ImVec2    SelectableTextAlign
        SeparatorTextBorderSize,// float  SeparatorTextBorderSize
        SeparatorTextAlign,  // ImVec2    SeparatorTextAlign
        SeparatorTextPadding,// ImVec2    SeparatorTextPadding
        COUNT
    }

    public enum ImGuiCol : int
    {
        Text,
        TextDisabled,
        WindowBg,              // Background of normal windows
        ChildBg,               // Background of child windows
        PopupBg,               // Background of popups, menus, tooltips windows
        Border,
        BorderShadow,
        FrameBg,               // Background of checkbox, radio button, plot, slider, text input
        FrameBgHovered,
        FrameBgActive,
        TitleBg,
        TitleBgActive,
        TitleBgCollapsed,
        MenuBarBg,
        ScrollbarBg,
        ScrollbarGrab,
        ScrollbarGrabHovered,
        ScrollbarGrabActive,
        CheckMark,
        SliderGrab,
        SliderGrabActive,
        Button,
        ButtonHovered,
        ButtonActive,
        Header,                // Header* colors are used for CollapsingHeader, TreeNode, Selectable, MenuItem
        HeaderHovered,
        HeaderActive,
        Separator,
        SeparatorHovered,
        SeparatorActive,
        ResizeGrip,            // Resize grip in lower-right and lower-left corners of windows.
        ResizeGripHovered,
        ResizeGripActive,
        Tab,                   // TabItem in a TabBar
        TabHovered,
        TabActive,
        TabUnfocused,
        TabUnfocusedActive,
        DockingPreview,        // Preview overlay color when about to docking something
        DockingEmptyBg,        // Background color for empty node (e.g. CentralNode with no window docked into it)
        PlotLines,
        PlotLinesHovered,
        PlotHistogram,
        PlotHistogramHovered,
        TableHeaderBg,         // Table header background
        TableBorderStrong,     // Table outer and header borders (prefer using Alpha=1.0 here)
        TableBorderLight,      // Table inner borders (prefer using Alpha=1.0 here)
        TableRowBg,            // Table row background (even rows)
        TableRowBgAlt,         // Table row background (odd rows)
        TextSelectedBg,
        DragDropTarget,        // Rectangle highlighting a drop target
        NavHighlight,          // Gamepad/keyboard: current highlighted item
        NavWindowingHighlight, // Highlight window when using CTRL+TAB
        NavWindowingDimBg,     // Darken/colorize entire screen behind the CTRL+TAB window list, when active
        ModalWindowDimBg,      // Darken/colorize entire screen behind a modal window, when one is active
        COUNT
    }

    public enum ImGuiWindowFlags
    {
        None                   = 0,
        NoTitleBar             = 1 << 0,   // Disable title-bar
        NoResize               = 1 << 1,   // Disable user resizing with the lower-right grip
        NoMove                 = 1 << 2,   // Disable user moving the window
        NoScrollbar            = 1 << 3,   // Disable scrollbars (window can still scroll with mouse or programmatically)
        NoScrollWithMouse      = 1 << 4,   // Disable user vertically scrolling with mouse wheel. On child window, mouse wheel will be forwarded to the parent unless NoScrollbar is also set.
        NoCollapse             = 1 << 5,   // Disable user collapsing window by double-clicking on it. Also referred to as Window Menu Button (e.g. within a docking node).
        AlwaysAutoResize       = 1 << 6,   // Resize every window to its content every frame
        NoBackground           = 1 << 7,   // Disable drawing background color (WindowBg, etc.) and outside border. Similar as using SetNextWindowBgAlpha(0.0f).
        NoSavedSettings        = 1 << 8,   // Never load/save settings in .ini file
        NoMouseInputs          = 1 << 9,   // Disable catching mouse, hovering test with pass through.
        MenuBar                = 1 << 10,  // Has a menu-bar
        HorizontalScrollbar    = 1 << 11,  // Allow horizontal scrollbar to appear (off by default). You may use SetNextWindowContentSize(ImVec2(width,0.0f)); prior to calling Begin() to specify width. Read code in imgui_demo in the "Horizontal Scrolling" section.
        NoFocusOnAppearing     = 1 << 12,  // Disable taking focus when transitioning from hidden to visible state
        NoBringToFrontOnFocus  = 1 << 13,  // Disable bringing window to front when taking focus (e.g. clicking on it or programmatically giving it focus)
        AlwaysVerticalScrollbar= 1 << 14,  // Always show vertical scrollbar (even if ContentSize.y < Size.y)
        AlwaysHorizontalScrollbar=1<< 15,  // Always show horizontal scrollbar (even if ContentSize.x < Size.x)
        AlwaysUseWindowPadding = 1 << 16,  // Ensure child windows without border uses style.WindowPadding (ignored by default for non-bordered child windows, because more convenient)
        NoNavInputs            = 1 << 18,  // No gamepad/keyboard navigation within the window
        NoNavFocus             = 1 << 19,  // No focusing toward this window with gamepad/keyboard navigation (e.g. skipped by CTRL+TAB)
        UnsavedDocument        = 1 << 20,  // Display a dot next to the title. When used in a tab/docking context, tab is selected when clicking the X + closure is not assumed (will wait for user to stop submitting the tab). Otherwise closure is assumed when pressing the X, so if you keep submitting the tab may reappear at end of tab bar.
        NoDocking              = 1 << 21,  // Disable docking of this window

        NoNav                  = NoNavInputs | NoNavFocus,
        NoDecoration           = NoTitleBar | NoResize | NoScrollbar | NoCollapse,
        NoInputs               = NoMouseInputs | NoNavInputs | NoNavFocus,

        // [Internal]
        NavFlattened           = 1 << 23,  // [BETA] On child window: allow gamepad/keyboard navigation to cross over parent border to this child or between sibling child windows.
        ChildWindow            = 1 << 24,  // Don't use! For internal use by BeginChild()
        Tooltip                = 1 << 25,  // Don't use! For internal use by BeginTooltip()
        Popup                  = 1 << 26,  // Don't use! For internal use by BeginPopup()
        Modal                  = 1 << 27,  // Don't use! For internal use by BeginPopupModal()
        ChildMenu              = 1 << 28,  // Don't use! For internal use by BeginMenu()
        DockNodeHost           = 1 << 29,  // Don't use! For internal use by Begin()/NewFrame()
    }

    public enum ImGuiDockNodeFlags
    {
        None                         = 0,
        KeepAliveOnly                = 1 << 0,   //       // Don't display the dockspace node but keep it alive. Windows docked into this dockspace node won't be undocked.
        //ImGuiDockNodeFlags_NoCentralNode              = 1 << 1,   //       // Disable Central Node (the node which can stay empty)
        NoDockingOverCentralNode     = 1 << 2,   //       // Disable docking over the Central Node, which will be always kept empty.
        PassthruCentralNode          = 1 << 3,   //       // Enable passthru dockspace: 1) DockSpace() will render a ImGuiCol_WindowBg background covering everything excepted the Central Node when empty. Meaning the host window should probably use SetNextWindowBgAlpha(0.0f) prior to Begin() when using this. 2) When Central Node is empty: let inputs pass-through + won't display a DockingEmptyBg background. See demo for details.
        NoDockingSplit               = 1 << 4,   //       // Disable other windows/nodes from splitting this node.
        NoResize                     = 1 << 5,   // Saved // Disable resizing node using the splitter/separators. Useful with programmatically setup dockspaces.
        AutoHideTabBar               = 1 << 6,   //       // Tab bar will automatically hide when there is a single window in the dock node.
        NoUndocking                  = 1 << 7,   //       // Disable undocking this node.
    }

    public enum ImGuiCond : int
    {
        None          = 0,        // No condition (always set the variable), same as _Always
        Always        = 1 << 0,   // No condition (always set the variable), same as _None
        Once          = 1 << 1,   // Set the variable once per runtime session (only the first call will succeed)
        FirstUseEver  = 1 << 2,   // Set the variable if the object/window has no persistently saved data (no entry in .ini file)
        Appearing     = 1 << 3,   // Set the variable if the object/window is appearing after being hidden/inactive (or the first time)
    }

    List<ImDrawVert> vertices = new List<ImDrawVert>();
    List<ushort> indices = new List<ushort>();
    List<ImDrawCmd> commands = new List<ImDrawCmd>();

    //typedef void (*imgui_animlib_draw_data_cb_t)(void *vtx_data, int vtx_bytes, void *idx_data, int idx_bytes);
    //typedef void (*imgui_animlib_draw_cb_t)(int i_offset, int i_count, int v_offset, int v_count, int texture, float clipX1, float clipY1, float clipX2, float clipY2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void imgui_animlib_draw_data_cb_t(IntPtr vtx_data, int vtx_count, int vtx_size, IntPtr idx_data, int idx_count, int idx_size);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void imgui_animlib_draw_cb_t(int i_offset, int i_count, int v_offset, int texture, float clipX1, float clipY1, float clipX2, float clipY2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void imgui_animlib_draw_menu_t();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void imgui_animlib_play_t();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void imgui_animlib_seek_t(float s);

    private const string LibraryName = "imgui_animlib";
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr imgui_animlib_init(imgui_animlib_draw_data_cb_t draw_data_cb, imgui_animlib_draw_cb_t draw_cb, [MarshalAs(UnmanagedType.LPStr)] string resources_path = null);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void imgui_animlib_shutdown(IntPtr ctx);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void imgui_animlib_update(IntPtr ctx, int width, int height, float dt, float mouseX, float mouseY, bool left, bool right, bool middle, float scrollDelta);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void imgui_animlib_begin_frame(IntPtr ctx);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void imgui_animlib_end_frame(IntPtr ctx);
    // EXPORT void imgui_animlib_get_fonts_texture_data(ImGuiAnimlibState* state, unsigned char** out_pixels, int* out_width, int* out_height, int* out_bytes_per_pixel);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void imgui_animlib_get_fonts_texture_data(IntPtr ctx, out IntPtr out_pixels, out int out_width, out int out_height, out int out_bytes_per_pixel);
    //EXPORT void imgui_animlib_set_font_texture(ImGuiAnimlibState* state, int texture);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void imgui_animlib_set_font_texture(IntPtr ctx, int texture);
    //EXPORT IRect imgui_animlib_scene_window(ImGuiAnimlibState* state, double view_aspect, int texture_handle);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IRect imgui_animlib_scene_window(IntPtr ctx, double view_aspect, int texture_handle, bool playing, float cursor, float cursosr_max);
    //EXPORT void imgui_animlib_set_cb(ImGuiAnimlibState* state, AnimlibCallbackId cb_id, void* cb);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    static extern void imgui_animlib_set_cb(IntPtr ctx, AnimlibCallbackId cb_id, IntPtr cb);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_begin")]
    public extern static bool Begin([MarshalAs(UnmanagedType.LPStr)] string name, ref bool show, ImGuiWindowFlags wflags = 0);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_end")]
    public extern static void End();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_text")]
    public extern static void Text([MarshalAs(UnmanagedType.LPStr)] string text);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_collapsing_header")]
    public extern static bool CollapsingHeader([MarshalAs(UnmanagedType.LPStr)] string text);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_button")]
    public extern static bool Button([MarshalAs(UnmanagedType.LPStr)] string text);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_same_line")]
    public extern static void SameLine();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_color_edit4")]
    public extern static bool ColorEdit4([MarshalAs(UnmanagedType.LPStr)] string label, ref Vector4 col);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_input_text")]
    extern static bool InputTextInternal([MarshalAs(UnmanagedType.LPStr)] string label, IntPtr buf, uint size);

    public static bool InputText(string label, ref string buf, uint size) {
        var bufPtr = Marshal.StringToHGlobalAnsi(buf);
        var ret = InputTextInternal(label, bufPtr, size);
        buf = Marshal.PtrToStringAnsi(bufPtr);
        Marshal.FreeHGlobal(bufPtr);
        return ret;
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_get_mouse_pos")]
    public extern static Vector2 GetMousePos();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_begin_drag_drop_target")]
    public extern static bool BeginDragDropTarget();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_end_drag_drop_target")]
    public extern static void EndDragDropTarget();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_accept_drag_drop_payload")]
    public extern static IntPtr AcceptDragDropPayload([MarshalAs(UnmanagedType.LPStr)] string name);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_set_next_window_size")]
    public extern static void SetNextWindowSize(Vector2 size, ImGuiCond cond_flags);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_columns")]
    public extern static void Columns(int count);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_separator")]
    public extern static void Separator();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_spacing")]
    public extern static void Spacing();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_push_style_var_float")]
    public extern static void PushStyleVar(ImGuiStyleVar idx, float val);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_push_style_var_float2")]
    public extern static void PushStyleVar(ImGuiStyleVar idx, ref Vector2 val);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_pop_style_var")]
    public extern static void PopStyleVar(int count = 1);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_begin_menu_bar")]
    public extern static bool BeginMenuBar();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_end_menu_bar")]
    public extern static void EndMenuBar();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_begin_menu")]
    public extern static bool BeginMenu([MarshalAs(UnmanagedType.LPStr)] string label);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_end_menu")]
    public extern static void EndMenu();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_menu_item")]
    public extern static bool MenuItem([MarshalAs(UnmanagedType.LPStr)] string item);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_get_window_size")]
    public extern static Vector2 GetWindowSize();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_push_style_color_u32")]
    public extern static void PushStyleColor(ImGuiCol idx, uint col);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_push_style_color_float4")]
    public extern static void PushStyleColor(ImGuiCol idx, ref Vector4 col);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_pop_style_color")]
    public extern static void PopStyleColor(int count = 1);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_input_double")]
    public extern static bool InputDouble([MarshalAs(UnmanagedType.LPStr)] string label, ref double v);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_input_float")]
    public extern static bool InputFloat([MarshalAs(UnmanagedType.LPStr)] string label, ref float v);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_input_float2")]
    public extern static bool InputFloat2([MarshalAs(UnmanagedType.LPStr)] string label, ref Vector2 v);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_input_float3")]
    public extern static bool InputFloat3([MarshalAs(UnmanagedType.LPStr)] string label, ref Vector3 v);

    //EXPORT bool imgui_animlib_is_mouse_clicked(int button, bool repeat);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_is_mouse_clicked")]
    public extern static bool IsMouseClicked(int button, bool repeat = false);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_is_mouse_down")]
    public extern static bool IsMouseDown(int button);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_list_box")]
    extern static bool ListBoxInternal([MarshalAs(UnmanagedType.LPStr)] string label, ref int current_item, IntPtr items, int items_count, int height_in_items = 1);

    // EXPORT bool imgui_animlib_begin_combo(const char *label, const char *preview_value, int flags);
    // EXPORT void imgui_animlib_end_combo();
    // EXPORT bool imgui_animlib_selectable(const char *label, bool *p_selected, int flags, const ImVec2 *size);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_begin_combo")]
    public extern static bool BeginCombo([MarshalAs(UnmanagedType.LPStr)] string label, [MarshalAs(UnmanagedType.LPStr)] string preview_value, int flags = 0);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_end_combo")]
    public extern static void EndCombo();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_selectable1")]
    public extern static bool Selectable([MarshalAs(UnmanagedType.LPStr)] string label, ref bool p_selected, int flags = 0, Vector2? size = null);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_selectable2")]
    public extern static bool Selectable([MarshalAs(UnmanagedType.LPStr)] string label, bool selected, int flags = 0, Vector2? size = null);
    //EXPORT void imgui_animlib_drag_drop_item(const char* item);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_drag_drop_item")]
    public extern static void DragDropItem([MarshalAs(UnmanagedType.LPStr)] string item);

    //EXPORT void imgui_animlib_fg_circle_filled(ImVec2 screen_pos, float radius, ImU32 col);
    //EXPORT void imgui_animlib_fg_text(ImVec2 screen_pos, ImU32 col, const char *text_begin);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_fg_circle_filled")]
    public extern static void FgCircleFilled(Vector2 screen_pos, float radius, uint col);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "imgui_animlib_fg_text")]
    public extern static void FgText(Vector2 screen_pos, uint col, [MarshalAs(UnmanagedType.LPStr)] string text_begin);

    public static bool ListBox(string label, ref int current_item, string[] items, int height_in_items = 1)
    {
        var itemsPtr = Marshal.AllocHGlobal(items.Length * IntPtr.Size);
        for (int i = 0; i < items.Length; i++)
        {
            var strPtr = Marshal.StringToHGlobalAnsi(items[i]);
            Marshal.WriteIntPtr(itemsPtr, i * IntPtr.Size, strPtr);
        }
        var ret = ListBoxInternal(label, ref current_item, itemsPtr, items.Length, height_in_items);
        for (int i = 0; i < items.Length; i++)
        {
            var strPtr = Marshal.ReadIntPtr(itemsPtr, i * IntPtr.Size);
            Marshal.FreeHGlobal(strPtr);
        }
        Marshal.FreeHGlobal(itemsPtr);
        return ret;
    }

    public event Action DrawMenuEvent;
    public event Action PlayEvent;
    public event Action<float> SeekEvent;

    void DrawMenuCallback() {
        DrawMenuEvent?.Invoke();
    }

    void PlayCallback() {
        PlayEvent?.Invoke();
    }

    void SeekCallback(float cursor) {
        SeekEvent?.Invoke(cursor);
    }

    void DrawDataCallback(IntPtr vtx_data, int vtx_count, int vtx_size, IntPtr idx_data, int idx_count, int idx_size) {
        for(int i = 0; i < vtx_count; i++) {
            var vtx = Marshal.PtrToStructure<ImDrawVert>(vtx_data + i*vtx_size);
            vertices.Add(vtx);
        }
        for(int i = 0; i < idx_count; i++) {
            var idx = Marshal.PtrToStructure<ushort>(idx_data + i*idx_size);
            indices.Add(idx);
        }
    }

    void DrawCallback(int i_offset, int i_count, int v_offset, int texture, float clipX1, float clipY1, float clipX2, float clipY2) {
        var cmd = new ImDrawCmd {
            texture = texture,
            elemCount = (uint)i_count,
            vOffset = (uint)v_offset,
            idxOffset = (uint)i_offset,
            clipRect = (clipX1, clipY1, clipX2, clipY2),
        };
        commands.Add(cmd);
    }

    string guid = Guid.NewGuid().ToString();

    int _width = 1920;
    int _height = 1080;
    public int Width { 
        get {
            return _width;
        }
    }
    public int Height { 
        get {
            return _height;
        }
    }

    imgui_animlib_draw_data_cb_t draw_data_cb;
    imgui_animlib_draw_cb_t draw_cb;
    imgui_animlib_draw_menu_t draw_menu_cb;
    imgui_animlib_play_t play_cb;
    imgui_animlib_seek_t seek_cb;
    IntPtr myCtx;

    IntPtr nativeCtx;
    Dictionary<int, Texture2D> textures = new Dictionary<int, Texture2D>();
    public ImguiContext(int width, int height, IPlatform platform) {
        draw_data_cb = new imgui_animlib_draw_data_cb_t(DrawDataCallback);
        draw_cb = new imgui_animlib_draw_cb_t(DrawCallback);
        draw_menu_cb = new imgui_animlib_draw_menu_t(DrawMenuCallback);
        play_cb = new imgui_animlib_play_t(PlayCallback);
        seek_cb = new imgui_animlib_seek_t(SeekCallback);

        var bin_path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        Console.WriteLine($"Bin path: {bin_path}");
        myCtx = imgui_animlib_init(draw_data_cb, draw_cb, bin_path);
        imgui_animlib_set_cb(myCtx, AnimlibCallbackId.AnimlibCallbackId_DrawMenu, Marshal.GetFunctionPointerForDelegate(draw_menu_cb));
        imgui_animlib_set_cb(myCtx, AnimlibCallbackId.AnimlibCallbackId_Play, Marshal.GetFunctionPointerForDelegate(play_cb));
        imgui_animlib_set_cb(myCtx, AnimlibCallbackId.AnimlibCallbackId_Seek, Marshal.GetFunctionPointerForDelegate(seek_cb));

        // TODO: free
        //


        int w, h, bpp;
        IntPtr pixels;
        imgui_animlib_get_fonts_texture_data(myCtx, out pixels, out w, out h, out bpp);
        var dstPixels = new byte[w*h*bpp];
        Marshal.Copy(pixels, dstPixels, 0, w*h*bpp);
        var tex = new Texture2D(guid) {
            Format = Texture2D.TextureFormat.RGBA8,
            Width = w,
            Height = h,
            RawData = dstPixels,
            GenerateMipmap = true,
        };
        platform.LoadTexture(tex);
        textures.Add(tex.GLHandle, tex);
        imgui_animlib_set_font_texture(myCtx, tex.GLHandle);

        Console.WriteLine($"Atlas w: {w} h: {h} bpp: {bpp} pixels: {pixels}");

        Console.WriteLine("Creating ImguiContext");

    }

    public void Update(int width, int height, float dt, Vector2 mousePos, bool left, bool right, bool middle, float scrollDelta) {
        _width = width;
        _height = height;

        this.vertices.Clear();
        this.indices.Clear();
        this.commands.Clear();

        imgui_animlib_update(myCtx, width, height, dt, mousePos.x, mousePos.y, left, right, middle, scrollDelta);
        imgui_animlib_begin_frame(myCtx);
    }

    public IRect SceneWindow(double viewAspect, int textureHandle, bool playing, float cursor, float cursor_max) {
        return imgui_animlib_scene_window(myCtx, viewAspect, textureHandle, playing, cursor, cursor_max);
    }

    public ImguiContext.DrawList Render() {
        imgui_animlib_end_frame(myCtx);
        var drawList = new DrawList {
            vertices = this.vertices.ToArray(),
            indices = this.indices.ToArray(),
            commands = this.commands.ToArray(),
        };
        return drawList;
    }

}
