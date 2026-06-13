namespace System.Windows.Input
{
	// Key enum — subset used by AvalonEdit commands and gestures.
	// Values match WPF's System.Windows.Input.Key for documentation clarity,
	// but UnoEdit never performs keyboard dispatch from this enum directly.
	public enum Key
	{
		None        = 0,
		Back        = 2,
		Tab         = 3,
		Return      = 6,
		Enter       = 6,
		Escape      = 27,
		Space       = 18,
		PageUp      = 33,
		Prior       = 33,
		PageDown    = 34,
		Next        = 34,
		End         = 35,
		Home        = 36,
		Left        = 37,
		Up          = 38,
		Right       = 39,
		Down        = 40,
		Delete      = 46,
		Insert      = 45,
		F1  = 112, F2  = 113, F3  = 114, F4  = 115,
		F5  = 116, F6  = 117, F7  = 118, F8  = 119,
		F9  = 120, F10 = 121, F11 = 122, F12 = 123,
		// Letters (A–Z)
		A = 65, B = 66, C = 67, D = 68, E = 69, F = 70,
		G = 71, H = 72, I = 73, J = 74, K = 75, L = 76,
		M = 77, N = 78, O = 79, P = 80, Q = 81, R = 82,
		S = 83, T = 84, U = 85, V = 86, W = 87, X = 88,
		Y = 89, Z = 90,
        // Modifier keys
        LeftShift   = 160,
        RightShift  = 161,
        LeftCtrl    = 162,
        RightCtrl   = 163,
        LeftAlt     = 164,
        RightAlt    = 165,
        // IME
        ImeProcessed = 229,
        // Sentinel WPF uses when the real key is in SystemKey (ALT combos).
        System = 156,
	}
}
