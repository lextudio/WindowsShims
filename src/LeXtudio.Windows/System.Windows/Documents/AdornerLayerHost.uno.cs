#if HAS_UNO
using System.Windows.Media;

namespace System.Windows.Documents;

internal interface IUnoAdornerLayerSource
{
    AdornerLayer AdornerLayer { get; }
}

internal interface IUnoAdornerLayerHost : IUnoAdornerLayerSource
{
    Visual AdornerScope { get; }
    void AddAdorner(Adorner adorner, int zOrder);
    void RemoveAdorner(Adorner adorner);
    void SetAdornerZOrder(Adorner adorner, int zOrder);
}
#endif
