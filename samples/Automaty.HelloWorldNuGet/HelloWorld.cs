﻿namespace Automaty.HelloWorldNuGet
{
	using Automaty.Common.Execution;
	using Automaty.Common.Output;
	using Newtonsoft.Json;

	public class HelloWorld : IAutomatyHost
	{
		public void Execute(IScriptContext context)
		{
			context.Output["helloworld.json"].WriteLine(JsonConvert.SerializeObject("Hello World!"));
		}
	}
}