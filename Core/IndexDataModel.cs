using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace todo.Core
{

	public class IndexDataModel : List<IndexItemDataModel>
	{

		public const string DefaultFilename = "index.json";

		public static IndexDataModel LoadFromFile()
		{
			return IndexDataModel.LoadFromFile(DefaultFilename);
		}

		private static IndexDataModel LoadFromFile(string filename)
		{
			try
			{
				using (StreamReader file = File.OpenText(filename))
				{
					JsonSerializer serializer = new JsonSerializer();
					return (IndexDataModel)serializer.Deserialize(file, typeof(IndexDataModel));
				}
			}
			catch (System.Exception)
			{
				return new IndexDataModel();
			}
		}

		public static void SaveToFile(IndexDataModel index)
		{
			IndexDataModel.SaveToFile(index, DefaultFilename);
		}

		private static bool SaveToFile(IndexDataModel index, string filename)
		{
			try
			{
				using (StreamWriter file = File.CreateText(filename))
				{
					JsonSerializer serializer = new JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented };
					serializer.Serialize(file, index);
				}
				return true;
			}
			catch (System.Exception)
			{
			}
			return false;
		}

	}

	public class IndexItemDataModel
	{
		public string Path { get; set; }
		public long Size { get; set; }
		public string Hash { get; set; }
	}

	public class IndexAddDataModel
	{
		// public int PercentageComplete { get; set; } = 0;
		public string BaseDirectory { get; set; }
		public int Count { get; set; }

	}

	public class IndexUpdateDataModel
	{
		public string Path { get; set; }
		public string Action { get; set; }
	}

	public class IndexCompareDataModel
	{
		public string File1 { get; set; }
		public string File2 { get; set; }
		public bool Identical { get; set; }
	}
}