using System.Text.Json.Nodes;

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: AddRuntimeToDepsJson <deps.jsonPath> <sdkId> <assemblyId> <assemblyPath>");
    return 1;
}

var depsJsonPath = args[0];
var sdkId = args[1];
var assemblyId = args[2];
var assemblyPath = args[3];

var dependencies = JsonNode.Parse(File.ReadAllText(depsJsonPath));
if (dependencies is null)
{
    Console.Error.WriteLine($"Failed to parse {depsJsonPath}");
    return 1;
}

var targets = dependencies["targets"];
if (targets is null)
{
    Console.Error.WriteLine($"Failed to find 'targets' in {depsJsonPath}");
    return 1;
}

var sdk = targets[sdkId];
if (sdk is null)
{
    Console.Error.WriteLine($"Failed to find '{sdkId}' in {depsJsonPath}");
    return 1;
}

var assembly = sdk[assemblyId];
if (assembly is null)
{
    Console.Error.WriteLine($"Failed to find '{assemblyId}' in {depsJsonPath}");
    return 1;
}

var runtime = assembly["runtime"];
if (runtime is not null)
    return 0;

assembly["runtime"] = JsonNode.Parse($$"""
                                       {
                                         "{{assemblyPath}}": {}
                                       }
                                       """);

File.WriteAllText(depsJsonPath, dependencies.ToString());
return 0;