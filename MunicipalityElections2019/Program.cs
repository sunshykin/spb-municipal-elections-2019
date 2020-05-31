using System.IO;

namespace MunicipalityElections2019
{
	class Program
	{
		static void Main(string[] args)
		{
			var parser = new Parser();

			var data = parser.Parse();

			File.WriteAllLines("data.csv", data);
		}
	}
}
