#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#tool nuget:?package=GitReleaseNotes.Portable&version=0.7.1
//#tool nuget:?package=Wyam&version=2.2.7
//#addin nuget:?package=Cake.Wyam&version=2.2.7

//#load nuget:https://www.nuget.org/api/v2?package=Cake.Wyam.Recipe&version=0.7.1

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build");

// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if an Environment variable exists, use that.
var configuration =
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration") != null ? EnvironmentVariable("Configuration") : "Release";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// The build number to use in the version number of the built NuGet packages.
// There are multiple ways this value can be passed, this is a common pattern.
// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if running on AppVeyor, get it's build number.
// 3. Otherwise if running on Travis CI, get it's build number.
// 4. Otherwise if an Environment variable exists, use that.
// 5. Otherwise default the build number to 0.
var buildNumber =
    HasArgument("BuildNumber") ? Argument<int>("BuildNumber") :
    AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number :
    TravisCI.IsRunningOnTravisCI ? TravisCI.Environment.Build.BuildNumber :
    EnvironmentVariable("BuildNumber") != null ? int.Parse(EnvironmentVariable("BuildNumber")) : 0;

// Define directories.
var artifactsDirectory = MakeAbsolute(Directory("./BuildArtifacts"));
var solutionFile = "./Source/Axiom.sln";

Func<MSBuildSettings,MSBuildSettings> commonSettings = settings => settings
    .SetConfiguration(configuration)
    .WithProperty("PackageOutputPath", artifactsDirectory.FullPath)
        .WithProperty("nowarn","1591, 1572, 1573");

Environment.SetVariableNames();

//BuildParameters.SetParameters(context: Context,
//                            buildSystem: BuildSystem,
//                            title: "Axiom 3D Rendering Engine",
//                            repositoryOwner: "Axiom3D",
//                            repositoryName: "axiom",
//                            appVeyorAccountName: "borrillis",
//                            webHost: "axiom3d.github.io",
//                            wyamRecipe: "Docs",
//                            wyamTheme: "Samson",
//                            wyamSourceFiles: MakeAbsolute(Directory("./")).FullPath + "/**/{!bin,!obj,!packages,!*.Tests,}/**/*.cs",
//                            wyamPublishDirectoryPath: MakeAbsolute(Directory("./BuildArtifacts/gh-pages")),
//                            webLinkRoot: "/axiom",
//                            webBaseEditUrl: "https://github.com/axiom3d/axiom/tree/master/",
//                            shouldPublishDocumentation: true,
//                            shouldPurgeCloudflareCache: false);

//BuildParameters.PrintParameters(Context);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);

        MSBuild(solutionFile,
            settings => commonSettings(settings)
                        .WithTarget("Clean"));
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        MSBuild(solutionFile,
            settings => commonSettings(settings)
                        .WithTarget("Restore"));
    });

Task("Build-Product")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        // Use MSBuild
        MSBuild(solutionFile, settings => commonSettings(settings)
            .SetConfiguration(configuration));
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
            NoResults = true
            });
    });

Task("Package")
    .IsDependentOn("Test")
    .Does(() => 
    {
        GenerateReleaseNotes();

        MSBuild(solutionFile,
            settings => commonSettings(settings)
                        .SetConfiguration("Package")
                        .WithTarget("Build")
                        .WithProperty("OutDir", artifactsDirectory.FullPath)
                        .WithProperty("IncludeSymbols","true"));

    });

private void GenerateReleaseNotes()
{
    var releaseNotesExitCode = StartProcess(
        @"tools\GitReleaseNotes.Portable.0.7.1\tools\gitreleasenotes.exe", 
        new ProcessSettings { Arguments = ". /o BuildArtifacts/releasenotes.md" });
    if (string.IsNullOrEmpty(System.IO.File.ReadAllText("./BuildArtifacts/releasenotes.md")))
        System.IO.File.WriteAllText("./BuildArtifacts/releasenotes.md", "No issues closed since last release");

    if (releaseNotesExitCode != 0) throw new Exception("Failed to generate release notes");
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
/*
BuildParameters.Tasks.CleanDocumentationTask
    .IsDependentOn("Clean");

BuildParameters.Tasks.BuildDocumentationTask
    .IsDependentOn("Build-Product");

BuildParameters.Tasks.PreviewDocumentationTask
    .IsDependentOn("Build-Product");
*/
Task("Build")
    .IsDependentOn("Build-Product")
    /* .IsDependentOn("Build-Documentation")*/;

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
