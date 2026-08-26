using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

const string BIN = @"C:\Program Files\Dassault Systemes\B19\win_b64\code\bin";

string[] dllNames = {
    "INFITF.dll",
    "MECMOD.dll",
    "PARTITF.dll",
    "NAVIGATORITF.dll",
    "KWEITF.dll",
    "PSITF.dll",
    "KnowledgewareTypeLib.dll",
};

// ── Chargement des assemblies ───────────────────────────────────────────────
var assemblies = new List<Assembly>();
Console.WriteLine("=== CHARGEMENT DES DLL ===");
foreach (var dll in dllNames)
{
    string path = Path.Combine(BIN, dll);
    try
    {
        var asm = Assembly.LoadFrom(path);
        assemblies.Add(asm);
        Console.WriteLine($"  OK  {dll}  ({asm.GetTypes().Length} types)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERR {dll}: {ex.Message}");
    }
}

// ── Helpers ──────────────────────────────────────────────────────────────────
static string FormatType(Type t) =>
    t.IsGenericType
        ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>"
        : t.Name;

static void PrintMembers(Type t, string label)
{
    Console.WriteLine();
    Console.WriteLine($"┌─── {label}");
    Console.WriteLine($"│    {t.FullName}  [{t.Assembly.GetName().Name}]");
    Console.WriteLine($"│    Interfaces: {string.Join(", ", t.GetInterfaces().Select(i => i.Name))}");
    Console.WriteLine("│");

    var members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                   .OrderBy(m => m.MemberType)
                   .ThenBy(m => m.Name);

    foreach (var m in members)
    {
        switch (m)
        {
            case PropertyInfo pi:
                var getter = pi.CanRead ? "get;" : "";
                var setter = pi.CanWrite ? "set;" : "";
                Console.WriteLine($"│  PROP   {FormatType(pi.PropertyType),-30} {pi.Name,-35} {{ {getter} {setter} }}");
                break;

            case MethodInfo mi when !mi.IsSpecialName:
                var parms = string.Join(", ", mi.GetParameters()
                    .Select(p => $"{FormatType(p.ParameterType)} {p.Name}"));
                Console.WriteLine($"│  METH   {FormatType(mi.ReturnType),-30} {mi.Name}({parms})");
                break;
        }
    }
    Console.WriteLine("└" + new string('─', 79));
}

static void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine(new string('═', 80));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('═', 80));
}

static List<Type> FindByPattern(List<Assembly> asms, string pattern)
{
    var result = new List<Type>();
    foreach (var asm in asms)
    {
        try
        {
            result.AddRange(asm.GetTypes()
                .Where(t => t.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
        }
        catch { }
    }
    return result;
}

static Type? FindExact(List<Assembly> asms, string name)
{
    foreach (var asm in asms)
    {
        try
        {
            var t = asm.GetTypes().FirstOrDefault(x => x.Name == name);
            if (t != null) return t;
        }
        catch { }
    }
    return null;
}

// ════════════════════════════════════════════════════════════════════════════
// 1. HybridBodies / HybridBody / HybridShapes
// ════════════════════════════════════════════════════════════════════════════
Section("1. HybridBodies / HybridBody / HybridShapes");

foreach (var name in new[] { "HybridBodies", "HybridBody", "HybridShapes" })
{
    var t = FindExact(assemblies, name);
    if (t != null) PrintMembers(t, name);
    else Console.WriteLine($"  [NON TROUVÉ] {name}");
}

// ════════════════════════════════════════════════════════════════════════════
// 2. Types HybridShapePlane* (tous)
// ════════════════════════════════════════════════════════════════════════════
Section("2. Types dont le nom contient 'HybridShapePlane'");

var planTypes = FindByPattern(assemblies, "HybridShapePlane");
if (planTypes.Count == 0)
    Console.WriteLine("  Aucun type trouvé avec 'HybridShapePlane'.");
foreach (var t in planTypes)
    PrintMembers(t, $"HybridShapePlane* : {t.Name}");

// ════════════════════════════════════════════════════════════════════════════
// 3. Types contenant 'Mirror'
// ════════════════════════════════════════════════════════════════════════════
Section("3. Types dont le nom contient 'Mirror'");

var mirrorTypes = FindByPattern(assemblies, "Mirror");
if (mirrorTypes.Count == 0)
    Console.WriteLine("  Aucun type trouvé avec 'Mirror'.");
foreach (var t in mirrorTypes)
    PrintMembers(t, $"Mirror : {t.Name}");

// ════════════════════════════════════════════════════════════════════════════
// 4. Pad et Pocket — Direction, Reversed, Orientation
// ════════════════════════════════════════════════════════════════════════════
Section("4. Pad — membres complets");
{
    var t = FindExact(assemblies, "Pad");
    if (t != null) PrintMembers(t, "Pad");
    else Console.WriteLine("  [NON TROUVÉ] Pad");
}

Section("4b. Pocket — membres complets");
{
    var t = FindExact(assemblies, "Pocket");
    if (t != null) PrintMembers(t, "Pocket");
    else Console.WriteLine("  [NON TROUVÉ] Pocket");
}

// Types Direction / DirectionFeature
Section("4c. Types contenant 'Direction'");
var dirTypes = FindByPattern(assemblies, "Direction");
foreach (var t in dirTypes)
    PrintMembers(t, $"Direction* : {t.Name}");

// ════════════════════════════════════════════════════════════════════════════
// 5. Éléments géométriques de sketch : Circle2D, Line2D, Point2D + 'Construction'
// ════════════════════════════════════════════════════════════════════════════
Section("5. Géométrie sketch : Circle2D, Line2D, Point2D, Ellipse2D, Spline2D, ControlPoint2D");

foreach (var name in new[] { "Circle2D", "Line2D", "Point2D", "Ellipse2D", "Spline2D", "ControlPoint2D" })
{
    var t = FindExact(assemblies, name);
    if (t != null) PrintMembers(t, name);
    else Console.WriteLine($"  [NON TROUVÉ] {name}");
}

Section("5b. Types contenant 'Geometric2D' (base des éléments sketch)");
var geo2dTypes = FindByPattern(assemblies, "Geometric2D");
foreach (var t in geo2dTypes)
    PrintMembers(t, $"Geometric2D* : {t.Name}");

Section("5c. Propriété 'Construction' — quels types l'exposent ?");
foreach (var asm in assemblies)
{
    try
    {
        var types = asm.GetTypes().Where(t =>
            t.GetProperty("Construction",
                BindingFlags.Public | BindingFlags.Instance) != null).ToList();
        if (types.Count > 0)
        {
            Console.WriteLine($"  [{asm.GetName().Name}]");
            foreach (var t in types)
                Console.WriteLine($"    {t.FullName}  → Construction : {t.GetProperty("Construction")!.PropertyType.Name}");
        }
    }
    catch { }
}

// ════════════════════════════════════════════════════════════════════════════
// 6. Résumé : tous les types de PARTITF (listing)
// ════════════════════════════════════════════════════════════════════════════
Section("6. Listing complet des types exposés par PARTITF");
var partitf = assemblies.FirstOrDefault(a => a.GetName().Name == "PARTITF");
if (partitf != null)
{
    foreach (var t in partitf.GetTypes().OrderBy(t => t.Name))
        Console.WriteLine($"  {t.Name,-50}  ({t.Namespace})");
}
else
{
    Console.WriteLine("  PARTITF non chargé.");
}

Section("6b. Listing complet des types exposés par MECMOD");
var mecmod = assemblies.FirstOrDefault(a => a.GetName().Name == "MECMOD");
if (mecmod != null)
{
    foreach (var t in mecmod.GetTypes().OrderBy(t => t.Name))
        Console.WriteLine($"  {t.Name,-50}  ({t.Namespace})");
}
else
{
    Console.WriteLine("  MECMOD non chargé.");
}

Console.WriteLine("\nInspection terminée.");
