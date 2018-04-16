namespace Automaty.Core.Logging
{
	using System;
	using Automaty.Common.Logging;

	public class NullLogger<T> : ILogger<T>
	{
		public void WriteDebug(string text)
		{
		}

		public void WriteError(string text)
		{
		}

		public void WriteInfo(string text, ConsoleColor consoleColor = ConsoleColor.Black, bool setConsoleColor = true)
		{

		}

		public void WriteWarning(string text)
		{
		}
	}
}