﻿namespace Automaty.Core.Resolution
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Automaty.Common.Logging;
	using Automaty.Core.Logging;
	using Microsoft.Build.Evaluation;
	using NuGet.LibraryModel;
	using NuGet.Packaging;
	using NuGet.ProjectModel;

	public class RuntimeLibraryResolver
	{
		public RuntimeLibraryResolver() : this(new NullLogger<RuntimeLibraryResolver>())
		{
		}

		public RuntimeLibraryResolver(ILogger<RuntimeLibraryResolver> logger)
		{
			Logger = logger;
		}

		public ILogger<RuntimeLibraryResolver> Logger { get; set; }

		public IEnumerable<RuntimeLibrary> GetRuntimeLibraries(string projectFilePath)
		{
			Logger.WriteDebug("Reading project file.");

			projectFilePath = Path.GetFullPath(projectFilePath.ToPlatformSpecificPath());

			Logger.WriteDebug($"xxxxx1 1");

			Environment.SetEnvironmentVariable(MSBuildFinder.MSBuildEnvironmentVariableName, MSBuildFinder.Find());

			Logger.WriteDebug($"xxxxx1 2");

			ProjectCollection projectCollection = new ProjectCollection();
			Logger.WriteDebug($"xxxxx1 3");

			Project project = projectCollection.LoadProject(projectFilePath);
			Logger.WriteDebug($"xxxxx1 4");

			List<RuntimeLibrary> runtimeLibraries = new List<RuntimeLibrary>();

			Logger.WriteDebug($"xxxxx1 5");

			runtimeLibraries.AddRange(GetProjectReferences(projectFilePath, projectCollection, project));
			Logger.WriteDebug($"xxxxx1 6");
			runtimeLibraries.AddRange(GetNugetReferences(projectFilePath, project));
			Logger.WriteDebug($"xxxxx1 7");
			return runtimeLibraries;
		}

		protected string GetMultiTargetingProjectAssemblyPath(string directoryPath, string targetFrameworks,
			string outputPath, string assemblyName)
		{
			string targetFramework = GetSuitableTargetFramework(targetFrameworks);
			Logger.WriteDebug($"Using target framework {targetFramework}.");

			return Path.GetFullPath(Path.Combine(directoryPath, outputPath, targetFramework, $"{assemblyName}.dll"));
		}

		protected IEnumerable<RuntimeLibrary> GetNugetReferences(string projectFilePath, Project project)
		{
			Logger.WriteInfo("Adding nuget references.");

			ICollection<RuntimeLibrary> runtimeLibraries = new List<RuntimeLibrary>();

			LockFileFormat lockFileFormat = new LockFileFormat();

			string lockFileFilePath = Path.Combine(Path.GetDirectoryName(projectFilePath),
				project.GetPropertyValue(PropertyNames.ProjectAssetsFile));

			if (!File.Exists(lockFileFilePath))
			{
				Logger.WriteError($"Lock file {lockFileFilePath} not found. Run dotnet restore before executing Automaty.");

				throw new AutomatyException();
			}

			LockFile lockFile = lockFileFormat.Read(lockFileFilePath);

			string targetFramework = project.GetPropertyValue(PropertyNames.TargetFramework);

			if (string.IsNullOrEmpty(targetFramework))
			{
				Logger.WriteDebug("Multi targeting project assembly detected.");
				targetFramework = GetSuitableTargetFramework(project.GetPropertyValue(PropertyNames.TargetFrameworks));
				Logger.WriteDebug($"Using target framework {targetFramework}.");
			}

			LockFileTarget lockFileTarget = lockFile.GetTarget(NuGetUtils.ParseFrameworkName(targetFramework), string.Empty);

			NuGetPackageResolver nuGetPackageResolver =
				NuGetPackageResolver.CreateResolver(lockFile, Path.GetDirectoryName(projectFilePath));

			// Add nuget references
			foreach (LockFileTargetLibrary library in lockFileTarget.Libraries)
			{
				if (library.Type != LibraryType.Package)
				{
					continue;
				}

				string packageDirectory = nuGetPackageResolver.GetPackageDirectory(library.Name, library.Version);

				foreach (LockFileItem file in library.RuntimeAssemblies.Where(file => !NuGetUtils.IsPlaceholderFile(file.Path)))
				{
					string filePath = Path.GetFullPath(Path.Combine(packageDirectory, file.Path));

					Logger.WriteDebug($"Adding \"{filePath}\".");

					runtimeLibraries.Add(new RuntimeLibrary
					{
						Name = library.Name,
						DirectoryName = Path.GetDirectoryName(filePath),
						FileName = Path.GetFileName(filePath)
					});
				}
			}

			return runtimeLibraries;
		}

		protected string GetProjectAssemblyPath(string directoryPath, string outputPath, string assemblyName)
		{
			return Path.GetFullPath(Path.Combine(directoryPath, outputPath, $"{assemblyName}.dll"));
		}

		protected IEnumerable<RuntimeLibrary> GetProjectReferences(string projectFilePath,
			ProjectCollection projectCollection, Project project)
		{
			Logger.WriteInfo("Adding project references.");

			ICollection<RuntimeLibrary> runtimeLibraries = new List<RuntimeLibrary>();

			ICollection<ProjectItem> projectItems = project.GetItems(ItemNames.ProjectReference);

			// Add project references
			foreach (ProjectItem projectItem in projectItems)
			{
				Project referencedProject =
					projectCollection.LoadProject(Path.Combine(Path.GetDirectoryName(projectFilePath), projectItem.EvaluatedInclude));

				var referencedProjectFilePath = Path.GetFullPath(referencedProject.FullPath.ToPlatformSpecificPath());

				//runtimeLibraries.AddRange(GetProjectReferences(referencedProjectFilePath, projectCollection, referencedProject));

				// TODO: Log?
				if (referencedProject.GetProperty(PropertyNames.OutputType).EvaluatedValue != "Library")
				{
					Logger.WriteDebug($"Not adding project as it not a library : {referencedProject.FullPath}");
					continue;
				}

				string outputPath = referencedProject.GetProperty(PropertyNames.OutputPath).EvaluatedValue.ToPlatformSpecificPath();
				string assemblyName = referencedProject.GetProperty(PropertyNames.AssemblyName).EvaluatedValue;
				string targetFrameworks = referencedProject.GetProperty(PropertyNames.TargetFrameworks)?.EvaluatedValue;

				string filePath;
				if (targetFrameworks != null)
				{
					Logger.WriteDebug("Multi targeting project assembly referenced.");
					filePath = GetMultiTargetingProjectAssemblyPath(referencedProject.DirectoryPath, targetFrameworks, outputPath,
						assemblyName);
				}
				else
				{
					filePath = GetProjectAssemblyPath(referencedProject.DirectoryPath, outputPath, assemblyName);
				}

				Logger.WriteDebug($"Adding \"{filePath}\".");

				runtimeLibraries.Add(new RuntimeLibrary
				{
					Name = assemblyName,
					DirectoryName = Path.GetDirectoryName(filePath),
					FileName = Path.GetFileName(filePath)
				});
			}

			return runtimeLibraries;
		}

		protected string GetSuitableTargetFramework(string targetFrameworks)
		{
			string[] frameworks = targetFrameworks.Split(';');

			string targetFramework =
				frameworks.FirstOrDefault(f => f.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) ??
				frameworks.FirstOrDefault(f => f.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase)) ?? frameworks[0];

			return targetFramework;
		}
	}
}