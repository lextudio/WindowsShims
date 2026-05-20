namespace System.Windows.Documents;

/// <summary>
/// WPF pagination interface — stub for HAS_UNO (pagination/printing not supported).
/// FlowDocument implements this in WPF; the methods are gated #if !HAS_UNO in the upstream file.
/// </summary>
public interface IDocumentPaginatorSource
{
    DocumentPaginator DocumentPaginator { get; }
}

/// <summary>Abstract base for WPF document paginators. Stub on HAS_UNO.</summary>
public abstract class DocumentPaginator
{
    public abstract bool IsPageCountValid { get; }
    public abstract int PageCount { get; }
    public abstract Size PageSize { get; set; }
    public abstract IDocumentPaginatorSource Source { get; }
}
