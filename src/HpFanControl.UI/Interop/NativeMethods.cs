using System;
using System.Runtime.InteropServices;

namespace HpFanControl.UI.Interop;

public static partial class NativeMethods
{
    private const string AppIndicatorLib = "libayatana-appindicator3.so.1";
    private const string GtkLib = "libgtk-3.so.0";
    private const string GObjectLib = "libgobject-2.0.so.0";

    [LibraryImport(AppIndicatorLib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void app_indicator_set_icon(IntPtr indicator, string icon_name);
    [LibraryImport(AppIndicatorLib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr app_indicator_new(string id, string icon_name, int category);

    [LibraryImport(AppIndicatorLib)]
    public static partial void app_indicator_set_status(IntPtr indicator, int status);

    [LibraryImport(AppIndicatorLib)]
    public static partial void app_indicator_set_menu(IntPtr indicator, IntPtr menu);

    [LibraryImport(GtkLib)]
    public static partial void gtk_init(IntPtr argc, IntPtr argv);

    [LibraryImport(GtkLib)]
    public static partial IntPtr gtk_menu_new();

    [LibraryImport(GtkLib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr gtk_menu_item_new_with_label(string label);

    [LibraryImport(GtkLib)]
    public static partial void gtk_menu_shell_append(IntPtr menu_shell, IntPtr child);
    [LibraryImport(GtkLib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void gtk_menu_item_set_label(IntPtr menu_item, string label);

    [LibraryImport(GtkLib)]
    public static partial void gtk_widget_show_all(IntPtr widget);

    [LibraryImport(GtkLib)]
    public static partial IntPtr gtk_separator_menu_item_new();

    [LibraryImport(GtkLib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr gtk_check_menu_item_new_with_label(string label);

    [LibraryImport(GtkLib)]
    public static partial void gtk_check_menu_item_set_active(IntPtr check_menu_item, int is_active);

    [LibraryImport(GtkLib)]
    public static partial void gtk_check_menu_item_set_draw_as_radio(IntPtr check_menu_item, int draw_as_radio);

    [LibraryImport(GtkLib)]
    public static partial void gtk_menu_item_set_submenu(IntPtr menu_item, IntPtr submenu);
    [LibraryImport(GtkLib)]
    public static partial IntPtr gtk_window_list_toplevels();

    [LibraryImport(GtkLib)]
    public static partial IntPtr gtk_window_get_title(IntPtr window);

    [LibraryImport(GtkLib)]
    public static partial void g_list_free(IntPtr list);

    [LibraryImport(GtkLib)]
    public static partial void gtk_widget_set_sensitive(IntPtr widget, int sensitive);
    [LibraryImport(GtkLib)]
    public static partial void gtk_widget_hide(IntPtr widget);

    [LibraryImport(GtkLib)]
    public static partial void gtk_window_present(IntPtr window);

    [LibraryImport(GtkLib)]
    public static partial void gtk_window_begin_move_drag(IntPtr window, int button, int root_x, int root_y, uint timestamp);

    [LibraryImport(GtkLib)]
    public static partial IntPtr gtk_bin_get_child(IntPtr bin);

    [LibraryImport(GtkLib)]
    public static partial void gtk_event_controller_set_propagation_phase(IntPtr controller, int phase);

    [LibraryImport(GtkLib)]
    public static partial void gtk_window_begin_resize_drag(IntPtr window, int edge, int button, int root_x, int root_y, uint timestamp);
    public delegate void GCallback(IntPtr widget, IntPtr data);

    [LibraryImport(GObjectLib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint g_object_get_data(nint obj, string key);

    [LibraryImport(GObjectLib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial ulong g_signal_connect_data(
        IntPtr instance,
        string detailed_signal,
        GCallback c_handler,
        IntPtr data,
        IntPtr destroy_data,
        int connect_flags);
}