using HtmlAgilityPack;
using Newtonsoft.Json;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DCNAPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<td>{0}</td>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        public WebParse()
        {
            Expiration = String.Empty;
            Sanction = SanctionType.None;
        }

        public Result<string> Execute(IRestResponse response)
        {
            try
            {
                return ParseResponse(response.Content);
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }
        
        private Result<string> ParseResponse(string response)
        {
            response = Clean(response);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);

            string licenseDetails = GetNodeValues(
                doc.DocumentNode.SelectSingleNode("//body")
            );

            return !String.IsNullOrWhiteSpace(licenseDetails) ? Result<string>.Success(licenseDetails) : Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
        }


        private string GetNodeValues(HtmlNode docNode)
        {
            StringBuilder builder = new StringBuilder();
            int step = 0;

            //get each moduleHeaderLabel and append as tr
            //then get the following nodes that have class data as a set and add
            //as td pairs underneath with the date as the first column(td)

            HtmlNodeCollection headers = docNode.SelectNodes("//h3");
            HtmlNodeCollection sections = new HtmlNodeCollection(docNode);
            List<string> headersText = new List<string>();

            foreach (var h in headers)
            {
                headersText.Add(h.InnerText);
                sections.Add(h.NextSibling);
            }

            if (sections != null)
            {
                foreach (var s in sections)
                {
                    builder.AppendFormat(TdPair, headersText[step], String.Empty);
                    builder.AppendLine();
                    step++;

                    //handle demographics
                    if (s.PreviousSibling.InnerText.Contains("DEMOGRAPHICS"))
                    {
                        builder.AppendFormat(TdSingle, "Name");
                        builder.AppendFormat(TdSingle, s.SelectSingleNode("tr").LastChild.InnerText);
                        builder.AppendLine();
                    }

                    //handle certification
                    else if (s.PreviousSibling.InnerText.Contains("CERTIFICATION"))
                    {
                        HtmlNodeCollection rows = s.SelectNodes("tr");
                        foreach (var r in rows)
                        {
                            HtmlNodeCollection cells = r.SelectNodes("td");
                            foreach (var c in cells)
                            {
                                builder.AppendFormat(TdSingle, c.InnerText);
                            }
                            builder.AppendLine();
                        }
                    }

                    //handle substantiated findings
                    else if (s.PreviousSibling.InnerText.Contains("SUBSTANTIATED FINDINGS"))
                    {
                        builder.AppendFormat(TdSingle, "Data");
                        builder.AppendFormat(TdSingle, s.NextSibling.InnerText);
                        builder.AppendLine();
                    }

                    else { break; }
                }
            }

            return builder.ToString();
        }

        public string Clean(string input)
        {
            string[] junk = { "\r\n", "</br>", "<br />", "<br/>", "<br>", "&nbsp;", "    " };
            foreach (var j in junk)
            {
                input = input.Replace(j, string.Empty);
            }
            return input;
        }
    }
}
