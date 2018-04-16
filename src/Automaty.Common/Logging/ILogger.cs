using System;

namespace Automaty.Common.Logging
{
	public interface ILogger<T>
	{
		void WriteDebug(string text);

		void WriteError(string text);

		void WriteInfo(string text, ConsoleColor consoleColor = ConsoleColor.Black, bool setConsoleColor = true);

		void WriteWarning(string text);
	}
}