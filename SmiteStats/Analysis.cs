using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;

namespace SmiteStats
{
	class Analysis
	{
		const string _Prefix = "http://account.hirezstudios.com/smitegame";

		int _MatchId;

		public Analysis(int matchId)
		{
			_MatchId = matchId;
		}

		public void Run()
		{
			string uri = string.Format("{0}/match-details.aspx?match={1}", _Prefix, _MatchId);
			var document = Download(uri);
			var nodes = document.DocumentNode.SelectNodes("//a[@id = 'pdName' and starts-with(@href, 'stats.aspx?player=')]");
			var differences = new List<int>();
			foreach (var node in nodes)
			{
				string path = node.Attributes["href"].Value;
				string playerUri = string.Format("{0}/{1}", _Prefix, path);
				int winLossDifference = GetPlayerWinLossDiffernce(playerUri);
				Console.WriteLine("{0}: {1}{2}", path, winLossDifference > 0 ? "+" : "", winLossDifference);
				differences.Add(winLossDifference);
			}
			differences.Sort();
			double median = GetMedian(differences);
			Console.WriteLine("Median: {0}", median);
		}

		double GetMedian(List<int> input)
		{
			int count = input.Count;
			if(count == 0)
				throw new ApplicationException("No win/loss differences available");
			double output;
			int center = count / 2;
			if (count % 2 == 0)
			{
				int rightIndex = center;
				int leftIndex = rightIndex - 1;
				output = (input[leftIndex] + input[rightIndex]) / 2.0;
			}
			else
				output = input[center];
			return output;
		}

		int GetPlayerWinLossDiffernce(string uri)
		{
			var document = Download(uri);
			var container = document.DocumentNode.SelectSingleNode("//div[@id = 'conquestTab']");
			if (container == null)
				throw new ApplicationException("Unable to find conquest stats");
			var nodes = container.SelectNodes(".//div[starts-with(@class, 'shadowDrop')]");
			int winLossDifference = 0;
			foreach (var node in nodes)
			{
				int difference = GetGodWinLossDifference(node);
				winLossDifference += difference;
			}
			return winLossDifference;
		}

		int GetGodWinLossDifference(HtmlNode container)
		{
			int wins = ExtractInteger(container, "lblWins");
			int losses = ExtractInteger(container, "lblLosses");
			int difference = wins - losses;
			return difference;
		}

		string ExtractValue(HtmlNode container, string id)
		{
			var node = container.SelectSingleNode(string.Format(".//span[@id = '{0}']", id));
			if (node == null)
				throw new ApplicationException(string.Format("Unable to find value with ID \"{0}\"", id));
			return node.InnerText;
		}

		int ExtractInteger(HtmlNode container, string id)
		{
			string value = ExtractValue(container, id);
			int output;
			if (!Int32.TryParse(value, out output))
				throw new ApplicationException(string.Format("Unable to parse integer for ID \"{0}\" from the following string: {1}", id, value));
			return output;
		}

		HtmlDocument Download(string uri)
		{
			Console.WriteLine("Downloading {0}", uri);
			var client = new WebClient();
			string content = client.DownloadString(new Uri(uri));
			var document = new HtmlDocument();
			document.LoadHtml(content);
			return document;
		}
	}
}
