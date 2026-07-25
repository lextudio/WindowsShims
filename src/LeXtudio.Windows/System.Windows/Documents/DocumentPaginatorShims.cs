namespace System.Windows.Documents;

using System.ComponentModel;
using System.Windows.Media;

/// <summary>A single page returned by DocumentPaginator.GetPage.</summary>
public class DocumentPage : IDisposable
{
    public static readonly DocumentPage Missing = new();
    public Visual? Visual => null;
    public Size Size { get; }
    public DocumentPage() { }
    public DocumentPage(Visual visual, Size pageSize) { Size = pageSize; }
    public void Dispose() { }
}

public interface IDocumentPaginatorSource
{
    DocumentPaginator DocumentPaginator { get; }
}

public abstract class DocumentPaginator
{
    public abstract bool IsPageCountValid { get; }
    public abstract int PageCount { get; }
    public abstract Size PageSize { get; set; }
    public abstract IDocumentPaginatorSource Source { get; }
    public abstract DocumentPage GetPage(int pageNumber);
    public virtual void GetPageAsync(int pageNumber) => GetPageAsync(pageNumber, null);
    public virtual void GetPageAsync(int pageNumber, object? userState)
    {
        var page = GetPage(pageNumber);
        var args = new GetPageCompletedEventArgs(page, pageNumber, userState, default, null, false, null);
        GetPageCompleted?.Invoke(this, args);
    }
    public event GetPageCompletedEventHandler? GetPageCompleted;
    public event PagesChangedEventHandler? PagesChanged;
    protected virtual void OnPagesChanged(PagesChangedEventArgs e) => PagesChanged?.Invoke(this, e);
}

public class GetPageCompletedEventArgs : AsyncCompletedEventArgs
{
    public DocumentPage? DocumentPage { get; }
    public int PageNumber { get; }
    public GetPageCompletedEventArgs(DocumentPage? page, int pageNumber, object? userState, bool cancelled, Exception? error, bool morePages, int? pagesCompleted)
        : base(error, cancelled, userState) { DocumentPage = page; PageNumber = pageNumber; }
}

public class PagesChangedEventArgs : EventArgs
{
    public int Start { get; }
    public int Count { get; }
    public PagesChangedEventArgs(int start, int count) { Start = start; Count = count; }
}

public delegate void GetPageCompletedEventHandler(object sender, GetPageCompletedEventArgs e);
public delegate void PagesChangedEventHandler(object sender, PagesChangedEventArgs e);
