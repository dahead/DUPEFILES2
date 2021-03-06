using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dupefiles2.Core
{

	public class IndexDataModel : IndexItemList
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

		internal bool ContainsItem(string fullName)
		{
			foreach (var item in this)
			{
				if (item.FullName == fullName)
					return true;
			}
			return false;
		}

		internal void MarkDuplicates(IndexCompareDataModel dupes)
		{
			var list = this.Where(t => t.Hash == dupes.Hash);
			foreach (var item in list)
				item.IsDupe = true;
		}
	}

	public class IndexItemList : List<IndexItemDataModel>
	{

	}

	public class IndexItemDataModel
	{
		public string FullName { get; set; }
		public string DirectoryName { get; set; }
		public long Size { get; set; }
		public string Hash { get; set; }
		public bool IsDupe { get; set; }
	}

	public class IndexAddDataModel
	{
		public string BaseDirectory { get; set; }
		public int Count { get; set; }

	}

	public class IndexUpdateDataModel
	{
		public string FullName { get; set; }
		public string Action { get; set; }
	}

	public class IndexCompareDataModelList : List<IndexCompareDataModel>
	{

	}

	public class IndexCompareDataModel
	{
		public string Hash { get; set; }
		public string Fullname1 { get; set; }
		public string Fullname2 { get; set; }
		public bool Identical { get; set; }
		public long Size { get; set; }
	}

	public class IndexPurgeDataModel
	{
		public string Fullname { get; set; }
		public Commands.IndexPurgeCommand.PurgeMode Mode { get; set; }
		public bool Success { get; set; }
	}
}