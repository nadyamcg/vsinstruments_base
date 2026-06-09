using System;
using System.IO;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace CakeBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }

    public class BuildContext : FrostingContext
    {
        public const string ProjectName = "VSInstrumentsBase";
        public const string ZipName = "vsinstruments_base.zip";
        public string BuildConfiguration { get; }
        public string Version { get; }
        public string Name { get; }
        public bool SkipJsonValidation { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            BuildConfiguration = context.Argument("configuration", "Release");
            SkipJsonValidation = context.Argument("skipJsonValidation", false);
            var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../../modinfo.json");
            Version = modInfo.Version;
            Name = modInfo.ModID;
        }
    }

    [TaskName("ValidateJson")]
    public sealed class ValidateJsonTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (context.SkipJsonValidation)
                return;
            var jsonFiles = context.GetFiles($"../../assets/**/*.json");
            foreach (var file in jsonFiles)
            {
                try
                {
                    JToken.Parse(File.ReadAllText(file.FullPath));
                }
                catch (JsonException ex)
                {
                    throw new Exception($"validation failed for json file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
                }
            }
        }
    }

    [TaskName("Build")]
    [IsDependentOn(typeof(ValidateJsonTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetClean($"../../{BuildContext.ProjectName}.csproj",
                new DotNetCleanSettings { Configuration = context.BuildConfiguration });
            context.DotNetBuild($"../../{BuildContext.ProjectName}.csproj",
                new DotNetBuildSettings { Configuration = context.BuildConfiguration });
        }
    }

    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var releaseDir = "../../release";
            var stageDir = $"{releaseDir}/{context.Name}";
            var modDir = $"../../bin/{context.BuildConfiguration}/Mods/mod";

            context.EnsureDirectoryExists(releaseDir);
            context.CleanDirectory(releaseDir);
            context.EnsureDirectoryExists(stageDir);

            // assembly + drywetmidi managed dll
            context.CopyFiles($"{modDir}/*.dll", stageDir);
            // assets and modinfo from source
            if (context.DirectoryExists("../../assets"))
                context.CopyDirectory("../../assets", $"{stageDir}/assets");
            context.CopyFile("../../modinfo.json", $"{stageDir}/modinfo.json");
            if (context.FileExists("../../modicon.png"))
                context.CopyFile("../../modicon.png", $"{stageDir}/modicon.png");

            context.Zip(stageDir, $"{releaseDir}/{BuildContext.ZipName}");
            context.DeleteDirectory(stageDir, new DeleteDirectorySettings { Recursive = true, Force = true });
        }
    }

    [TaskName("Default")]
    [IsDependentOn(typeof(PackageTask))]
    public class DefaultTask : FrostingTask
    {
    }
}
