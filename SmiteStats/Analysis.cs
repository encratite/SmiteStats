using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmiteStats
{
	class Analysis
	{
		const string _Prefix = "http://account.hirezstudios.com/smitegame";
		const string _DifferenceFormat = "+#;-#";

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
			var tasks = new List<Task>();
			foreach (var node in nodes)
			{
				string path = node.Attributes["href"].Value;
				var pattern = new Regex("^stats\\.aspx\\?player=(.+)$");
				var match = pattern.Match(path);
				if (match == null)
					throw new ApplicationException("Unable to extract player name");
				string playerName = match.Groups[1].Value;
				string playerUri = string.Format("{0}/{1}", _Prefix, path);
				var task = new Task(() => PlayerTask(playerName, playerUri, differences));
				tasks.Add(task);
				task.Start();
			}
			foreach (var task in tasks)
				task.Wait();
			differences.Sort();
			double median = GetMedian(differences);
			Console.WriteLine("Median: {0}", median.ToString(_DifferenceFormat));
		}

		void PlayerTask(string playerName, string playerUri, List<int> differencesOutput)
		{
			int winLossDifference = GetPlayerWinLossDifference(playerUri);
			lock (differencesOutput)
			{
				Console.WriteLine("{0}: {1}", playerName, winLossDifference.ToString(_DifferenceFormat));
				differencesOutput.Add(winLossDifference);
			}
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

		int GetPlayerWinLossDifference(string uri)
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
			// Console.WriteLine("Downloading {0}", uri);
			var client = new WebClient();
			client.Encoding = Encoding.UTF8;
			string content = client.DownloadString(new Uri(uri));
			var document = new HtmlDocument();
			document.LoadHtml(content);
			return document;
		}
	}
}
