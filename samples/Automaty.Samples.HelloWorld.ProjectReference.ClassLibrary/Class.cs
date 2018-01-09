namespace Automaty.Samples.HelloWorld.ProjectReference.ClassLibrary
{
	using System.IO;

	public class Class
	{
		public void test()
		{
			var x = new ClassLibraryLevel2.Level2Class();
			x.TestDependancyOnMultilevelProject();

		}
	}
}