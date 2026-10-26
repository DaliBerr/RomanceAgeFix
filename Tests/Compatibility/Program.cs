using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
string game = args.Length > 1 ? args[1] : @"C:\Program Files (x86)\Steam\steamapps\common\RimWorld";
int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
XElement about = XDocument.Load(Path.Combine(root, "About/About.xml")).Root;
XElement load = XDocument.Load(Path.Combine(root, "LoadFolders.xml")).Root;
foreach (string version in new[] { "1.5", "1.6", "1.7" })
{
    string managed = version == "1.7" ? game : Path.Combine(root, version, "ExternalMods");
    using AssemblyData core = new(Path.Combine(managed, "Assembly-CSharp.dll"));
    using AssemblyData mod = new(Path.Combine(root, version, "Assemblies/RomanceAgeFix.dll"));
    Check(core.Version.Minor == int.Parse(version.Split('.')[1]), "Wrong original game reference");
    Check(core.Method("RimWorld.Pawn_RelationsTracker", "SecondaryRomanceChanceFactor", false, "System.Single",
        new[] { "Verse.Pawn" }, new[] { "otherPawn" }), "Romance-factor Harmony target changed");
    Check(core.Field("RimWorld.Pawn_RelationsTracker", "pawn", "Verse.Pawn"), "Injected pawn field changed");
    Check(core.Method("RimWorld.LovePartnerRelationUtility", "GetLovinMtbHours", true, "System.Single",
        new[] { "Verse.Pawn", "Verse.Pawn" }, new[] { "pawn", "partner" }), "Lovin-interval Harmony target changed");
    Check(core.Method("RimWorld.RitualOutcomeEffectWorker_FromQuality", "GetQuality", false, "System.Single",
        new[] { "RimWorld.LordJob_Ritual", "System.Single" }, new[] { "jobRitual", "progress" }), "Ritual-quality Harmony target changed");
    Check(core.Method("RimWorld.ThoughtWorker_AgeReversalDemanded", "ShouldHaveThought", false, "RimWorld.ThoughtState",
        new[] { "Verse.Pawn" }, new[] { "p" }, requireNonPublic: true), "Age-reversal Harmony target changed");
    Check(mod.Method("RomanceAgeFix.HarmonyPatches.Prefix_MinAgeForRomance", "SecondaryRomanceChanceFactor_Postfix", true, "System.Void",
        new[] { "Verse.Pawn", "Verse.Pawn", "System.Single&" }, new[] { "___pawn", "otherPawn", "__result" }), "Romance postfix signature changed");
    var references = mod.References();
    Check(references["0Harmony"].Major == (version == "1.7" ? 3 : 2), "Wrong Harmony major version");
    Check(references.ContainsKey(version == "1.7" ? "UnityStubs" : "UnityEngine.CoreModule"), "Wrong engine reference");
    Check(about.Element("supportedVersions").Elements("li").Any(item => item.Value == version), "Supported version missing");
    string[] folders = load.Element("v" + version).Elements("li").Select(item => item.Value).ToArray();
    string[] dlls = folders.SelectMany(folder => {
        string path = Path.Combine(root, folder == "/" ? "" : folder, "Assemblies");
        return Directory.Exists(path) ? Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories) : Array.Empty<string>();
    }).ToArray();
    Check(dlls.Length == 1 && Path.GetFileName(dlls[0]) == "RomanceAgeFix.dll", "Duplicate or foreign runtime DLL");
    Console.WriteLine($"PASS: {version}; four real Harmony targets, pawn injection, matching engine/Harmony and loading folders.");
}
Console.WriteLine($"PASS: {checks} real assembly signature, reference and loading checks.");

sealed class AssemblyData : IDisposable
{
    static readonly SignatureNames names = new();
    readonly FileStream stream;
    readonly PEReader pe;
    readonly MetadataReader reader;
    readonly Dictionary<string, TypeDefinition> types;
    public Version Version => reader.GetAssemblyDefinition().Version;
    public AssemblyData(string path)
    {
        stream = File.OpenRead(path); pe = new PEReader(stream); reader = pe.GetMetadataReader();
        types = reader.TypeDefinitions.Select(reader.GetTypeDefinition).Where(type => !type.IsNested)
            .ToDictionary(type => reader.GetString(type.Namespace) + "." + reader.GetString(type.Name));
    }
    public bool Field(string type, string field, string fieldType) => types.TryGetValue(type, out var definition)
        && definition.GetFields().Select(reader.GetFieldDefinition).Any(member => reader.GetString(member.Name) == field
            && member.DecodeSignature(names, null) == fieldType);
    public bool Method(string type, string method, bool isStatic, string returnType, string[] parameterTypes, string[] parameterNames,
        bool requireNonPublic = false)
    {
        if (!types.TryGetValue(type, out var definition)) return false;
        return definition.GetMethods().Select(reader.GetMethodDefinition).Any(member => {
            if (reader.GetString(member.Name) != method || member.Attributes.HasFlag(MethodAttributes.Static) != isStatic
                || requireNonPublic && member.Attributes.HasFlag(MethodAttributes.Public)) return false;
            MethodSignature<string> signature = member.DecodeSignature(names, null);
            return signature.ReturnType == returnType && signature.ParameterTypes.SequenceEqual(parameterTypes)
                && member.GetParameters().Select(reader.GetParameter).Where(parameter => parameter.SequenceNumber > 0)
                    .Select(parameter => reader.GetString(parameter.Name)).SequenceEqual(parameterNames);
        });
    }
    public Dictionary<string, Version> References() => reader.AssemblyReferences.Select(reader.GetAssemblyReference)
        .ToDictionary(reference => reader.GetString(reference.Name), reference => reference.Version);
    public void Dispose() { pe.Dispose(); stream.Dispose(); }
}
sealed class SignatureNames : ISignatureTypeProvider<string, object>
{
    public string GetArrayType(string element, ArrayShape shape) => element + "[" + new string(',', shape.Rank - 1) + "]";
    public string GetByReferenceType(string element) => element + "&";
    public string GetFunctionPointerType(MethodSignature<string> signature) => "methodptr";
    public string GetGenericInstantiation(string type, ImmutableArray<string> arguments) => type + "<" + string.Join(",", arguments) + ">";
    public string GetGenericMethodParameter(object context, int index) => "!!" + index;
    public string GetGenericTypeParameter(object context, int index) => "!" + index;
    public string GetModifiedType(string modifier, string type, bool required) => type;
    public string GetPinnedType(string element) => element;
    public string GetPointerType(string element) => element + "*";
    public string GetPrimitiveType(PrimitiveTypeCode code) => "System." + code;
    public string GetSZArrayType(string element) => element + "[]";
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte kind)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte kind)
    {
        TypeReference type = reader.GetTypeReference(handle);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
    public string GetTypeFromSpecification(MetadataReader reader, object context, TypeSpecificationHandle handle, byte kind)
        => reader.GetTypeSpecification(handle).DecodeSignature(this, context);
}
