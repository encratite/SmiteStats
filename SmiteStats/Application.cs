using System;
using System.Reflection;

namespace SmiteStats
{
	class Application
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 1)
			{
				string name = Assembly.GetExecutingAssembly().ManifestModule.Name;
				Console.WriteLine("Usage:");
				Console.WriteLine("{0} <Smite match ID>", name);
				return;
			}
			int matchId = Convert.ToInt32(arguments[0]);
			var analysis = new Analysis(matchId);
			analysis.Run();
		}
	}
}
