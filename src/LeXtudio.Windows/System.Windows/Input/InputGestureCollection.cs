using System.Collections.Generic;

namespace System.Windows.Input
{
	/// <summary>Collection of <see cref="InputGesture"/> objects.</summary>
	public class InputGestureCollection : List<InputGesture>
	{
		public InputGestureCollection() { }
		public InputGestureCollection(IEnumerable<InputGesture> gestures) : base(gestures) { }
	}
}
