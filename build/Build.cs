using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.PowerShell;
using CP.BuildTools;
using System.Linq;

////[GitHubActions(
////    "BuildOnly",
////    GitHubActionsImage.WindowsLatest,
////    OnPushBranchesIgnore = new[] { "main" },
////    FetchDepth = 0,
////    InvokedTargets = new[] { nameof(Compile) })]
////[GitHubActions(
////    "BuildDeploy",
////    GitHubActionsImage.WindowsLatest,
////    OnPushBranches = new[] { "main" },
////    FetchDepth = 0,
////    ImportSecrets = new[] { nameof(NuGetApiKey) },
////    InvokedTargets = new[] { nameof(Compile), nameof(Deploy) })]
partial class Build : NukeBuild
{
    [GitRepository]
    private readonly GitRepository Repository;

    [Solution(GenerateProjects = true)]
    private readonly Solution Solution;

    [NerdbankGitVersioning]
    private readonly NerdbankGitVersioning NerdbankVersioning;

    [Parameter]
    [Secret]
    private readonly string NuGetApiKey;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    public static int Main() => Execute<Build>(x => x.Test);

    private AbsolutePath PackagesDirectory => RootDirectory / "output";

    private Target Print => _ => _
        .Executes(() => Log.Information("NerdbankVersioning = {Value}", NerdbankVersioning.NuGetPackageVersion));

    private Target Clean => _ => _
        .Before(Restore)
        .Executes(async () =>
        {
            if (IsLocalBuild)
            {
                return;
            }

            PackagesDirectory.CreateOrCleanDirectory();
            await this.InstallDotNetSdk("8.x.x", "9.x.x", "10.x.x");
        });

    private Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(s => s.SetProjectFile(Solution)));

    private Target Compile => _ => _
        .DependsOn(Restore, Print)
        .Executes(() => DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()));

    private Target Test => _ => _
       .DependsOn(Compile)
       .Executes(() =>
       {
           var testProjects = Solution.AllProjects.Where(x => x.GetProperty("IsTestProject") == "true").ToList();
           foreach (var project in testProjects!)
           {
               Log.Information("Testing {Project}", project.Name);

               // Run the test executable directly for each target framework
               // Microsoft.Testing.Platform on .NET 10 SDK requires running the test executable directly
               var projectDirectory = project.Directory;
               var targetFrameworks = project.GetTargetFrameworks();

               foreach (var tfm in targetFrameworks!)
               {
                   var testExePath = projectDirectory / "bin" / Configuration / tfm / $"{project.Name}.dll";
                   if (testExePath.FileExists())
                   {
                       Log.Information("Running tests for {Project} ({Framework})", project.Name, tfm);
                       DotNet($"exec {testExePath}");
                   }
               }
           }
       });

    private Target Pack => _ => _
    .After(Compile)
    .Produces(PackagesDirectory / "*.nupkg")
    .Executes(() =>
    {
        if (Repository.IsOnMainOrMasterBranch())
        {
            var packableProjects = Solution.GetPackableProjects();

            foreach (var project in packableProjects!)
            {
                Log.Information("Packing {Project}", project.Name);
            }

            DotNetPack(settings => settings
                .SetConfiguration(Configuration)
                .SetVersion(NerdbankVersioning.NuGetPackageVersion)
                .SetOutputDirectory(PackagesDirectory)
                .CombineWith(packableProjects, (packSettings, project) =>
                    packSettings.SetProject(project)));
        }
    });

    private Target Deploy => _ => _
    .DependsOn(Pack)
    .Requires(() => NuGetApiKey)
    .Executes(() =>
    {
        if (Repository.IsOnMainOrMasterBranch())
        {
            DotNetNuGetPush(settings => settings
                        .SetSource(this.PublicNuGetSource())
                        .SetSkipDuplicate(true)
                        .SetApiKey(NuGetApiKey)
                        .CombineWith(PackagesDirectory.GlobFiles("*.nupkg"), (s, v) => s.SetTargetPath(v)),
                    degreeOfParallelism: 5, completeOnFailure: true);
        }
    });
}
