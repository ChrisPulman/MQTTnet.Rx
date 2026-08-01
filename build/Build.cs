// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace MQTTnet.Rx.Building;

/// <summary>Defines the repository build pipeline.</summary>
public sealed class Build : NukeBuild
{
    /// <summary>Gets the repository solution that the build pipeline operates on.</summary>
    private static readonly AbsolutePath SolutionFile = RootDirectory / "src" / "MQTTnet.Rx.slnx";

    /// <summary>Gets or sets the build configuration.</summary>
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public Configuration Configuration { get; set; } = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    /// <summary>Gets the directory used for generated packages.</summary>
    private static AbsolutePath PackagesDirectory => RootDirectory / "output";

    /// <summary>Gets the target that displays the effective build configuration and version override.</summary>
    private Target Print => target => target
        .Executes(() =>
        {
            Log.Information("Configuration = {Configuration}", Configuration);
            Log.Information("MinVerVersionOverride = {Value}", Environment.GetEnvironmentVariable("MinVerVersionOverride") ?? "<auto>");
        });

    /// <summary>Gets the target that cleans generated package output before a non-local restore.</summary>
    private Target Clean => target => target
        .Before(Restore)
        .Executes(() =>
        {
            if (IsLocalBuild)
            {
                return;
            }

            _ = PackagesDirectory.CreateOrCleanDirectory();
        });

    /// <summary>Gets the target that restores the repository solution dependencies.</summary>
    private Target Restore => target => target
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(s => s.SetProjectFile(SolutionFile)));

    /// <summary>Gets the target that builds the repository solution for the selected configuration.</summary>
    private Target Compile => target => target
        .DependsOn(Restore, Print)
        .Executes(() => DotNetBuild(s => s
                .SetProjectFile(SolutionFile)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)));

    /// <summary>Runs the requested NUKE targets.</summary>
    /// <returns>The process exit code.</returns>
    public static int Main() => Execute<Build>(x => x.Compile);
}
