namespace System.Windows.Input;

// Session 59: minimal shim for WPF's input-event base. The linked DataGrid
// column bodies declare OnInput(InputEventArgs) and downcast to the concrete
// input arg types, so those shim types derive from this (as in real WPF:
// InputEventArgs : RoutedEventArgs, with Key/Mouse/TextComposition args below).
public class InputEventArgs : System.Windows.RoutedEventArgs
{
    public InputEventArgs() { }

    public InputEventArgs(InputDevice? inputDevice, int timestamp)
    {
        Device = inputDevice;
        Timestamp = timestamp;
    }

    public InputDevice? Device { get; set; }

    public int Timestamp { get; }
}
