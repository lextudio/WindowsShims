namespace System.Windows.Documents;

public partial class TextRange
{
	private Func<TextPointer> _startProvider;
	private Func<TextPointer> _endProvider;
	private Func<string> _textProvider;
	private Action<TextPointer, TextPointer> _selectAction;
	private TextPointer? _start;
	private TextPointer? _end;
	private string _text = string.Empty;

	public TextRange(TextPointer position1, TextPointer position2)
	{
		InitializeRange(position1, position2);
		_startProvider = () => _start ?? position1;
		_endProvider = () => _end ?? position2;
		_textProvider = () => _text;
		_selectAction = InitializeRange;
	}

	public TextRange(
		Func<TextPointer> startProvider,
		Func<TextPointer> endProvider,
		Func<string> textProvider,
		Action<TextPointer, TextPointer> selectAction)
	{
		_startProvider = startProvider;
		_endProvider = endProvider;
		_textProvider = textProvider;
		_selectAction = selectAction;
	}

	public virtual TextPointer Start => _startProvider();

	public virtual TextPointer End => _endProvider();

	public virtual string Text => _textProvider();

	public virtual bool IsEmpty => string.IsNullOrEmpty(Text);

	public virtual void ApplyPropertyValue(DependencyProperty formattingProperty, object value)
	{
	}

	public virtual void Select(TextPointer startPosition, TextPointer endPosition)
		=> _selectAction(startPosition, endPosition);

	protected void SetTextCore(string? text)
		=> _text = text ?? string.Empty;

	private void InitializeRange(TextPointer position1, TextPointer position2)
	{
		ArgumentNullException.ThrowIfNull(position1);
		ArgumentNullException.ThrowIfNull(position2);

		if (!position1.IsInSameDocument(position2))
		{
			throw new ArgumentException(SR.InDifferentTextContainers);
		}

		if (position1.CompareTo(position2) <= 0)
		{
			_start = position1;
			_end = position2;
		}
		else
		{
			_start = position2;
			_end = position1;
		}
	}
}
