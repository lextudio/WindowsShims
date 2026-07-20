using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var repoRoot = FindRepoRoot();
string wpfRoot = Path.Combine(repoRoot, "librewpf", "src", "Microsoft.DotNet.Wpf", "src", "PresentationFramework", "System", "Windows");
string shimsDir = Path.Combine(repoRoot, "WindowsShims", "src", "LeXtudio.Windows");
string extDir = Path.Combine(repoRoot, "WindowsShims", "ext", "wpf", "src", "Microsoft.DotNet.Wpf", "src", "PresentationFramework", "System", "Windows");

string[] areas = ["Controls", "Controls/Primitives", "Automation/Peers"];

var wpfFiles = new List<string>();
var shimsFiles = new List<string>();
var extFiles = new List<string>();

foreach (var area in areas)
{
    wpfFiles.AddRange(Directory.EnumerateFiles(
        Path.Combine(wpfRoot, area), "DataGrid*.cs", SearchOption.TopDirectoryOnly));
    shimsFiles.AddRange(Directory.EnumerateFiles(
        Path.Combine(shimsDir, "System.Windows", area), "DataGrid*.cs", SearchOption.TopDirectoryOnly));
    if (Directory.Exists(Path.Combine(extDir, area)))
        extFiles.AddRange(Directory.EnumerateFiles(
            Path.Combine(extDir, area), "DataGrid*.cs", SearchOption.TopDirectoryOnly));
}

Console.WriteLine("=== DataGrid API Parity Check (Roslyn) ===");
Console.WriteLine();

var wpfSymbols = CollectSymbols(wpfFiles, "WPF (librewpf)");
Console.WriteLine();
var extSymbols = CollectSymbols(extFiles, "ext/wpf (linked)");
Console.WriteLine();

var shimsSymbols = CollectSymbols(shimsFiles, "WindowsShims (local)");

Console.WriteLine();

// Merge ext symbols into shims (they compile together)
foreach (var (typeName, members) in extSymbols.Types)
{
    if (shimsSymbols.Types.ContainsKey(typeName))
    {
        foreach (var member in members)
            shimsSymbols.Types[typeName].Add(member);
    }
    else
    {
        shimsSymbols.Types[typeName] = members;
    }
}

// Compare
var wpfTypeNames = wpfSymbols.Types.Keys.OrderBy(x => x).ToArray();
var shimsTypeNames = shimsSymbols.Types.Keys.OrderBy(x => x).ToArray();

var wpfTypeSet = wpfSymbols.Types.Keys.ToFrozenSet();
var shimsTypeSet = shimsSymbols.Types.Keys.ToFrozenSet();

int matchedTypes = 0, missingTypes = 0;
foreach (var tn in wpfTypeNames)
{
    if (shimsTypeSet.Contains(tn)) matchedTypes++;
    else missingTypes++;
}

int extraTypes = shimsTypeNames.Count(t => !wpfTypeSet.Contains(t));

Console.WriteLine("Type parity:");
Console.WriteLine($"  WPF reference types:   {wpfTypeNames.Length}");
Console.WriteLine($"  WindowsShims types:    {shimsTypeNames.Length}");
Console.WriteLine($"  Matched:               {matchedTypes}");
Console.WriteLine($"  Missing:               {missingTypes}");
Console.WriteLine($"  Extra (shim-only):     {extraTypes}");
Console.WriteLine();

if (missingTypes > 0)
{
    Console.WriteLine("Missing types:");
    foreach (var tn in wpfTypeNames.Where(t => !shimsTypeSet.Contains(t)))
        Console.WriteLine($"  ✗ {tn}");
    Console.WriteLine();
}

if (extraTypes > 0)
{
    Console.WriteLine("Extra types (shim-only):");
    foreach (var tn in shimsTypeNames.Where(t => !wpfTypeSet.Contains(t)))
        Console.WriteLine($"  + {tn}");
    Console.WriteLine();
}

// Member comparison
int totalWpfMembers = 0, totalMatchedMembers = 0, totalMissingMembers = 0, totalExtraMembers = 0;
var missingMembers = new List<string>();
var extraMembers = new List<string>();

foreach (var (typeName, wpfMembers) in wpfSymbols.Types)
{
    shimsSymbols.Types.TryGetValue(typeName, out var shimsMembers);
    shimsMembers ??= [];

    foreach (var m in wpfMembers)
    {
        totalWpfMembers++;
        if (shimsMembers.Contains(m))
            totalMatchedMembers++;
        else
        {
            totalMissingMembers++;
            missingMembers.Add($"{typeName}.{m}");
        }
    }
}

foreach (var (typeName, shimsMembers) in shimsSymbols.Types)
{
    if (!wpfSymbols.Types.TryGetValue(typeName, out var wpfMembers))
    {
        foreach (var m in shimsMembers)
        {
            totalExtraMembers++;
            extraMembers.Add($"{typeName}.{m}");
        }
        continue;
    }
    foreach (var m in shimsMembers)
    {
        if (!wpfMembers.Contains(m))
        {
            totalExtraMembers++;
            extraMembers.Add($"{typeName}.{m}");
        }
    }
}

Console.WriteLine("Member parity:");
Console.WriteLine($"  WPF members:          {totalWpfMembers}");
Console.WriteLine($"  WindowsShims members: {totalWpfMembers - totalMissingMembers + totalExtraMembers}");
Console.WriteLine($"  Matched:              {totalMatchedMembers}");
Console.WriteLine($"  Missing:              {totalMissingMembers}");
Console.WriteLine($"  Extra (shim-only):    {totalExtraMembers}");
double pct = totalWpfMembers > 0 ? Math.Round((double)totalMatchedMembers / totalWpfMembers * 100, 1) : 0;
Console.WriteLine($"  Parity:               {totalMatchedMembers}/{totalWpfMembers} ({pct}%)");
Console.WriteLine();

if (missingMembers.Count > 0)
{
    Console.WriteLine("Missing members:");
    foreach (var m in missingMembers.OrderBy(x => x))
        Console.WriteLine($"  ✗ {m}");
    Console.WriteLine();
}

if (extraMembers.Count > 0)
{
    Console.WriteLine("Extra members:");
    foreach (var m in extraMembers.OrderBy(x => x))
        Console.WriteLine($"  + {m}");
}

// Priority gap summary
Console.WriteLine();
Console.WriteLine("=== Gap Summary ===");
Console.WriteLine($"  Types:           {matchedTypes} matched, {missingTypes} missing, {extraTypes} extra");
Console.WriteLine($"  Members:         {totalMatchedMembers} matched, {totalMissingMembers} missing, {totalExtraMembers} extra");
Console.WriteLine($"  Member parity:   {pct}%");

static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    for (var d = new DirectoryInfo(dir); d is not null; d = d.Parent)
        if (File.Exists(Path.Combine(d.FullName, "WindowsShims.slnx")) ||
            Directory.Exists(Path.Combine(d.FullName, "WindowsShims")))
            return d.FullName;
    throw new InvalidOperationException("Cannot find repo root.");
}

static SourceSymbols CollectSymbols(List<string> files, string label)
{
    var types = new SortedDictionary<string, HashSet<string>>();
    var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in files)
    {
        if (!processedFiles.Add(Path.GetFullPath(file))) continue;
        if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
            file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            continue;

        var text = File.ReadAllText(file);
        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(preprocessorSymbols: ["HAS_UNO"]));
        var walker = new PublicApiWalker(types);
        walker.Visit(tree.GetRoot());
    }

    Console.WriteLine($"Scanned {files.Count,2} files in {label,-20}: {types.Count,2} types, {types.Sum(kv => kv.Value.Count),3} members");
    return new SourceSymbols(types);
}

internal sealed class PublicApiWalker(SortedDictionary<string, HashSet<string>> types) : CSharpSyntaxWalker(SyntaxWalkerDepth.Node)
{
    private readonly Stack<string> _typeStack = new();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node) =>
        VisitType(node, node.Identifier.ValueText, static (w, n) => w.VisitClassDeclarationCore(n));

    public override void VisitStructDeclaration(StructDeclarationSyntax node) =>
        VisitType(node, node.Identifier.ValueText, static (w, n) => w.VisitStructDeclarationCore(n));

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) =>
        VisitType(node, node.Identifier.ValueText, static (w, n) => w.VisitInterfaceDeclarationCore(n));

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node) =>
        VisitType(node, node.Identifier.ValueText, static (w, n) => w.VisitRecordDeclarationCore(n));

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node) =>
        VisitType(node, node.Identifier.ValueText, static (w, n) => w.VisitEnumDeclarationCore(n));

    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        if (IsPublic(node.Modifiers))
        {
            var fullName = GetFullName(node.Identifier.ValueText);
            GetOrCreate(fullName);
        }
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (IsPublicMember(node.Modifiers, node.Parent) && TryGetCurrentType(out var type))
        {
            var paramList = string.Join(", ", node.ParameterList.Parameters.Select(p => p.Type?.ToString() ?? "?"));
            var name = $"{node.Identifier.ValueText}({paramList})";
            GetOrCreate(type).Add(name);
        }
        base.VisitMethodDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (IsPublicMember(node.Modifiers, node.Parent) && TryGetCurrentType(out var type))
            GetOrCreate(type).Add(node.Identifier.ValueText);
        base.VisitPropertyDeclaration(node);
    }

    public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        if (IsPublicMember(node.Modifiers, node.Parent) && TryGetCurrentType(out var type))
            GetOrCreate(type).Add("this[]");
        base.VisitIndexerDeclaration(node);
    }

    public override void VisitEventDeclaration(EventDeclarationSyntax node)
    {
        if (IsPublicMember(node.Modifiers, node.Parent) && TryGetCurrentType(out var type))
            GetOrCreate(type).Add(node.Identifier.ValueText);
        base.VisitEventDeclaration(node);
    }

    public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        if (IsPublic(node.Modifiers) && TryGetCurrentType(out var type))
            foreach (var v in node.Declaration.Variables)
                GetOrCreate(type).Add(v.Identifier.ValueText);
        base.VisitEventFieldDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (IsPublic(node.Modifiers) && TryGetCurrentType(out var type))
            foreach (var v in node.Declaration.Variables)
                GetOrCreate(type).Add(v.Identifier.ValueText);
        base.VisitFieldDeclaration(node);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (IsPublic(node.Modifiers) && TryGetCurrentType(out var type))
            GetOrCreate(type).Add(".ctor");
        base.VisitConstructorDeclaration(node);
    }

    private void VisitType<TNode>(TNode node, string typeName, Action<PublicApiWalker, TNode> baseVisit)
        where TNode : MemberDeclarationSyntax
    {
        if (!IsPublic(node)) return;
        var fullName = GetFullName(typeName);
        GetOrCreate(fullName);
        _typeStack.Push(fullName);
        try { baseVisit(this, node); }
        finally { _typeStack.Pop(); }
    }

    private void VisitClassDeclarationCore(ClassDeclarationSyntax node) => base.VisitClassDeclaration(node);
    private void VisitStructDeclarationCore(StructDeclarationSyntax node) => base.VisitStructDeclaration(node);
    private void VisitInterfaceDeclarationCore(InterfaceDeclarationSyntax node) => base.VisitInterfaceDeclaration(node);
    private void VisitRecordDeclarationCore(RecordDeclarationSyntax node) => base.VisitRecordDeclaration(node);
    private void VisitEnumDeclarationCore(EnumDeclarationSyntax node) => base.VisitEnumDeclaration(node);

    private string GetFullName(string localName)
    {
        if (_typeStack.Count > 0)
            return $"{_typeStack.Peek()}.{localName}";
        return GetCurrentNamespace() is { } ns ? $"{ns}.{localName}" : localName;
    }

    private string? GetCurrentNamespace()
    {
        // Walk up to find enclosing namespace
        return null; // handled by type stack composition
    }

    private bool TryGetCurrentType(out string type)
    {
        if (_typeStack.Count > 0) { type = _typeStack.Peek(); return true; }
        type = ""; return false;
    }

    private HashSet<string> GetOrCreate(string typeName)
    {
        if (!types.TryGetValue(typeName, out var set))
            types[typeName] = set = [];
        return set;
    }

    private static bool IsPublicMember(SyntaxTokenList modifiers, SyntaxNode? parent) =>
        IsPublic(modifiers) || parent is InterfaceDeclarationSyntax;

    private static bool IsPublic(MemberDeclarationSyntax node) =>
        IsPublic(node.Modifiers) || node.Parent is InterfaceDeclarationSyntax;

    private static bool IsPublic(SyntaxTokenList modifiers) =>
        modifiers.Any(SyntaxKind.PublicKeyword);
}

internal sealed record SourceSymbols(SortedDictionary<string, HashSet<string>> Types);
