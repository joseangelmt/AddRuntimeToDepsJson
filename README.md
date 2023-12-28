# AddRuntimeToDepsJson

If your native application provides a mixed assembly (C++/CLI) so that your users can interact with it via .NET, you can provide such a mixed assembly via a NuGet package.

#### Example of the _files_ section of the `.nuspec` file of the NuGet package publishing the assemblies in the `lib` folder.
```xml
<files>
  <file src="..\Digi21.DigiNG\bin\Release\net8.0\Digi21.DigiNG.dll" target="lib\net8.0\Digi21.DigiNG.dll" />
</files>
```

In the example above, since we provided the assemblies in the `lib` folder, when using this NuGet package, the `.deps.json` files of the dependent applications will provide information so that the .NET runtime knows the path to load the assembly....

```json
{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v8.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v8.0": {
      "cargabind/1.0.0": {
        "dependencies": {
          "Digi21.DigiNG": "24.0.0",
        },
        "runtime": {
          "cargabind.dll": {}
        }
      },
      "Digi21.DigiNG/24.0.0": {
        "runtime": {
          "lib/net8.0/Digi21.DigiNG.dll": {
            "assemblyVersion": "24.0.0.0",
            "fileVersion": "24.0.0.0"
          }
        }
      },
```

In this case, when your client runs its program, the loader will load your mixed assembly from the path: `%userprofile%\.nuget\packages\digi21.diging\24.0.0\lib\net8.0\Digi21.DigiNG.dll`.

If you publish a new version of your application, and in this new version some internal modification has been made to the mixed assembly, you will have to re-publish a new NuGet package, even if the public surface of the assembly has not changed, and you will have to apply publisher policies so that programs deployed by your users for previous versions of the NuGet package continue to run, or you will have to force your users to re-publish a new NuGet package, even if the public surface of the assembly has not changed.

To solve this, instead of publishing the mixed assemblies (which are runtime assemblies) in the NuGet package, you can publish [Reference Assemblies](https://learn.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies) (which you will have to implement in C# for example because you cannot create reference assemblies in C++/CLI projects).

#### Example of the _files_ section of the `.nuspec` file of the NuGet package publishing the assemblies in the `ref` folder.
```xml
<files>
  <file src="..\Digi21.DigiNG\obj\Release\net8.0\ref\Digi21.DigiNG.dll" target="ref\net8.0\Digi21.DigiNG.dll" />
</files>
```

NuGet packages with _Reference Assemblies_ are intended for cases where the NuGet package provides multiple runtime assemblies in the same package (one for each operating system or framework version) as explained in [Multi-targeting for NuGet Packages](https://learn.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks), but because __we are not including runtimes__ in our NuGet package, when you build an application using our NuGet package, the application's `.deps.json` file will not provide the name of the DLL to load, as we can see below:

```json
{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v8.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v8.0": {
      "cargabind/1.0.0": {
        "dependencies": {
          "Digi21.DigiNG": "24.0.0",
        },
        "runtime": {
          "cargabind.dll": {}
        }
      },
      "Digi21.DigiNG/24.0.0": {},
```

As you can see in the `.deps.json` file above, there is no reference to _Digi21.DigiNG.dll_, so the loader will not know which DLL the _Digi21.DigiNG/24.0.0_ assembly implements.

To solve this problem, we would have to manually add the name of the DLL that implements the assembly to the _runtime_ attribute of our assembly:

```json
{
  "runtimeTarget": {
    "name": ".NETCoreApp,Version=v8.0",
    "signature": ""
  },
  "compilationOptions": {},
  "targets": {
    ".NETCoreApp,Version=v8.0": {
      "cargabind/1.0.0": {
        "dependencies": {
          "Digi21.DigiNG": "24.0.0",
          "Digi21.DigiNG.IO.BinDouble": "24.0.0",
          "Newtonsoft.Json": "13.0.3"
        },
        "runtime": {
          "cargabind.dll": {}
        }
      },
      "Digi21.DigiNG/24.0.0": {
        "runtime": {
          "Digi21.DigiNG.dll": {}
        }
```

But we don't want the user to have to do this manually every time they build, so we can have our NuGet package provide a __Post Build Event__ that runs a tool that parses the app's `.deps.json` file and adds this entry in case you don't have it, and this is the purpose of this repository.

The __AddRutimeToDepsJson__ application is a console application that receives the following parameters:

* Path of the .deps.json file to modify
* SDK name
* Assembly name
* Path to the .DLL file that implements the assembly

For our example the parameters would be `AddRutimeToDepsJson.exe "$(OutDir)$(TargetName).deps.json" ".NETCoreApp,Version=v8.0" "Digi21.DigiNG/24.0.0" Digi21.DigiNG.dll`

To add a _Post Build Event_ to our NuGet package we can follow the instructions explained in this [Stack Overflow answer](https://stackoverflow.com/a/37963015/583336)

We create the file `Digi21.DigiNG.targets`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <BuildDependsOn>
			$(BuildDependsOn);
			Digi21DigiNGCustomTarget
		</BuildDependsOn>
	</PropertyGroup>

	<Target Name="Digi21DigiNGCustomTarget">
		<Exec Command="$(MSBuildThisFileDirectory)AddRuntimeToDepsJson.exe &quot;$(OutDir)$(TargetName).deps.json&quot; &quot;.NETCoreApp,Version=v8.0&quot; &quot;Digi21.DigiNG/24.0.0&quot; Digi21.DigiNG.dll" />
	</Target>
</Project>
```

...and finally, we add both the `Digi21.DigiNG.targets` file and the `AddRuntimeToDepsJson` program to the `build` folder of the NuGet package:

```xml
	<files>
		<file src="..\Digi21.DigiNG\obj\Release\net8.0\ref\Digi21.DigiNG.dll" target="ref\net8.0\Digi21.DigiNG.dll" />
		<file src="Digi21.DigiNG.targets" target="build\Digi21.DigiNG.targets" />
		<file src="C:\Users\josea\source\repos\AddRuntimeToDepsJson\AddRuntimeToDepsJson\bin\Release\net8.0\AddRuntimeToDepsJson.deps.json" target="build\AddRuntimeToDepsJson.deps.json" />
		<file src="C:\Users\josea\source\repos\AddRuntimeToDepsJson\AddRuntimeToDepsJson\bin\Release\net8.0\AddRuntimeToDepsJson.dll" target="build\AddRuntimeToDepsJson.dll" />
		<file src="C:\Users\josea\source\repos\AddRuntimeToDepsJson\AddRuntimeToDepsJson\bin\Release\net8.0\AddRuntimeToDepsJson.exe" target="build\AddRuntimeToDepsJson.exe" />
		<file src="C:\Users\josea\source\repos\AddRuntimeToDepsJson\AddRuntimeToDepsJson\bin\Release\net8.0\AddRuntimeToDepsJson.runtimeconfig.json" target="build\AddRuntimeToDepsJson.runtimeconfig.json" />
	</files>
```

And that's it: every time the application is built with our NuGet package, the _runtime_ entry will automatically be added to the `.deps.json` file of the compiled project.
