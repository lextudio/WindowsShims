# Code Reuse Opportunities — WPF DataGrid & Base-Type Upstream Linking

This document surveys every upstream WPF source file under
`ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/`
that the `LeXtudio.Windows` shim project could link (or link more fully) via its
existing `#if HAS_UNO` fork-guard + `*.uno.cs` partial strategy.

---

## 1. DataGrid-Specific Files — Done (51/52)

**52** `DataGrid*.cs` files exist upstream. **51** are already linked in the
csproj (as `_upstream.cs` or `.upstream.cs` links). The **one remaining**
file is **not worth linking**:

| Upstream file | Lines | Local | Why not link |
|---|---|---|---|
| `DataGridHyperlinkColumn.cs` | 309 | `DataGridHyperlinkColumn.cs` (113 行) | 大量 WPF-only 类型：`System.Windows.Documents.Hyperlink`、`InlineUIContainer`、`TextCompositionEventArgs`、`Dispatcher`、`Keyboard.PreviewKeyDownEvent`。本地实现使用 `Tapped` 事件，更简单且已验证可行。 |

**结论**: DataGrid 文件级复用已经完成。

---

## 2. Base-Type Inheritance Chain — Partial Reuse

DataGrid 继承自 `MultiSelector → Selector → ItemsControl → Control → FrameworkElement`。

当前策略：部分链接上游（带 `#if HAS_UNO` 守卫），部分本地 shim。

### 2.1 已链接的上游文件

| 文件 | 上游行数 | 链接方式 |
|------|---------|---------|
| `MultiSelector.cs` | 102 | 直接链接，`#if HAS_UNO` 守卫 few |
| `Selector.cs` | 3,052 | 链接，大量 `#if HAS_UNO` 守卫（property-engine、automation、layout-event） |
| `HeaderedItemsControl.cs` | 545 | 链接 |
| `HeaderedContentControl.cs` | ~517 | 链接 |

### 2.2 本地 shim 替代

| 文件 | 本地行数 | 上游行数 | 差异说明 |
|------|---------|---------|---------|
| `ItemsControl.cs` + `ItemsControlSpine.cs` + `ItemsControlItemInfo.cs` | 782 | **4,026** | 本地精心裁剪，仅提供 DataGrid 需要的 subset。缺少 `ItemContainerGenerator` 完整实现、`ItemTemplate`/`ItemTemplateSelector` WPF 通知、`ItemsPanel` 动态生成、`GroupStyle`、`AutomationPeer` 等。 |
| `ContentControl.cs` | 141 | 517 | 缺少 `ContentStringFormat`、`ContentTemplateSelector` 完整 WPF 语义 |
| `Control.cs` | 195 | 763 | 缺少 `MouseDoubleClick`/`PreviewMouseDoubleClick`、`IsTabStop`、`Focusable` WPF 行为 |
| `PanelShims.cs` | 89 | ~400 | 缺少 `UIElementCollection` WPF 语义、`InternalChildren` 排序、`IsItemsHost` 变更通知 |
| `VirtualizingPanelStubs.cs` | 412 (stubs) | 533 | 仅有 API 表面桩，无实际实现。DataGrid 虚拟化依赖此类型。 |

### 2.3 复用收益 vs 成本矩阵

| 上游文件 | 行数 | 成本 | 收益 | 推荐 |
|----------|------|------|------|------|
| `VirtualizingPanel.cs` | 533 | 🟡 **中** — 需 bridge `IItemContainerGenerator`、`IScrollInfo`、`ScrollContentPresenter`、`BringIndexIntoView` | ⭐ **高** — DataGrid 虚拟化的关键缺失 | **最高优先级** |
| `Panel.cs` | ~400 | 🟢 **低** — `InternalChildren` `IsItemsHost` 大部分对齐 | ⭐ **中** — 改善布局，减少 stubs | 推荐 |
| `ContentControl.cs` | 517 | 🟢 **低** — 主要是 `ContentStringFormat` DP、`ContentTemplateSelector` | ⭐ **中** — 消除 WPF 兼容 gap | 推荐 |
| `Control.cs` | 763 | 🟢 **低** — `Focusable`、`IsTabStop`、`MouseDoubleClick` | ⭐ **低-中** — 基础接入即可 | 推荐 |
| `ItemsControl.cs` | 4,026 | 🔴 **高** — 依赖 `IAddChild`、`IGeneratorHost`、`AutomationPeer`、`EventManager`、`KeyboardNavigation`、完整 `ItemCollection`、`GroupStyle` 等约 30+ 辅助类型 | ⭐⭐⭐ **最高** — 消除最大的 API gap，减少本地维护 | **分批进行** |

---

## 3. Non-DataGrid Upstream Files Already Linked

Aside from DataGrid and the Selector spine, the project already links a large
number of non-DataGrid files from `ext/`. Notable categories:

| Category | Examples | Purpose |
|---|---|---|
| **Documents/RichTextBox** | `FlowDocument.cs`, `Paragraph.cs`, `Run.cs`, `RichTextBox.cs`, `TextPointer.cs`, `TextRange.cs`, ~200 files | WPF 文档引擎 + RichTextBox 完整 port |
| **ToolBar** | `ToolBar.cs`, `ToolBarTray.cs`, `ToolBarPanel.cs`, `ToolBarOverflowPanel.cs` | ToolBar 控件族 |
| **Selection/Collection** | `SelectedItemCollection.cs`, `MultipleCopiesCollection.cs` | WPF 选择模型 |
| **Validation** | `ValidationRule.cs`, `ValidationResult.cs`, `ValidationStep.cs` | 数据验证 |
| **Data binding** | `SortDescription.cs`, `SortDescriptionCollection.cs`, `IEditableCollectionView.cs`, `IItemProperties.cs` | 排序/编辑接口 |
| **Templates** | `DataTemplateSelector.cs`, `StyleSelector.cs` | 模板选择器 |
| **Window** | `Window.cs` | WPF Window API |
| **Input** | `TraversalRequest.cs` | 焦点导航 |
| **KnownBoxes** | `KnownBoxes.cs` (from WindowsBase) | WPF 装箱常量 |

---

## 4. WPF Internal Types NOT Linked (Gaps)

Types that upstream `DataGrid.cs` references but are **not linked** from `ext/`:

### 4.1 `MS.Internal.Data` (45 files, none linked)

This is the **biggest gap**. Examples:

| File | Why it matters | Risk |
|---|---|---|
| `DataBindEngine.cs` | WPF 数据绑定引擎核心 | 低 — DataGrid 中 gated by `#if !HAS_UNO` |
| `BindingWorker.cs` | 绑定工作器 | 同上 |
| `CollectionViewGroupInternal.cs` | 分组视图 | 同上 — 但 Roma 的分组可能依赖 |
| `LiveShapingTree.cs` | 实时排序/分组/筛选 | 有本地 `LiveShaping` 替代 |

**策略**: 大部分被 `#if !HAS_UNO` 保护，无需立即处理。如果未来需要 WPF 兼容的
CollectionView 分组，再针对性链接。

### 4.2 `MS.Internal` helpers (10+ files, none linked)

| File | Status | Notes |
|---|---|---|
| `Helper.cs` | 有本地版本 | 提供 `DoubleUtil` 等 |
| `FrameworkObject.cs` | 未链接 | WPF 模板系统核心 |
| `WeakDictionary.cs`, `WeakHashtable.cs`, etc. | 未链接 | 弱引用集合 |
| `InheritedPropertyChangedEventArgs.cs` | 未链接 | 属性继承 |
| `PrePostDescendentsWalker.cs` | 未链接 | 可视化树遍历 |
| `TraceData.cs`, `TraceHwndHost.cs` | 未链接 | WPF 跟踪 — 低风险 |

### 4.3 Misc system types

| Type | Status | Notes |
|---|---|---|
| `System.Windows.Style` | 未链接 — 解析为 WinUI `Style` | WinUI Style 比 WPF 简单。若有复杂 `BasedOn` 链场景需桥接 |
| `DataTemplate` | 未链接 — 全局别名到 WinUI | `global using DataTemplate = Microsoft.UI.Xaml.DataTemplate;`。基础场景够用 |
| `FrameworkTemplate` | 未链接 | WPF 模板基类 — 当前不需要 |
| `ItemContainerGenerator` | 有本地实现 | 无虚拟化/回收 — 每次重建全部 |

---

## 5. `RealizedColumnsBlock.cs`

上游 `ext/wpf/.../Controls/RealizedColumnsBlock.cs` **已存在但未链接**。
本地有一个同名拷贝在 `System.Windows/Controls/RealizedColumnsBlock.cs`。

上游版本有 `#if !HAS_UNO` 守卫，直接链接上游版本并删除本地拷贝是安全的。
优先级：**低**（不影响功能，只是消除冗余）。

---

## 6. Recommended Phased Plan

### Phase A — Low effort, high confidence (1-2 days each)

| File | Action | Detail |
|---|---|---|
| `VirtualizingPanel.cs` | 链接上游 + `VirtualizingPanel.uno.cs` | 桥接 `IScrollInfo`、`IItemContainerGenerator`（已有本地实现）。DataGrid 虚拟化直接受益。 |
| `ContentControl.cs` | 替换本地 shim 为链接上游 + `ContentControl.uno.cs` | 本地 `ContentControl.cs` (141行) 改为最小的 uno 适配 partial，主体从 upstream 链接。 |
| `Control.cs` | 同上 | 本地 `Control.cs` (195行) 改为 uno partial，主体链接上游。 |
| `Panel.cs` | 链接上游 + `Panel.uno.cs` | 本地 `PanelShims.cs` (89行) 缩小为 uno partial。 |
| `RealizedColumnsBlock.cs` | 删除本地拷贝，链接上游 | 上游文件已有 HAS_UNO 守卫。 |

### Phase B — Big investment, big payoff (1-2 weeks)

| File | Action | Detail |
|---|---|---|
| `ItemsControl.cs` | **拆分成 10+ 个文件逐步链接** | 4,026 行的大文件，不能一次全链接。建议：<br>1. 先链接 DP 定义部分（`ItemTemplate`、`ItemTemplateSelector`、`ItemsPanel`、`ItemContainerStyle` 等）<br>2. 再链接 `ItemContainerGenerator` 交互部分<br>3. 最后链接 `GroupStyle`、`SortDescription` 等分组/排序支持 |

### Phase C — Nice to have

| File | Action | Detail |
|---|---|---|
| `DataGridHyperlinkColumn.cs` | **不链接**，保留本地 shim | 上游 309 行，本地 113 行。链接需要 fork-guard 替换全部 WPF 类型，不如本地实现简洁。 |
| `MS.Internal.Data/*.cs` | **暂不处理** | 除非有明确场景需要 WPF CollectionView 分组/筛选语义。 |

---

## 7. Technical Approach

### 7.1 File naming pattern (existing convention)

```
ext/DataGrid.cs                   →  csproj Link="System.Windows.Controls\DataGrid_upstream.cs"
src/System.Windows/Controls/DataGrid.cs  →  partial class (uno-specific)
```

For new base-type links:

```
ext/Controls/VirtualizingPanel.cs  →  csproj Link="System.Windows.Controls\VirtualizingPanel_upstream.cs"
src/.../VirtualizingPanel.uno.cs   →  partial class (uno-specific overrides)
```

### 7.2 `#if HAS_UNO` fork-guard pattern

Upstream files get `#if !HAS_UNO` / `#if HAS_UNO` blocks patched in-place (already
done for DataGrid, Selector, etc.). The `*.uno.cs` partials compile only under
`HAS_UNO` (which is always defined in this project).

### 7.3 Stub/bridge contract

Each newly linked upstream file may require 1-3 new small shim types in
`MS.Internal.*` or `System.Windows.*` namespaces. The current approach is to
add them inline in the `*.uno.cs` partial or as dedicated `*Shims.cs` files.

---

## 8. Summary Snapshot

```
                              linked   local    stubs    not worth
                              ──────   ─────    ─────    ─────────
DataGrid*.cs (52 files)        51       1        0        0
Selector + MultiSelector        2       0        0        0
HeaderedItemsControl            1       0        0        0
HeaderedContentControl          1       0        0        0
ItemsControl                    0       3        0        0
ContentControl                  0       1        0        0
Control                         0       1        0        0
Panel                           0       1        0        0
VirtualizingPanel               0       0        1        0
MS.Internal.Data               45       0        0       45*
MS.Internal helpers            12       1        0       11*
                              ────    ───       ──       ──
Total                          60       7        1       56

* protected by #if !HAS_UNO — no action needed unless new feature demands it.
```
