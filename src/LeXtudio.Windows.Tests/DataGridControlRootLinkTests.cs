using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NUnit.Framework;

namespace LeXtudio.Windows.Tests;

// Session 22: the upstream DataGrid.cs control root is linked and compiles.
// These tests pin the merge points: upstream members that only exist when the
// linked file is active, the bridge contracts the link depends on, and the
// honest stubs that keep WPF paths inert.
[TestFixture]
public sealed class DataGridControlRootLinkTests
{
    [Test]
    public void ControlRootIsLinkedUpstream()
    {
        // These members exist only in the upstream control root, not the old
        // local shell: editing commands and the frozen-column surface.
        Assert.That(typeof(DataGrid).GetField("BeginEditCommand"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetField("CommitEditCommand"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetField("CancelEditCommand"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetProperty("FrozenColumnCount"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetProperty("ClipboardCopyMode"), Is.Not.Null);
    }

    [Test]
    public void LocalPartialMergesWithUpstream()
    {
        // UpdateVisualState lives in the local partial; ChangeVisualState is
        // the upstream override. Both resolving on one type proves the
        // HAS_UNO partial fork guard merged the two parts.
        var updateVisualState = typeof(DataGrid).GetMethod(
            "UpdateVisualState", BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
        var changeVisualState = typeof(DataGrid).GetMethod(
            "ChangeVisualState", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(updateVisualState, Is.Not.Null);
        Assert.That(changeVisualState, Is.Not.Null);
        Assert.That(changeVisualState!.DeclaringType, Is.EqualTo(typeof(DataGrid)));
    }

    [Test]
    public void DeleteAndSelectAllCommandsAreRoutedUICommands()
    {
        // Upstream returns ApplicationCommands.Delete/SelectAll as
        // RoutedUICommand; the shim's ApplicationCommands was retyped to match.
        var deleteCommand = typeof(DataGrid).GetProperty("DeleteCommand");
        var selectAllCommand = typeof(DataGrid).GetProperty("SelectAllCommand");

        Assert.That(deleteCommand, Is.Not.Null);
        Assert.That(deleteCommand!.PropertyType, Is.EqualTo(typeof(RoutedUICommand)));
        Assert.That(selectAllCommand, Is.Not.Null);
        Assert.That(selectAllCommand!.PropertyType, Is.EqualTo(typeof(RoutedUICommand)));
        Assert.That(ApplicationCommands.Delete, Is.InstanceOf<RoutedUICommand>());
    }

    [Test]
    public void FocusBorderBrushKeyIsComponentResourceKey()
    {
        // Upstream stores SystemResourceKey.DataGridFocusBorderBrushKey in a
        // ComponentResourceKey-typed property; the stub key derives from it.
        var key = typeof(DataGrid).GetProperty("FocusBorderBrushKey");

        Assert.That(key, Is.Not.Null);
        Assert.That(key!.PropertyType, Is.EqualTo(typeof(ComponentResourceKey)));
    }

    [Test]
    public void KeyEventArgsRoutesIntoBeginEdit()
    {
        // Upstream passes KeyEventArgs straight to BeginEdit(RoutedEventArgs).
        Assert.That(typeof(KeyEventArgs).IsSubclassOf(typeof(System.Windows.RoutedEventArgs)), Is.True);
    }

    [Test]
    public void VectorLengthIsEuclidean()
    {
        var v = new Vector(3, 4);

        Assert.That(v.Length, Is.EqualTo(5));
    }

    [Test]
    public void MouseCaptureReportsFailureForNonElements()
    {
        // WPF Mouse.Capture returns bool; non-UIElement targets cannot be
        // captured by the shim, and drag paths fall through honestly.
        Assert.That(Mouse.Capture(null!), Is.False);
        Assert.That(Mouse.Capture((IInputElement)null!, CaptureMode.SubTree), Is.False);
    }

    [Test]
    public void PresenterShellsExposeLinkContract()
    {
        var presenters = typeof(DataGrid).Assembly;
        var cellsPresenter = presenters.GetType("System.Windows.Controls.Primitives.DataGridCellsPresenter");
        var detailsPresenter = presenters.GetType("System.Windows.Controls.Primitives.DataGridDetailsPresenter");
        var rowHeader = presenters.GetType("System.Windows.Controls.Primitives.DataGridRowHeader");

        Assert.That(cellsPresenter, Is.Not.Null);
        Assert.That(detailsPresenter, Is.Not.Null);
        Assert.That(rowHeader, Is.Not.Null);
        Assert.That(
            cellsPresenter!.GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            detailsPresenter!.GetProperty("DetailsElement", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
        Assert.That(
            rowHeader!.GetProperty("ParentRow", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null);
    }

    [Test]
    public void DispatcherSupportsWpfBeginInvokeShapes()
    {
        // The control root uses both argument orders; both overloads must exist.
        var delegateFirst = typeof(System.Windows.Threading.Dispatcher).GetMethod(
            "BeginInvoke",
            [typeof(System.Windows.Threading.DispatcherOperationCallback), typeof(System.Windows.Threading.DispatcherPriority), typeof(object)]);
        var priorityFirst = typeof(System.Windows.Threading.Dispatcher).GetMethod(
            "BeginInvoke",
            [typeof(System.Windows.Threading.DispatcherPriority), typeof(Delegate), typeof(object)]);

        Assert.That(delegateFirst, Is.Not.Null);
        Assert.That(priorityFirst, Is.Not.Null);
    }

    [Test]
    public void ItemsControlSpineRebasesOntoWinUiControl()
    {
        // Session 24: the shim ItemsControl (foundation of the whole
        // DataGrid → MultiSelector → Selector → ItemsControl tower) derives
        // from WinUI Control, which unlocks the template pipeline. Pinned so
        // a revert to FrameworkElement is caught.
        var winuiControl = typeof(Microsoft.UI.Xaml.Controls.Control);

        Assert.That(winuiControl.IsAssignableFrom(typeof(System.Windows.Controls.ItemsControl)), Is.True);
        Assert.That(winuiControl.IsAssignableFrom(typeof(DataGrid)), Is.True);
    }

    [Test]
    public void IsEnabledComesFromWinUiControlAfterRebase()
    {
        // The spine no longer declares its own IsEnabled DP; it must resolve
        // to the real WinUI Control property so the WPF logic sees real state.
        var isEnabled = typeof(DataGrid).GetProperty(nameof(DataGrid.IsEnabled));

        Assert.That(isEnabled, Is.Not.Null);
        Assert.That(
            typeof(Microsoft.UI.Xaml.Controls.Control).IsAssignableFrom(isEnabled!.DeclaringType),
            Is.True,
            "IsEnabled should be inherited from WinUI Control, not redeclared on the shim spine.");
    }

    // Session 25: shim render-path API. The runtime render gate is the sample
    // probe (`dotnet run -- --probe`); UI construction needs a dispatcher, so
    // here we only pin the method surface so a refactor that drops it is caught.
    [Test]
    public void ShimRenderPathSurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.That(typeof(DataGrid).GetMethod("BuildShimVisualTree", flags), Is.Not.Null,
            "DataGrid.BuildShimVisualTree() drives the shim render path.");
        Assert.That(typeof(DataGrid).GetMethod("EnsureShimStyleKey", flags), Is.Not.Null,
            "DataGrid.EnsureShimStyleKey() assigns the code-built template.");
        Assert.That(
            typeof(DataGridColumn).GetMethod("BuildCellContent", flags),
            Is.Not.Null,
            "DataGridColumn.BuildCellContent() exposes element generation to the render path.");
        Assert.That(
            typeof(DataGridCell).GetMethod("BuildVisualTree", flags),
            Is.Not.Null,
            "DataGridCell.BuildVisualTree() populates a cell from its column.");
    }

    [Test]
    public void DataGridRowHostsItsOwnCells()
    {
        // Session 26: the row is the visual container — it builds its own cells
        // (BuildCells) and answers TryGetCell from them. Pin the surface; the
        // runtime render gate remains the sample probe.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.That(typeof(DataGridRow).GetMethod("BuildCells", flags), Is.Not.Null,
            "DataGridRow.BuildCells() builds the row's cells from the owner columns.");
        Assert.That(
            typeof(DataGridRow).GetMethod("OnApplyTemplate", flags | BindingFlags.Public),
            Is.Not.Null,
            "DataGridRow overrides OnApplyTemplate to build its cells when templated.");

        var tryGetCell = typeof(DataGridRow).GetMethod("TryGetCell", flags);
        Assert.That(tryGetCell, Is.Not.Null, "DataGridRow.TryGetCell(int) exposes generated cells.");
    }

    [Test]
    public void DataGridReactsToCollectionChanges()
    {
        // The shim subscribes to Items/Columns changes to re-render.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.That(typeof(DataGrid).GetMethod("HookShimChangeNotifications", flags), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("OnShimContentChanged", flags), Is.Not.Null);
    }

    [Test]
    public void EmptyGeneratorReportsNotStarted()
    {
        // Session 27: the generator holds a container registry. A fresh,
        // empty generator reports NotStarted and resolves nothing. (Populated
        // round-trip resolution is verified by the sample probe, since
        // registering a container needs a real DependencyObject / dispatcher.)
        var generator = new System.Windows.Controls.ItemContainerGenerator();

        Assert.That(generator.Status, Is.EqualTo(System.Windows.Controls.Primitives.GeneratorStatus.NotStarted));
        Assert.That(generator.ContainerFromIndex(0), Is.Null);
        Assert.That(generator.ContainerFromItem("anything"), Is.Null);
        Assert.That(generator.IndexFromContainer(null!), Is.EqualTo(-1));
    }

    [Test]
    public void GeneratorRegistrySurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.That(typeof(System.Windows.Controls.ItemContainerGenerator).GetMethod("ResetContainers", flags), Is.Not.Null);
        Assert.That(typeof(System.Windows.Controls.ItemContainerGenerator).GetMethod("RegisterContainer", flags), Is.Not.Null);
    }

    [Test]
    public void ShimSelectionSurfaceExists()
    {
        // Session 28: pointer input routes to HandleShimRowClicked (single
        // select). The interactive + visual behavior is verified by the
        // sample probe; here we pin the entry point and the IsSelected setter.
        Assert.That(
            typeof(DataGrid).GetMethod("HandleShimRowClicked",
                BindingFlags.Instance | BindingFlags.NonPublic, [typeof(DataGridRow)]),
            Is.Not.Null,
            "DataGrid.HandleShimRowClicked(DataGridRow) is the selection entry point.");
        Assert.That(
            typeof(DataGrid).GetMethod("HandleShimCellClicked", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.HandleShimCellClicked(DataGridCell) routes into the linked cell-selection engine.");

        var isSelected = typeof(DataGridRow).GetProperty(nameof(DataGridRow.IsSelected));
        Assert.That(isSelected, Is.Not.Null);
        Assert.That(isSelected!.CanWrite, Is.True, "DataGridRow.IsSelected must be settable to drive the highlight.");
    }

    [Test]
    public void ColumnWidthResolverExists()
    {
        // Session 29: headers are DataGridColumnHeader controls and explicit
        // pixel widths are honored via ShimColumnWidth. The visual behavior is
        // verified by the sample probe; pin the resolver so it isn't dropped.
        Assert.That(
            typeof(DataGrid).GetMethod("ShimColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.ShimColumnWidth(DataGridColumn) resolves the per-column width.");
    }

    [Test]
    public void HeaderSortSurfaceExists()
    {
        // Session 30: header click toggles sort. Behavior (order + glyph) is
        // verified by the sample probe; pin the entry point and ordering hook.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.That(typeof(DataGrid).GetMethod("HandleShimHeaderClicked", flags), Is.Not.Null,
            "DataGrid.HandleShimHeaderClicked(DataGridColumn) is the sort entry point.");
        Assert.That(typeof(DataGrid).GetMethod("OrderedItems", flags), Is.Not.Null,
            "DataGrid.OrderedItems() applies the active sort to the render order.");
    }

    [Test]
    public void RetainedSelectionUsesRealSelectedItems()
    {
        // Sessions 31/62/63: selection is retained by the linked Selector
        // engine's SelectedItems, and rebuilds re-apply row visuals from that
        // collection. Behavior is verified by the probe.
        Assert.That(
            typeof(DataGrid).GetMethod("PruneRealRowSelection", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid prunes SelectedItems when the backing item leaves the collection.");
    }

    [Test]
    public void ComboBoxColumnBodyIsReusedFromUpstream()
    {
        // Session 60: the local combo shim was replaced by the linked upstream
        // body. Evidence: the three real WPF binding properties + ItemsSource/
        // SelectedValuePath/DisplayMemberPath are present (write-back behavior
        // verified by the probe via the TwoWay-by-default binding bridge).
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty("SelectedItemBinding"), Is.Not.Null);
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty("SelectedValueBinding"), Is.Not.Null);
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty("TextBinding"), Is.Not.Null);
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty("ItemsSource"), Is.Not.Null);
        Assert.That(typeof(DataGridComboBoxColumn).GetProperty("SelectedValuePath"), Is.Not.Null);
    }

    [Test]
    public void CheckBoxColumnGeneratesCheckBox()
    {
        // Session 44: checkbox column type. Toggle write-back verified by the
        // probe; here assert the column produces a WinUI CheckBox element.
        var generate = typeof(DataGridCheckBoxColumn).GetMethod(
            "GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(generate, Is.Not.Null);
        Assert.That(generate!.ReturnType, Is.EqualTo(typeof(Microsoft.UI.Xaml.FrameworkElement)));
    }

    [Test]
    public void AutoWidthSurfaceExists()
    {
        // Session 41: Auto column width via a post-layout measure pass.
        // Behavior verified by the probe.
        Assert.That(
            typeof(DataGrid).GetMethod("OnAutoWidthLayoutUpdated", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.OnAutoWidthLayoutUpdated runs the Auto/Star/clamp width pass.");
        Assert.That(
            typeof(DataGrid).GetMethod("Clamp", BindingFlags.Static | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.Clamp applies MinWidth/MaxWidth (session 42).");
        Assert.That(
            typeof(DataGrid).GetMethod("ShimTryResizeColumn", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.ShimTryResizeColumn commits a user resize into a pixel column width.");
        Assert.That(
            typeof(DataGrid).GetMethod("ShimTryAutoSizeColumn", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.ShimTryAutoSizeColumn commits a best-fit column width for double-click resize.");
        Assert.That(
            typeof(DataGrid).GetMethod("ShimBestFitColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.ShimBestFitColumnWidth measures realized header/filter/data cells for best-fit resize.");
        Assert.That(typeof(DataGrid).GetMethod("TryBeginHeaderResize", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("ContinueHeaderResize", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("EndHeaderResize", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("HeaderResizeEdgeAt", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("ResolveHeaderResizeColumn", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("PreviousVisibleColumn", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("OnHeaderDoubleTapped", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        var resizeShim = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridColumnResizeShim");
        Assert.That(resizeShim?.GetMethod("ComputeWidth", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null,
            "DataGridColumnResizeShim.ComputeWidth clamps gripper deltas without requiring a UI dispatcher.");
        Assert.That(resizeShim?.GetMethod("ClampWidth", BindingFlags.Static | BindingFlags.NonPublic), Is.Not.Null,
            "DataGridColumnResizeShim.ClampWidth clamps best-fit widths without requiring a UI dispatcher.");
    }

    [Test]
    public void ColumnResizeComputationClampsToMinAndMax()
    {
        var resizeShim = typeof(DataGrid).Assembly.GetType("System.Windows.Controls.DataGridColumnResizeShim");
        var method = resizeShim?.GetMethod("ComputeWidth", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);

        static double Invoke(System.Reflection.MethodInfo method, double current, double delta, double min, double max)
            => (double)method.Invoke(null, [current, delta, min, max])!;

        Assert.That(Invoke(method!, 100, 25, 20, 200), Is.EqualTo(125));
        Assert.That(Invoke(method!, 100, -200, 40, 200), Is.EqualTo(40));
        Assert.That(Invoke(method!, 100, 250, 20, 180), Is.EqualTo(180));

        var clamp = resizeShim?.GetMethod("ClampWidth", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(clamp, Is.Not.Null);
        Assert.That((double)clamp!.Invoke(null, [10.0, 20.0, 100.0])!, Is.EqualTo(20));
        Assert.That((double)clamp.Invoke(null, [120.0, 20.0, 100.0])!, Is.EqualTo(100));
    }

    [Test]
    public void MultiSelectSurfaceExists()
    {
        // Session 63: Ctrl/Shift row clicks now route through the linked WPF
        // DataGrid selection handler; the shim only bridges Uno modifier flags
        // into Keyboard.Modifiers for the duration of the call.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Assert.That(
            typeof(DataGrid).GetMethod("HandleShimRowClicked", flags,
                [typeof(DataGridRow), typeof(global::Windows.System.VirtualKeyModifiers)]),
            Is.Not.Null,
            "DataGrid.HandleShimRowClicked(row, modifiers) drives multi-select.");
        Assert.That(
            typeof(DataGrid).GetMethod("HandleSelectionForRowHeaderAndDetailsInput", flags,
                [typeof(DataGridRow), typeof(bool)]),
            Is.Not.Null,
            "The linked WPF row-header/details selection path is reused for row clicks.");
        Assert.That(
            typeof(DataGrid).GetMethod("ToWpfModifiers", BindingFlags.Static | BindingFlags.NonPublic),
            Is.Not.Null,
            "Uno pointer modifiers are bridged to WPF Keyboard.Modifiers.");
    }

    [Test]
    public void CellEditSurfaceExists()
    {
        // Session 39: text-cell editing. Behavior verified by the probe.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Assert.That(typeof(DataGridCell).GetMethod("BeginEdit", flags), Is.Not.Null);
        Assert.That(typeof(DataGridCell).GetMethod("CommitEdit", flags), Is.Not.Null);
        Assert.That(typeof(DataGridCell).GetMethod("CancelEdit", flags), Is.Not.Null);
        // Session 43: read-only coercion + edit-event forwarders.
        Assert.That(typeof(DataGrid).GetMethod("IsCellEffectivelyReadOnly", flags), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("RaiseBeginningEdit", flags), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("RaiseCellEditEnding", flags), Is.Not.Null);
        // Session 46: validation surface.
        Assert.That(typeof(DataGridCell).GetProperty("HasValidationError", flags), Is.Not.Null);
        Assert.That(typeof(DataGridCell).GetProperty("ValidationError", flags), Is.Not.Null);
        // Session 47: row edit transactions.
        Assert.That(typeof(DataGrid).GetMethod("BeginRowEdit", flags), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("CommitRowEdit", flags), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("CancelRowEdit", flags), Is.Not.Null);
        // Session 48: row-level validation indicator.
        Assert.That(typeof(DataGridRow).GetMethod("SetRowError", flags), Is.Not.Null);
        Assert.That(typeof(DataGridRow).GetProperty("HasRowValidationError", flags), Is.Not.Null);
        // Session 49: row headers.
        Assert.That(typeof(DataGrid).GetProperty("AreRowHeadersVisible", flags), Is.Not.Null);
        Assert.That(typeof(DataGridRow).GetMethod("BuildRowHeader", flags), Is.Not.Null);
    }

    [Test]
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
        Assert.That(appliesTo, Is.Not.Null);
        // null target → applies; non-matching plain object → not (no tree).
        Assert.That((bool)appliesTo!.Invoke(binding, [null])!, Is.True);
        Assert.That((bool)appliesTo.Invoke(binding, [new object()])!, Is.False);
    }

    [Test]
    public void BoundColumnBodyIsReusedFromUpstream()
    {
        // Sessions 58/63: the local DataGridBoundColumn shim was replaced by the
        // linked upstream file; DataGridColumn is linked too, so the local
        // partial keeps only BindingPath + Uno bridge helpers. (DependencyObject
        // instances need the UI thread, so binding/sort behavior is verified by
        // the sample probe; here we assert the reused surface exists.)
        var instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // Upstream-only members that prove the linked body is in the type:
        Assert.That(typeof(DataGridBoundColumn).GetProperty("ElementStyle"), Is.Not.Null);
        Assert.That(typeof(DataGridBoundColumn).GetProperty("EditingElementStyle"), Is.Not.Null);
        Assert.That(typeof(DataGridBoundColumn).GetField("ElementStyleProperty"), Is.Not.Null);
        Assert.That(typeof(DataGridBoundColumn).GetMethod("ApplyBinding", instanceFlags), Is.Not.Null);
        Assert.That(typeof(DataGridBoundColumn).GetMethod("ApplyStyle", instanceFlags), Is.Not.Null);

        // The Uno-specific members kept in the local partials:
        Assert.That(typeof(DataGridBoundColumn).GetProperty("BindingPath", instanceFlags), Is.Not.Null,
            "BindingPath helper remains in the local partial.");
        var coerce = typeof(DataGridBoundColumn).GetMethod("CoerceValue", instanceFlags);
        Assert.That(coerce, Is.Not.Null,
            "CoerceValue bridge exists because the shim DP system runs no coerce callbacks.");
        Assert.That(coerce!.DeclaringType, Is.EqualTo(typeof(DataGridColumn)));
    }

    [Test]
    public void TextColumnBodyIsReusedFromUpstream()
    {
        // Session 59: the local DataGridTextColumn shim was deleted and the
        // upstream file linked. Evidence the upstream body is in the type: the
        // Font*/Foreground DPs and DefaultElementStyle (none of which existed in
        // the old 47-line local shim) are now present.
        Assert.That(typeof(DataGridTextColumn).GetProperty("FontFamily"), Is.Not.Null);
        Assert.That(typeof(DataGridTextColumn).GetProperty("FontSize"), Is.Not.Null);
        Assert.That(typeof(DataGridTextColumn).GetProperty("FontWeight"), Is.Not.Null);
        Assert.That(typeof(DataGridTextColumn).GetProperty("Foreground"), Is.Not.Null);
        Assert.That(typeof(DataGridTextColumn).GetField("FontFamilyProperty"), Is.Not.Null);
        Assert.That(typeof(DataGridTextColumn).GetProperty("DefaultElementStyle"), Is.Not.Null);

        // The input substrate that lets concrete columns link: InputEventArgs is
        // the shared base of the input arg shims, and the column base exposes
        // OnInput/BeginEdit.
        Assert.That(typeof(System.Windows.KeyEventArgs).IsSubclassOf(typeof(System.Windows.Input.InputEventArgs)), Is.True);
        Assert.That(typeof(System.Windows.Input.MouseEventArgs).IsSubclassOf(typeof(System.Windows.Input.InputEventArgs)), Is.True);
        var onInput = typeof(DataGridColumn).GetMethod("OnInput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(onInput, Is.Not.Null);
    }

    [Test]
    public void RowDetailsSurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        // Session 57: row details. The row computes effective visibility and
        // materializes the grid's RowDetailsTemplate into PART_DetailsHost;
        // behavior (Visible vs VisibleWhenSelected + selection) is verified by
        // the sample probe.
        Assert.That(typeof(DataGridRow).GetMethod("ComputeDetailsVisibility", flags), Is.Not.Null,
            "DataGridRow.ComputeDetailsVisibility mirrors OnCoerceDetailsVisibility.");
        Assert.That(typeof(DataGridRow).GetMethod("BuildRowDetails", flags), Is.Not.Null,
            "DataGridRow.BuildRowDetails materializes the details template.");
        // The linked WPF Loading/Unloading wrappers are reused (not reimplemented).
        Assert.That(typeof(DataGrid).GetMethod("OnLoadingRowDetailsWrapper", flags), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetMethod("OnUnloadingRowDetailsWrapper", flags), Is.Not.Null);
        // Public RowDetails surface from the linked control root.
        Assert.That(typeof(DataGrid).GetProperty("RowDetailsTemplate"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetProperty("RowDetailsVisibilityMode"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetEvent("LoadingRowDetails"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetEvent("RowDetailsVisibilityChanged"), Is.Not.Null);
    }

    [Test]
    public void RealSelectionEngineIsDriven()
    {
        // Session 61-63: row input now drives the linked Selector/MultiSelector
        // engine for the public selection surface (SelectedItems collection +
        // SelectionChanged event). Behavior verified by the probe; here assert
        // the reused surface and WPF row-selection handler exist.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Assert.That(
            typeof(DataGrid).GetMethod("HandleSelectionForRowHeaderAndDetailsInput", flags,
                [typeof(DataGridRow), typeof(bool)]),
            Is.Not.Null,
            "DataGrid row clicks reuse the linked WPF selection engine.");
        // The reused engine surface comes from the linked Selector/MultiSelector.
        Assert.That(typeof(DataGrid).GetProperty("SelectedItems"), Is.Not.Null);
        Assert.That(typeof(DataGrid).GetEvent("SelectionChanged"), Is.Not.Null);
        Assert.That(
            typeof(DataGrid).GetMethod("BeginUpdateSelectedItems", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null, "MultiSelector batch API is reused, not reimplemented.");
    }

    [Test]
    public void KeyboardNavigationSurfaceExists()
    {
        // Session 33: Up/Down move the selection. Behavior verified by probe.
        Assert.That(
            typeof(DataGrid).GetMethod("MoveSelectionByOffset", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.MoveSelectionByOffset(int) drives arrow-key selection.");
        Assert.That(
            typeof(DataGrid).GetMethod("MoveSelectionToIndex", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.MoveSelectionToIndex(int) drives Home/End (session 33/34).");
        Assert.That(
            typeof(DataGrid).GetMethod("MoveCurrentCellByOffset", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.MoveCurrentCellByOffset(rowDelta, columnDelta, extendSelection) drives cell keyboard navigation.");
        Assert.That(
            typeof(DataGrid).GetMethod("ShimSelectAllCells", BindingFlags.Instance | BindingFlags.NonPublic),
            Is.Not.Null,
            "DataGrid.ShimSelectAllCells() drives Ctrl+A for cell/row selection.");
    }

    [Test]
    public void ClipboardCopySurfaceExists()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.That(typeof(DataGrid).GetMethod("ShimCopySelectionToClipboard", flags), Is.Not.Null,
            "DataGrid.ShimCopySelectionToClipboard writes the selected-cell/row payload to Clipboard.");
        Assert.That(typeof(DataGrid).GetMethod("ShimBuildClipboardDataObject", flags), Is.Not.Null,
            "DataGrid.ShimBuildClipboardDataObject builds Text/UnicodeText/CSV payloads for probes and command routing.");
        Assert.That(typeof(DataGrid).GetMethod("ShimBuildClipboardPlan", flags), Is.Not.Null,
            "DataGrid.ShimBuildClipboardPlan maps SelectedCells/SelectedItems/CurrentCell to copy rows and columns.");
    }

    [Test]
    public void ColumnHeaderCursorSurfaceExists()
    {
        // Session 65: resize cursor. DataGridColumnHeader exposes
        // SetShimCursor/ClearShimCursor so the owning DataGrid can change
        // the cursor without accessing ProtectedCursor (a protected member).
        var header = typeof(DataGrid).Assembly
            .GetType("System.Windows.Controls.Primitives.DataGridColumnHeader");
        Assert.That(header, Is.Not.Null);

        var setCursor = header!.GetMethod("SetShimCursor",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(setCursor, Is.Not.Null,
            "DataGridColumnHeader.SetShimCursor() sets the resize cursor.");
        Assert.That(setCursor!.ReturnType, Is.EqualTo(typeof(void)));

        var clearCursor = header!.GetMethod("ClearShimCursor",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(clearCursor, Is.Not.Null,
            "DataGridColumnHeader.ClearShimCursor() clears the custom cursor.");
        Assert.That(clearCursor!.ReturnType, Is.EqualTo(typeof(void)));
    }

    [Test]
    public void FilterFlyoutSurfaceExists()
    {
        // Session 65: column-header filter flyout builders.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        Assert.That(typeof(DataGrid).GetMethod("BuildFilterButtonForColumn", flags,
            [typeof(DataGridColumn)]), Is.Not.Null,
            "DataGrid.BuildFilterButtonForColumn builds the funnel-icon button.");
        var fctType = typeof(DataGrid).Assembly.GetType("DataGridExtensions.FilterControlTemplate");
        Assert.That(fctType, Is.Not.Null);

        Assert.That(typeof(DataGrid).GetMethod("BuildFilterFlyoutContent", flags,
            [typeof(DataGridColumn), fctType!]), Is.Not.Null,
            "DataGrid.BuildFilterFlyoutContent dispatches to Text/Hex/Flags flyout builders.");
        Assert.That(typeof(DataGrid).GetMethod("BuildTextFilterFlyout", flags,
            [typeof(DataGridColumn)]), Is.Not.Null,
            "DataGrid.BuildTextFilterFlyout builds a text-filter flyout.");
        Assert.That(typeof(DataGrid).GetMethod("BuildHexFilterFlyout", flags,
            [typeof(DataGridColumn)]), Is.Not.Null,
            "DataGrid.BuildHexFilterFlyout builds a hex-filter flyout.");
        Assert.That(typeof(DataGrid).GetMethod("BuildFlagsFilterFlyout", flags,
            [typeof(DataGridColumn), typeof(Type)]), Is.Not.Null,
            "DataGrid.BuildFlagsFilterFlyout builds a flags-enum filter flyout.");
        Assert.That(typeof(DataGrid).GetMethod("OnHeaderPointerExited", flags,
            [typeof(object), typeof(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs)]), Is.Not.Null,
            "DataGrid.OnHeaderPointerExited clears the resize cursor when the pointer leaves.");
    }

    [Test]
    public void HeaderContentMethodIsInternal()
    {
        // Session 65: HeaderContent is the per-column header factory exposed
        // as an internal method so BuildHeaderRow can call it.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var headerContent = typeof(DataGrid).GetMethod("HeaderContent", flags,
            [typeof(DataGridColumn)]);
        Assert.That(headerContent, Is.Not.Null,
            "DataGrid.HeaderContent(DataGridColumn) is the internal header-content factory.");
        Assert.That(headerContent!.ReturnType, Is.EqualTo(typeof(object)));
    }
}
