namespace System.Windows.Documents;

public sealed partial class TextSelection : TextRange
{
	private TextPointer _anchorPosition;
	private TextPointer _movingPosition;

	internal TextSelection(System.Windows.Controls.Primitives.TextBoxBase owner, TextPointer position)
		: base(position, position)
	{
		Owner = owner;
		_anchorPosition = position;
		_movingPosition = position;
	}

	public TextSelection(
		Func<TextPointer> startProvider,
		Func<TextPointer> endProvider,
		Func<string> textProvider,
		Action<TextPointer, TextPointer> selectAction)
		: base(startProvider, endProvider, textProvider, selectAction)
	{
		_anchorPosition = startProvider();
		_movingPosition = endProvider();
	}

	internal System.Windows.Controls.Primitives.TextBoxBase? Owner { get; }

	public TextPointer AnchorPosition => _anchorPosition;

	public TextPointer MovingPosition => _movingPosition;

	public override TextPointer Start => _anchorPosition.CompareTo(_movingPosition) <= 0 ? _anchorPosition : _movingPosition;

	public override TextPointer End => _anchorPosition.CompareTo(_movingPosition) <= 0 ? _movingPosition : _anchorPosition;

	public override bool IsEmpty => AnchorPosition.CompareTo(MovingPosition) == 0;

	public override void Select(TextPointer startPosition, TextPointer endPosition)
	{
		base.Select(startPosition, endPosition);
		_anchorPosition = startPosition;
		_movingPosition = endPosition;
		SetTextCore(_anchorPosition.CompareTo(_movingPosition) == 0 ? string.Empty : Text);
		Owner?.NotifySelectionChanged();
	}
}
