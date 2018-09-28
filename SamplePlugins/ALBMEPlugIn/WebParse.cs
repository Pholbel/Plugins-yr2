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

namespace ALBMEPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
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
                CheckLicenseDetails(response.Content);
                return ParseResponse(response.Content);
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }

        private void CheckLicenseDetails(string response)
        {
            Match exp = Regex.Match(response, "Expirationdate2\">(?<EXP>.*?)</span>", RegOpt);
            if (exp.Success)
            {
                Expiration = exp.Groups["EXP"].ToString();
            }

            Match status = Regex.Match(response, "Licensestatus2\">(?<ACTION>.*?)</span>", RegOpt);
            if (status.Success)
            {
                Sanction = Regex.Match(status.Groups["ACTION"].ToString(), "None", RegOpt).Success ? SanctionType.None : SanctionType.Red;
                Sanction = Regex.Match(status.Groups["ACTION"].ToString(), "Active", RegOpt).Success && !Regex.IsMatch(status.Groups["ACTION"].ToString(), "Not Active", RegOpt) ? SanctionType.None : SanctionType.Red;
            }
        }

        private Result<string> ParseResponse(string response)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);

            string licenseDetails = GetNodeValues(
                doc.DocumentNode.SelectSingleNode("//div[@class='container']"),
                ".//div[@class='row']", 
                ".//div[@class='row']", 
                ".//*[self::span or self::b or self::table]"
            );

            return !String.IsNullOrWhiteSpace(licenseDetails) ? Result<string>.Success(licenseDetails) : Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
        }


        private string GetNodeValues(HtmlNode docNode, params string[] xpaths)
        {
            StringBuilder builder = new StringBuilder();
            HtmlNodeCollection sections = docNode.SelectNodes(xpaths[0]);

            if (sections != null)
            {
                foreach (var s in sections)
                {
                    if (xpaths.Count() > 1)
                    {
                        builder.AppendLine(GetNodeValues(s, xpaths.Skip(1).ToArray()));
                    }
                    else if (s.Name == "b")
                    {
                        builder.AppendFormat(TdPair, s.InnerText, String.Empty);
                    }
                    else if (s.Name == "table")
                    {
                        builder.AppendFormat(ParseTable(s));
                    }
                    else
                    {
                        builder.AppendFormat("<td>{0}</td>", s.InnerText);
                    }
                }
            }

            return builder.ToString();
        }

        private string ParseTable(HtmlNode table)
        {
            StringBuilder builder = new StringBuilder();

            try
            {
                if (table != null)
                {
                    List<string> headers = (from x in table.SelectNodes(".//tr/th") ?? new HtmlNodeCollection(null)
                                            select x != null ? x.InnerText : String.Empty).ToList();
                    List<string> cells = (from x in table.SelectNodes(".//tr/td") ?? new HtmlNodeCollection(null)
                                          select x != null ? x.InnerText : String.Empty).ToList();
                    int i = 0;
                    foreach (var c in cells)
                    {
                        if (i >= headers.Count)
                        {
                            i = 0;
                        }
                        string header = headers.Count > i ? headers[i++] : String.Empty;

                        builder.AppendFormat(TdPair, !String.IsNullOrWhiteSpace(header) ? header : c, !String.IsNullOrWhiteSpace(header) ? c : String.Empty);
                    }
                }
            }
            catch { }

            return builder.ToString();
        }
    }
}
