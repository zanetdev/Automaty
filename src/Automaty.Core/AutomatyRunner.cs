namespace Automaty.Core
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using Automaty.Common.Logging;
	using Automaty.Core.Execution;
	using Automaty.Core.Logging;
	using Automaty.Core.Resolution;

#if NETSTANDARD2_0
	using System.Runtime.Loader;
#else
	using System.Reflection;
#endif

	public class AutomatyRunner
	{
		public AutomatyRunner() : this(new NullLoggerFactory())
		{
		}

		public AutomatyRunner(ILoggerFactory loggerFactory)
		{
			LoggerFactory = loggerFactory;
		}

		public ILoggerFactory LoggerFactory { get; set; }

		public bool Execute(IEnumerable<string> sourceFilePaths, string projectFilePath)
		{
			ILogger<AutomatyRunner> logger = LoggerFactory.CreateLogger<AutomatyRunner>();

			try
			{
				logger.WriteInfo($"AUTOMATY CODE GENERATOR");
				logger.WriteInfo($"VERSION = {typeof(AutomatyRunner).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version}");
				logger.WriteInfo($"NB: If you are accessing the assemblies within a generator (EG AppDomain.CurrentDomain.GetAssemblies() )" + Environment.NewLine +
					             $"    , then ensure that your projects ONLY reference Newtonsoft.Json 9.0.1 as " +
								 $"    Microsoft.Extensions.DependancyModel and Nuget.ProjectModel both depend on this version and will " +
								 $"    therefore remove any reference to other versions of Newtonsoft.Json");
				logger.WriteInfo("");

				logger.WriteDebug($"Starting Execution : "
								+ $"Version = {typeof(AutomatyRunner).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version}");

				if (sourceFilePaths == null || !sourceFilePaths.Any())
				{
					logger.WriteWarning($"{nameof(sourceFilePaths)} is null or empty.");
					return true;
				}

				logger.WriteDebug($"xxxxx 1");

				IEnumerable<RuntimeLibrary> runtimeLibraries = new List<RuntimeLibrary>();

				logger.WriteDebug($"xxxxx 2");

				if (!string.IsNullOrEmpty(projectFilePath))
				{
					logger.WriteDebug($"xxxxx 3");
					if (!File.Exists(projectFilePath))
					{
						logger.WriteDebug($"xxxxx 4");
						logger.WriteError($"Project file path '{projectFilePath}' not found.");
						return true;
					}

					logger.WriteDebug($"xxxxx 5");
					RuntimeLibraryResolver runtimeLibraryResolver =
						new RuntimeLibraryResolver(LoggerFactory.CreateLogger<RuntimeLibraryResolver>());
					logger.WriteDebug($"xxxxx 6");

					runtimeLibraries = runtimeLibraryResolver.GetRuntimeLibraries(projectFilePath);
					
					logger.WriteDebug($"xxxxx 7");
#if NETSTANDARD2_0
					AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) =>
					{
						RuntimeLibrary runtimeLibrary = runtimeLibraries.FirstOrDefault(x => x.AssemblyName == assemblyName.Name);

						if (runtimeLibrary != null)
						{
							logger.WriteDebug($"Resolving for {runtimeLibrary.AssemblyName}.");
						}

						return runtimeLibrary == null ? null : assemblyLoadContext.LoadFromAssemblyPath(runtimeLibrary.FilePath);
					};
#else
					AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
					{
						RuntimeLibrary runtimeLibrary = runtimeLibraries.FirstOrDefault(x => x.AssemblyName == args.Name);

						if (runtimeLibrary != null)
						{
							logger.WriteDebug($"Resolving for {runtimeLibrary.AssemblyName}.");
						}

						return runtimeLibrary == null ? null : Assembly.LoadFile(runtimeLibrary.FilePath);
					};
#endif
				}

				ScriptCompiler scriptCompiler = new ScriptCompiler(LoggerFactory.CreateLogger<ScriptCompiler>());
				scriptCompiler.AddRuntimeLibraries(runtimeLibraries);

				ScriptRunner scriptRunner = new ScriptRunner(scriptCompiler, LoggerFactory);

				foreach (string sourceFilePath in sourceFilePaths)
				{
					if (!File.Exists(sourceFilePath))
					{
						logger.WriteError($"Source file path '{sourceFilePath}' not found.");
						continue;
					}

					scriptRunner.Run(sourceFilePath);
				}
			}
			catch (AutomatyException automatyException)
			{
				logger.WriteError($"Exception of type {typeof(AutomatyException)} occured: {automatyException}.");

				return true;
			}
			catch (Exception e)
			{
				logger.WriteError($"Exception of type {e.GetType()} occured: {e}.");

				return false;
			}

			return false;
		}
	}
}