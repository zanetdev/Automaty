using System;
using Xunit;
using Automaty.Core;

namespace Automaty.ManualTest
{
	using Automaty.Manual.Test;

	public class UnitTest1
    {
		protected const string ProjectDirectoryPath = "./samples/Automaty.Samples.HelloWorld/";

		protected const string ProjectFilePath = "Automaty.Samples.HelloWorld.csproj";

        [Fact]
        public void Test1()
        {
			string sampleProjectDirectoryPath = ProjectDirectoryPath.ToPlatformSpecificPath();
			string projectFilePath = ProjectFilePath;

			string sourceFilePath = "HelloWorldWithContext.cs";
			string generatedFilePath = "helloworldwithcontext.txt";

			//Helper.AssertSampleProjectDirectoryPathExists(sampleProjectDirectoryPath);
			//Helper.AssertGeneratedFileDoesNotExist(sampleProjectDirectoryPath, generatedFilePath);

			//Helper.DotNetRestore(sampleProjectDirectoryPath, projectFilePath);
			MyHelper.AutomatyRun(sampleProjectDirectoryPath, sourceFilePath, projectFilePath);
			//Helper.AssertGeneratedFileExists(sampleProjectDirectoryPath, generatedFilePath);

			//Assert.AreEqual($"Hello World!{Environment.NewLine}", File.ReadAllText(Path.Combine(sampleProjectDirectoryPath, generatedFilePath)));
        }
    }
}
