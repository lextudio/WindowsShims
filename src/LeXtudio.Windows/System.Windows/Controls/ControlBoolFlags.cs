namespace System.Windows.Controls;

// WPF-internal bit flags stored on Control. On HAS_UNO these are used as simple bit masks
// on the _controlBoolField in the Control shim — no bit-packing optimization needed.
[Flags]
internal enum ControlBoolFlags : ushort
{
    ContentIsNotLogical = 0x0001,
    IsSpaceKeyDown      = 0x0002,
    HeaderIsNotLogical  = 0x0004,
    CommandDisabled     = 0x0008,
    ContentIsItem       = 0x0010,
    HeaderIsItem        = 0x0020,
    ScrollHostValid     = 0x0040,
    ContainsSelection   = 0x0080,
    VisualStateChangeSuspended = 0x0100,
}
