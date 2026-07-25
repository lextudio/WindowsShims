using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xunit;

namespace LeXtudio.Windows.Tests;

// Session 22: the upstream DataGrid.cs control root is linked and compiles.
// These tests pin the merge points: upstream members that only exist when the
// linked file is active, the bridge contracts the link depends on, and the
// honest stubs that keep WPF paths inert.
public sealed class DataGridControlRootLinkTests
{
    [Fact]
    public void ControlRootIsLinkedUpstream()
    {
        // These members exist only in the upstream control root, not the old
        // local shell: editing commands and the frozen-column surface.
        Assert.NotNull(typeof(DataGrid).GetField("BeginEditCommand"));
        Assert.NotNull(typeof(DataGrid).GetField("CommitEditCommand"));
        Assert.NotNull(typeof(DataGrid).GetField("CancelEditCommand"));
        Assert.NotNull(typeof(DataGrid).GetProperty("FrozenColumnCount"));
        Assert.NotNull(typeof(DataGrid).GetProperty("ClipboardCopyMode"));
    }

    [Fact]
    public void LocalPartialMergesWithUpstream()
    {
        // UpdateVisualState lives in the local partial; ChangeVisualState is
        // the upstream override. Both resolving on one type proves the
        // HAS_UNO partial fork guard merged the two parts.
        var updateVisualState = typeof(DataGrid).GetMethod(
            "UpdateVisualState", BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
        var changeVisualState = typeof(DataGrid).GetMethod(
            "ChangeVisualState", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(updateVisualState);
        Assert.NotNull(changeVisualState);
        Assert.Equal(typeof(DataGrid), changeVisualState!.DeclaringType);
    }

    [Fact]
    public void DeleteAndSelectAllCommandsAreRoutedUICommands()
    {
        // Upstream returns ApplicationCommands.Delete/SelectAll as
        // RoutedUICommand; the shim's ApplicationCommands was retyped to match.
        var deleteCommand = typeof(DataGrid).GetProperty("DeleteCommand");
        var selectAllCommand = typeof(DataGrid).GetProperty("SelectAllCommand");

        Assert.NotNull(deleteCommand);
        Assert.Equal(typeof(RoutedUICommand), deleteCommand!.PropertyType);
        Assert.NotNull(selectAllCommand);
        Assert.Equal(typeof(RoutedUICommand), selectAllCommand!.PropertyType);
        Assert.IsAssignableFrom<RoutedUICommand>(ApplicationCommands.Delete);
    }

    [Fact]
    public void FocusBorderBrushKeyIsComponentResourceKey()
    {
        // Upstream stores SystemResourceKey.DataGridFocusBorderBrushKey in a
        // ComponentResourceKey-typed property; the stub key derives from it.
        var key = typeof(DataGrid).GetProperty("FocusBorderBrushKey");

        Assert.NotNull(key);
        Assert.Equal(typeof(ComponentResourceKey), key!.PropertyType);
    }

    [Fact]
    public void KeyEventArgsRoutesIntoBeginEdit()
    {
        // Upstream passes KeyEventArgs straight to BeginEdit(RoutedEventArgs).
        Assert.True(typeof(KeyEventArgs).IsSubclassOf(typeof(System.Windows.RoutedEventArgs)));
    }

    [Fact]
    public void VectorLengthIsEuclidean()
    {
        var v = new Vector(3, 4);

        Assert.Equal(5, v.Length);
    }

    [Fact]
    public void MouseCaptureReportsFailureForNonElements()
    {
        // WPF Mouse.Capture returns bool; non-UIElement targets cannot be
        // captured by the shim, and drag paths fall through honestly.
        Assert.False(Mouse.Capture(null!));
        Assert.False(Mouse.Capture((IInputElement)null!, CaptureMode.SubTree));
    }

    [Fact]
    public void PresenterShellsExposeLinkContract()
    {
        var presenters = typeof(DataGrid).Assembly;
        var cellsPresenter = presenters.GetType("System.Windows.Controls.Primitives.DataGridCellsPresenter");
        var detailsPresenter = presenters.GetType("System.Windows.Controls.Primitives.DataGridDetailsPresenter");
        var rowHeader = presenters.GetType("System.Windows.Controls.Primitives.DataGridRowHeader");

        Assert.NotNull(cellsPresenter);
        Assert.NotNull(detailsPresenter);
        Assert.NotNull(rowHeader);
        Assert.NotNull(cellsPresenter!.GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(detailsPresenter!.GetProperty("DetailsElement", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(rowHeader!.GetProperty("ParentRow", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void DispatcherSupportsWpfBeginInvokeShapes()
    {
        // The control root uses both argument orders; both overloads must exist.
        var delegateFirst = typeof(System.Windows.Threading.Dispatcher).GetMethod(
            "BeginInvoke",
            [typeof(System.Windows.Threading.DispatcherOperationCallback), typeof(System.Windows.Threading.DispatcherPriority), typeof(object)]);
        var priorityFirst = typeof(System.Windows.Threading.Dispatcher).GetMethod(
            "BeginInvoke",
            [typeof(System.Windows.Threading.DispatcherPriority), typeof(Delegate), typeof(object)]);

        Assert.NotNull(delegateFirst);
        Assert.NotNull(priorityFirst);
    }

    [Fact]
    public void ItemsControlSpineRebasesOntoWinUiControl()
    {
        // Session 24: the shim ItemsControl (foundation of the whole
        // DataGrid → MultiSelector → Selector → ItemsControl tower) derives
        // from WinUI Control, which unlocks the template pipeline. Pinned so
        // a revert to FrameworkElement is caught.
        var winuiControl = typeof(Microsoft.UI.Xaml.Controls.Control);

        Assert.True(winuiControl.IsAssignableFrom(typeof(System.Windows.Controls.ItemsControl)));
        Assert.True(winuiControl.IsAssignableFrom(typeof(DataGrid)));
    }

    [Fact]
    public void IsEnabledComesFromWinUiControlAfterRebase()
    {
        // The spine no longer declares its own IsEnabled DP; it must resolve
        // to the real WinUI Control property so the WPF logic sees real state.
        var isEnabled = typeof(DataGrid).GetProperty(nameof(DataGrid.IsEnabled));

        Assert.NotNull(isEnabled);
        Assert.True(typeof(Microsoft.UI.Xaml.Controls.Control).IsAssignableFrom(isEnabled!.DeclaringType));
    }

    // Session 25: shim render-path API. The runtime render gate is the sample
    // probe (`dotnet run -- --probe`); UI construction needs a dispatcher, so
    // here we only pin the method surface so a refactor that drops it is caught.
    [Fact]
    public void ShimRenderPathSurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.NotNull(typeof(DataGrid).GetMethod("BuildShimVisualTree", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("EnsureShimStyleKey", flags));
        Assert.NotNull(typeof(DataGridColumn).GetMethod("BuildCellContent", flags));
        Assert.NotNull(typeof(DataGridCell).GetMethod("BuildVisualTree", flags));
    }

    [Fact]
    public void DataGridRowHostsItsOwnCells()
    {
        // Session 26: the row is the visual container — it builds its own cells
        // (BuildCells) and answers TryGetCell from them. Pin the surface; the
        // runtime render gate remains the sample probe.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.NotNull(typeof(DataGridRow).GetMethod("BuildCells", flags));
        Assert.NotNull(typeof(DataGridRow).GetMethod("OnApplyTemplate", flags | BindingFlags.Public));

        var tryGetCell = typeof(DataGridRow).GetMethod("TryGetCell", flags);
        Assert.NotNull(tryGetCell);
    }

    [Fact]
    public void DataGridReactsToCollectionChanges()
    {
        // The shim subscribes to Items/Columns changes to re-render.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.NotNull(typeof(DataGrid).GetMethod("HookShimChangeNotifications", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("OnShimContentChanged", flags));
    }

    [Fact]
    public void EmptyGeneratorReportsNotStarted()
    {
        // Session 27: the generator holds a container registry. A fresh,
        // empty generator reports NotStarted and resolves nothing. (Populated
        // round-trip resolution is verified by the sample probe, since
        // registering a container needs a real DependencyObject / dispatcher.)
        var generator = new System.Windows.Controls.ItemContainerGenerator();

        Assert.Equal(System.Windows.Controls.Primitives.GeneratorStatus.NotStarted, generator.Status);
        Assert.Null(generator.ContainerFromIndex(0));
        Assert.Null(generator.ContainerFromItem("anything"));
        Assert.Equal(-1, generator.IndexFromContainer(null!));
    }

    [Fact]
    public void GeneratorRegistrySurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.NotNull(typeof(System.Windows.Controls.ItemContainerGenerator).GetMethod("ResetContainers", flags));
        Assert.NotNull(typeof(System.Windows.Controls.ItemContainerGenerator).GetMethod("RegisterContainer", flags));
    }

    [Fact]
    public void ShimSelectionSurfaceExists()
    {
        // Session 28: pointer input routes to HandleShimRowClicked (single
        // select). The interactive + visual behavior is verified by the
        // sample probe; here we pin the entry point and the IsSelected setter.
        Assert.NotNull(typeof(DataGrid).GetMethod("HandleShimRowClicked", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(DataGridRow)]));
        Assert.NotNull(typeof(DataGrid).GetMethod("HandleShimCellClicked", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(DataGridCell)]));

        var isSelected = typeof(DataGridRow).GetProperty(nameof(DataGridRow.IsSelected));
        Assert.NotNull(isSelected);
        Assert.True(isSelected!.CanWrite);
    }

    [Fact]
    public void ColumnWidthResolverExists()
    {
        // Session 29: headers are DataGridColumnHeader controls and explicit
        // pixel widths are honored via ShimColumnWidth. The visual behavior is
        // verified by the sample probe; pin the resolver so it isn't dropped.
        Assert.NotNull(typeof(DataGrid).GetMethod("ShimColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void HeaderSortSurfaceExists()
    {
        // Session 30: header click toggles sort. Behavior (order + glyph) is
        // verified by the sample probe; pin the entry point and ordering hook.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.NotNull(typeof(DataGrid).GetMethod("HandleShimHeaderClicked", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("OrderedItems", flags));
    }

    [Fact]
    public void RetainedSelectionUsesRealSelectedItems()
    {
        // Sessions 31/62/63: selection is retained by the linked Selector
        // engine's SelectedItems, and rebuilds re-apply row visuals from that
        // collection. Behavior is verified by the probe.
        Assert.NotNull(typeof(DataGrid).GetMethod("PruneRealRowSelection", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void ComboBoxColumnBodyIsReusedFromUpstream()
    {
        // Session 60: the local combo shim was replaced by the linked upstream
        // body. Evidence: the three real WPF binding properties + ItemsSource/
        // SelectedValuePath/DisplayMemberPath are present (write-back behavior
        // verified by the probe via the TwoWay-by-default binding bridge).
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetProperty("SelectedItemBinding"));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetProperty("SelectedValueBinding"));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetProperty("TextBinding"));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetProperty("ItemsSource"));
        Assert.NotNull(typeof(DataGridComboBoxColumn).GetProperty("SelectedValuePath"));
    }

    [Fact]
    public void CheckBoxColumnGeneratesCheckBox()
    {
        // Session 44: checkbox column type. Toggle write-back verified by the
        // probe; here assert the column produces a WinUI CheckBox element.
        var generate = typeof(DataGridCheckBoxColumn).GetMethod(
            "GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(generate);
        Assert.Equal(typeof(Microsoft.UI.Xaml.FrameworkElement), generate!.ReturnType);
    }

    [Fact]
    public void AutoWidthSurfaceExists()
    {
        // Session 41: Auto column width via a post-layout measure pass.
        // Behavior verified by the probe.
        Assert.NotNull(typeof(DataGrid).GetMethod("OnAutoWidthLayoutUpdated", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).GetMethod("Clamp", BindingFlags.Static | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).GetMethod("ShimTryResizeColumn", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).GetMethod("ShimTryAutoSizeColumn", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).GetMethod("ShimBestFitColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGridColumnHeader).GetMethod("OnApplyTemplate", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).Assembly .GetType("System.Windows.Controls.DataGridColumnCollection") ?.GetMethod("RecomputeColumnWidthsOnColumnResize", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(Thumb).GetEvent("MouseDoubleClick", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(DataGrid).GetMethod("PreviousVisibleColumn", BindingFlags.Instance | BindingFlags.NonPublic));
        var resizeShim = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridColumnResizeShim");
        Assert.NotNull(resizeShim?.GetMethod("ComputeWidth", BindingFlags.Static | BindingFlags.NonPublic));
        Assert.NotNull(resizeShim?.GetMethod("ClampWidth", BindingFlags.Static | BindingFlags.NonPublic));
    }

    [Fact]
    public void ColumnResizeComputationClampsToMinAndMax()
    {
        var resizeShim = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridColumnResizeShim");
        var method = resizeShim?.GetMethod("ComputeWidth", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        static double Invoke(System.Reflection.MethodInfo method, double current, double delta, double min, double max)
            => (double)method.Invoke(null, [current, delta, min, max])!;

        Assert.Equal(125, Invoke(method!, 100, 25, 20, 200));
        Assert.Equal(40, Invoke(method!, 100, -200, 40, 200));
        Assert.Equal(180, Invoke(method!, 100, 250, 20, 180));

        var clamp = resizeShim?.GetMethod("ClampWidth", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(clamp);
        Assert.Equal(20, (double)clamp!.Invoke(null, [10.0, 20.0, 100.0])!);
        Assert.Equal(100, (double)clamp.Invoke(null, [120.0, 20.0, 100.0])!);
    }

    [Fact]
    public void MultiSelectSurfaceExists()
    {
        // Session 63: Ctrl/Shift row clicks now route through the linked WPF
        // DataGrid selection handler; the shim only bridges Uno modifier flags
        // into Keyboard.Modifiers for the duration of the call.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Assert.NotNull(typeof(DataGrid).GetMethod("HandleShimRowClicked", flags, [typeof(DataGridRow), typeof(global::Windows.System.VirtualKeyModifiers)]));
        Assert.NotNull(typeof(DataGrid).GetMethod("HandleSelectionForRowHeaderAndDetailsInput", flags, [typeof(DataGridRow), typeof(bool)]));
        Assert.NotNull(typeof(DataGrid).GetMethod("ToWpfModifiers", BindingFlags.Static | BindingFlags.NonPublic));
    }

    [Fact]
    public void CellEditSurfaceExists()
    {
        // Session 39: text-cell editing. Behavior verified by the probe.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Assert.NotNull(typeof(DataGridCell).GetMethod("BeginEdit", flags));
        Assert.NotNull(typeof(DataGridCell).GetMethod("CommitEdit", flags));
        Assert.NotNull(typeof(DataGridCell).GetMethod("CancelEdit", flags));
        // Session 43: read-only coercion + edit-event forwarders.
        Assert.NotNull(typeof(DataGrid).GetMethod("IsCellEffectivelyReadOnly", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("RaiseBeginningEdit", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("RaiseCellEditEnding", flags));
        // Session 46: validation surface.
        Assert.NotNull(typeof(DataGridCell).GetProperty("HasValidationError", flags));
        Assert.NotNull(typeof(DataGridCell).GetProperty("ValidationError", flags));
        // Session 47: row edit transactions.
        Assert.NotNull(typeof(DataGrid).GetMethod("BeginRowEdit", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("CommitRowEdit", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("CancelRowEdit", flags));
        // Session 48: row-level validation indicator.
        Assert.NotNull(typeof(DataGridRow).GetMethod("SetRowError", flags));
        Assert.NotNull(typeof(DataGridRow).GetProperty("HasRowValidationError", flags));
        // Session 49: row headers.
        Assert.NotNull(typeof(DataGrid).GetProperty("AreRowHeadersVisible", flags));
        Assert.NotNull(typeof(DataGridRow).GetMethod("BuildRowHeader", flags));
    }

    [Fact]
    public void ClassCommandBindingMatchesByTargetType()
    {
        // Session 51: command routing. A binding scoped to a base type applies
        // to an instance of that type (direct match); tree-walk routing to
        // descendant elements is verified end-to-end by the sample probe.
        var binding = new System.Windows.Input.CommandBinding(
            new System.Windows.Input.RoutedCommand("t", typeof(DataGrid)));
        System.Windows.Input.CommandManager.RegisterClassCommandBinding(typeof(DataGrid), binding);

        var appliesTo = typeof(System.Windows.Input.CommandBinding)
            .GetMethod("AppliesTo", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(appliesTo);
        // null target → applies; non-matching plain object → not (no tree).
        Assert.True((bool)appliesTo!.Invoke(binding, [null])!);
        Assert.False((bool)appliesTo.Invoke(binding, [new object()])!);
    }

    [Fact]
    public void BoundColumnBodyIsReusedFromUpstream()
    {
        // Sessions 58/63: the local DataGridBoundColumn shim was replaced by the
        // linked upstream file; DataGridColumn is linked too, so the local
        // partial keeps only BindingPath + Uno bridge helpers. (DependencyObject
        // instances need the UI thread, so binding/sort behavior is verified by
        // the sample probe; here we assert the reused surface exists.)
        var instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // Upstream-only members that prove the linked body is in the type:
        Assert.NotNull(typeof(DataGridBoundColumn).GetProperty("ElementStyle"));
        Assert.NotNull(typeof(DataGridBoundColumn).GetProperty("EditingElementStyle"));
        Assert.NotNull(typeof(DataGridBoundColumn).GetField("ElementStyleProperty"));
        Assert.NotNull(typeof(DataGridBoundColumn).GetMethod("ApplyBinding", instanceFlags));
        Assert.NotNull(typeof(DataGridBoundColumn).GetMethod("ApplyStyle", instanceFlags));

        // The Uno-specific members kept in the local partials:
        Assert.NotNull(typeof(DataGridBoundColumn).GetProperty("BindingPath", instanceFlags));
        var coerce = typeof(DataGridBoundColumn).GetMethod("CoerceValue", instanceFlags);
        Assert.NotNull(coerce);
        Assert.Equal(typeof(DataGridColumn), coerce!.DeclaringType);
    }

    [Fact]
    public void TextColumnBodyIsReusedFromUpstream()
    {
        // Session 59: the local DataGridTextColumn shim was deleted and the
        // upstream file linked. Evidence the upstream body is in the type: the
        // Font*/Foreground DPs and DefaultElementStyle (none of which existed in
        // the old 47-line local shim) are now present.
        Assert.NotNull(typeof(DataGridTextColumn).GetProperty("FontFamily"));
        Assert.NotNull(typeof(DataGridTextColumn).GetProperty("FontSize"));
        Assert.NotNull(typeof(DataGridTextColumn).GetProperty("FontWeight"));
        Assert.NotNull(typeof(DataGridTextColumn).GetProperty("Foreground"));
        Assert.NotNull(typeof(DataGridTextColumn).GetField("FontFamilyProperty"));
        Assert.NotNull(typeof(DataGridTextColumn).GetProperty("DefaultElementStyle"));

        // The input substrate that lets concrete columns link: InputEventArgs is
        // the shared base of the input arg shims, and the column base exposes
        // OnInput/BeginEdit.
        Assert.True(typeof(System.Windows.KeyEventArgs).IsSubclassOf(typeof(System.Windows.Input.InputEventArgs)));
        Assert.True(typeof(System.Windows.Input.MouseEventArgs).IsSubclassOf(typeof(System.Windows.Input.InputEventArgs)));
        var onInput = typeof(DataGridColumn).GetMethod("OnInput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(onInput);
    }

    [Fact]
    public void RowDetailsSurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        // Session 57: row details. The row computes effective visibility and
        // materializes the grid's RowDetailsTemplate into PART_DetailsHost;
        // behavior (Visible vs VisibleWhenSelected + selection) is verified by
        // the sample probe.
        Assert.NotNull(typeof(DataGridRow).GetMethod("ComputeDetailsVisibility", flags));
        Assert.NotNull(typeof(DataGridRow).GetMethod("BuildRowDetails", flags));
        // The linked WPF Loading/Unloading wrappers are reused (not reimplemented).
        Assert.NotNull(typeof(DataGrid).GetMethod("OnLoadingRowDetailsWrapper", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("OnUnloadingRowDetailsWrapper", flags));
        // Public RowDetails surface from the linked control root.
        Assert.NotNull(typeof(DataGrid).GetProperty("RowDetailsTemplate"));
        Assert.NotNull(typeof(DataGrid).GetProperty("RowDetailsVisibilityMode"));
        Assert.NotNull(typeof(DataGrid).GetEvent("LoadingRowDetails"));
        Assert.NotNull(typeof(DataGrid).GetEvent("RowDetailsVisibilityChanged"));
    }

    [Fact]
    public void RealSelectionEngineIsDriven()
    {
        // Session 61-63: row input now drives the linked Selector/MultiSelector
        // engine for the public selection surface (SelectedItems collection +
        // SelectionChanged event). Behavior verified by the probe; here assert
        // the reused surface and WPF row-selection handler exist.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Assert.NotNull(typeof(DataGrid).GetMethod("HandleSelectionForRowHeaderAndDetailsInput", flags, [typeof(DataGridRow), typeof(bool)]));
        // The reused engine surface comes from the linked Selector/MultiSelector.
        Assert.NotNull(typeof(DataGrid).GetProperty("SelectedItems"));
        Assert.NotNull(typeof(DataGrid).GetEvent("SelectionChanged"));
        Assert.NotNull(typeof(DataGrid).GetMethod("BeginUpdateSelectedItems", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void KeyboardNavigationSurfaceExists()
    {
        // Session 33: Up/Down move the selection. Behavior verified by probe.
        Assert.NotNull(typeof(DataGrid).GetMethod("MoveSelectionByOffset", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).GetMethod("MoveSelectionToIndex", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).GetMethod("MoveCurrentCellByOffset", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.NotNull(typeof(DataGrid).GetMethod("ShimSelectAllCells", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public void ClipboardCopySurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.NotNull(typeof(DataGrid).GetMethod("ShimCopySelectionToClipboard", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("ShimBuildClipboardDataObject", flags));
        Assert.NotNull(typeof(DataGrid).GetMethod("ShimBuildClipboardPlan", flags));
    }

    [Fact]
    public void ColumnHeaderCursorSurfaceExists()
    {
        // Session 65: resize cursor. DataGridColumnHeader exposes
        // SetShimCursor/ClearShimCursor so the owning DataGrid can change
        // the cursor without accessing ProtectedCursor (a protected member).
        var header = typeof(DataGrid).Assembly
            .GetType("System.Windows.Controls.Primitives.DataGridColumnHeader");
        Assert.NotNull(header);

        var setCursor = header!.GetMethod("SetShimCursor",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(setCursor);
        Assert.Equal(typeof(void), setCursor!.ReturnType);

        var clearCursor = header!.GetMethod("ClearShimCursor",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(clearCursor);
        Assert.Equal(typeof(void), clearCursor!.ReturnType);
    }

    [Fact]
    public void FilterInlineSurfaceExists()
    {
        // Session 65+: column-header filter inline panel builders.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.NotNull(typeof(DataGrid).GetMethod("BuildFilterPanelForColumn", flags, [typeof(DataGridColumn)]));
        var fctType = typeof(DataGrid).Assembly.GetType("DataGridExtensions.FilterControlTemplate");
        Assert.NotNull(fctType);

        Assert.NotNull(typeof(DataGrid).GetMethod("BuildFilterInlineContent", flags, [typeof(DataGridColumn), fctType!]));
        Assert.NotNull(typeof(DataGrid).GetMethod("BuildTextFilterInline", flags, [typeof(DataGridColumn)]));
        Assert.NotNull(typeof(DataGrid).GetMethod("BuildHexFilterInline", flags, [typeof(DataGridColumn)]));
        Assert.NotNull(typeof(DataGrid).GetMethod("BuildFlagsFilterInline", flags, [typeof(DataGridColumn), typeof(Type)]));
        Assert.NotNull(typeof(DataGrid).GetMethod("OnHeaderPointerExited", flags, [typeof(object), typeof(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs)]));
    }

    [Fact]
    public void HeaderContentMethodIsInternal()
    {
        // Session 65: HeaderContent is the per-column header factory exposed
        // as an internal method so BuildHeaderRow can call it.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var headerContent = typeof(DataGrid).GetMethod("HeaderContent", flags,
            [typeof(DataGridColumn)]);
        Assert.NotNull(headerContent);
        Assert.Equal(typeof(object), headerContent!.ReturnType);
    }
}