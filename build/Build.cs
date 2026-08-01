using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
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
    private readonly GitRepository Repository = default;

    [NerdbankGitVersioning]
    private readonly NerdbankGitVersioning NerdbankVersioning = default;

    [Parameter]
    [Secret]
    private readonly string NuGetApiKey = default;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    public static int Main() => Execute<Build>(x => x.Test);

    private static AbsolutePath PackagesDirectory => RootDirectory / "output";

    private static AbsolutePath SolutionFile => RootDirectory / "src" / "MQTTnet.Rx.slnx";

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
            await this.InstallDotNetSdk("8.x.x", "9.x.x", "10.x.x", "11.0.100-preview.6.26359.118");
        });

    private Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(s => s.SetProjectFile(SolutionFile)));

    private Target Compile => _ => _
        .DependsOn(Restore, Print)
        .Executes(() => DotNetBuild(s => s
                .SetProjectFile(SolutionFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore()));

    private Target Test => _ => _
       .DependsOn(Compile)
       .Executes(() => DotNetTest(settings => settings
           .SetProjectFile(SolutionFile)
           .SetConfiguration(Configuration)
           .EnableNoBuild()
           .EnableNoRestore()));

    private Target Pack => _ => _
    .After(Compile)
    .Produces(PackagesDirectory / "*.nupkg")
    .Executes(() =>
    {
        if (Repository.IsOnMainOrMasterBranch())
        {
            var packableProjects = SolutionFile
                .ReadSolutionProjectInfos()
                .GetPackableProjectInfos();

            foreach (var project in packableProjects!)
            {
                Log.Information("Packing {Project}", project.Name);
            }

            DotNetPack(settings => settings
                .SetConfiguration(Configuration)
                .SetVersion(NerdbankVersioning.NuGetPackageVersion)
                .SetOutputDirectory(PackagesDirectory)
                .CombineWith(packableProjects, (packSettings, project) =>
                    packSettings.SetProject(project.Path)));
        }
    });

    private Target Deploy => _ => _
    .DependsOn(Pack)
    .Requires(() => !string.IsNullOrWhiteSpace(NuGetApiKey))
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
