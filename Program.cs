using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ID_mining
{
	class Program
	{
		static void Main(string[] args)
		{
			DateTime _time = DateTime.Now;
			List<string> list_ID;
			List<string> list_Delete;
			Dictionary<string, int> listArticle;
			Dictionary<string, int> dicFinal = new Dictionary<string, int>();
			Dictionary<string, int> dicFinal2 = new Dictionary<string, int>();
			GetInfoCsv recup = new GetInfoCsv();

			list_ID = recup.ListId;
			list_Delete = recup.ListDeleteWords;
			foreach (var ID in list_ID)
			{
				GetWordsArticle words = new GetWordsArticle(ID, list_Delete);
				listArticle = words.WordsCountArticle;
							
				if (listArticle != null)
					dicFinal2 = dicFinal2.Concat(listArticle).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.First().Value);
				Console.WriteLine("OK");
			}
			dicFinal2 = dicFinal2.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
			string csv = String.Join(Environment.NewLine, dicFinal2.Select(d => d.Key + ";" + d.Value + ";"));
			File.WriteAllText("pipelineWords.csv", csv);

			Console.WriteLine("\n" + (DateTime.Now - _time) + " s");
			Console.ReadKey();
		}

		public class GetInfoCsv
		{
			public List<string> ListId { get; set; }
			public List<string> ListClassic { get; set; }
			public List<string> ListUseless { get; set; }
			public List<string> ListDeleteWords { get; set; }

			public GetInfoCsv()
			{
				ListId = new List<string>();
				ListClassic = new List<string>();
				ListUseless = new List<string>();
				ListDeleteWords = new List<string>();

				Get_IDFromCsv();
				Classic_WordsFromCsv();
				Useless_WordsFromCsv();
				GetDeleteWords();
			}

			private void Get_IDFromCsv()
			{
				using (var reader = new StreamReader("pipeline_id_list.csv"))
				{
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();

						ListId.Add("https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pmc&format=xml&id=PMC" + line);
					}
				}
			}

			private void Classic_WordsFromCsv()
			{
				using (var reader = new StreamReader("classic_word.csv"))
				{
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						ListClassic.Add(line.ToLower());
					}
				}
			}

			private void Useless_WordsFromCsv()
			{
				using (var reader = new StreamReader("useless_words.csv"))
				{
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						ListUseless.Add(line.ToLower());
					}
				}
			}

			private void GetDeleteWords()
			{
				ListDeleteWords.AddRange(ListClassic);
				ListDeleteWords.AddRange(ListUseless);
				ListDeleteWords = ListDeleteWords.Distinct().ToList();
			}
		}

		public class GetWordsArticle
		{
			string UrlPmc { get; set; }
			string Article { get; set; }
			string CleanArticle { get; set; }
			string RegexArticle { get; set; }
			List<string> SplitArticle { get; set; }
			List<string> ListUseless { get; set; }
			public Dictionary<string, int> WordsCountArticle { get; set; }

			public GetWordsArticle(string ID, List<string> UselessWords)
			{
				UrlPmc = ID;
				ListUseless = UselessWords;

				if (GetArticleFromWeb() == true)
				{
					if (ParseArticleHTML() == true)
					{
						if (CleanArticleRegex() == true)
						{
							if (SplitWordsFromRegexArticle() == true)
								CountWordsFromSplitArticle();
						}
					}
				}
			}

			private bool GetArticleFromWeb()
			{
				bool retour = false;
				using (WebClient client = new WebClient())
				{
					Article = client.DownloadString(UrlPmc);
				}
				if (Article != null)
					retour = true;
				return retour;
			}

			private bool ParseArticleHTML()
			{
				bool retour = false;
				var htmlDoc = new HtmlDocument();
				htmlDoc.LoadHtml(Article);
				var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");
				if (htmlBody != null)
					CleanArticle = htmlBody.InnerText;
				if (CleanArticle != null)
					retour = true;
				return retour;
			}

			private bool CleanArticleRegex()
			{
				bool retour = false;
				string pattern = "[0-9]|\n|\r|\t|\f|\v|[.,;:()%&_=/\\\"'#]";
				RegexArticle = Regex.Replace(CleanArticle, pattern, "");
				if (RegexArticle != null)
					retour = true;
				return retour;
			}

			private bool SplitWordsFromRegexArticle()
			{
				bool retour = false;
				char[] delimiters = new char[] { ' ' };
				SplitArticle = RegexArticle.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).ToList();
				SplitArticle.RemoveAll(x => ListUseless.Contains(x));
				if (SplitArticle != null)
					retour = true;
				return retour;
			}

			private void CountWordsFromSplitArticle()
			{
				var groups = SplitArticle.GroupBy(s => s).Select(s => new { Word = s.Key, Count = s.Count() });
				WordsCountArticle = groups.ToDictionary(g => g.Word, g => g.Count);
			}

		}
	}
}

