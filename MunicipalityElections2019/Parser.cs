using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using HtmlAgilityPack;
using RestSharp;

namespace MunicipalityElections2019
{
	public class Parser
	{
		private readonly List<string> candidateData;

		private readonly RestClient restClient;

		public Parser()
		{
			candidateData = new List<string>
			{
				"№ п/п,ФИО кандидата,Дата рождения кандидата,Субьект выдвижения,Номер округа,выдвижение,регистрация,избрание"
			};


			restClient = new RestClient("http://www.st-petersburg.vybory.izbirkom.ru/region/st-petersburg");
		}

		public string[] Parse()
		{
			//dfsdfs
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			ParseIdsPage();

			var result = candidateData.ToArray();
			candidateData.Clear();

			return result;
		}

		private HtmlDocument GetHtmlPage(IRestResponse response)
		{
			var pageHtml = Encoding.GetEncoding("Windows-1251").GetString(response.RawBytes);
			HtmlDocument pageDocument = new HtmlDocument();
			pageDocument.LoadHtml(pageHtml);

			return pageDocument;
		}

		private void ParseIdsPage()
		{
			var request = new RestRequest("", Method.POST);
			request.AddParameter("start_date", "01.04.2019", ParameterType.GetOrPost);
			request.AddParameter("urovproved", 4, ParameterType.GetOrPost);
			request.AddParameter("vidvibref", "all", ParameterType.GetOrPost);
			request.AddParameter("vibtype", "all", ParameterType.GetOrPost);
			request.AddParameter("end_date", "01.10.2019", ParameterType.GetOrPost);
			request.AddParameter("sxemavib", "all", ParameterType.GetOrPost);
			request.AddParameter("action", "search_by_calendar", ParameterType.GetOrPost);
			request.AddParameter("region", "78", ParameterType.GetOrPost);

			var response = restClient.Execute(request);
			var doc = GetHtmlPage(response);

			//ToDo: make XPath beautiful
			var tableAnchors = doc.DocumentNode.SelectNodes("//body/table[3]//table[2]//a");

			var urls = tableAnchors.Select(a => new Uri(a.Attributes["href"].Value));

			int count = 1;
			foreach (var url in urls)
			{
				Console.Clear();
				Console.WriteLine(count);
				ParseEventPage(url.Query);
				count++;
			}

		}

		private void ParseEventPage(string query)
		{
			var request = new RestRequest(query, Method.POST);

			var response = restClient.Execute(request);
			var doc = GetHtmlPage(response);

			var link = doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Сведения о кандидатах')]");
			if (link != null)
			{
				var url = new Uri(link.Attributes["href"].Value.Replace("amp;", ""));
				var queryColl = HttpUtility.ParseQueryString(url.Query);

				ParseDataPage(queryColl["tvd"], queryColl["vrn"]);
			}
		}

		private void ParseDataPage(string tvd, string vrn)
		{
			var request = new RestRequest("", Method.POST);
			
			// Add query
			request.AddQueryParameter("action", "show");
			request.AddQueryParameter("root", "1");
			request.AddQueryParameter("tvd", tvd);
			request.AddQueryParameter("vrn", vrn);
			request.AddQueryParameter("region", "78");
			request.AddQueryParameter("global", String.Empty);
			request.AddQueryParameter("sub_region", "78");
			request.AddQueryParameter("pronetvd", "null");
			request.AddQueryParameter("vibid", vrn);
			request.AddQueryParameter("type", "220");

			// Add form
			request.AddParameter("search_surname", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("search_name", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("search_secondname", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("search_party", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("search_okrug", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("search_vidvig", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("search_registr", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("search_izbr", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("vrnio", String.Empty, ParameterType.GetOrPost);
			request.AddParameter("rep_action", "search", ParameterType.GetOrPost);

			var response = restClient.Execute(request);
			var doc = GetHtmlPage(response);

			var rows = doc.DocumentNode.SelectNodes("//tbody[@id='test']/tr");

			foreach (var row in rows)
			{
				var data = row.ChildNodes
					.Where(node => node.Name.Equals("td"))
					.Select(node => node.InnerText.Trim());

				candidateData.Add(String.Join(',', data));
			}
		}
	}
}