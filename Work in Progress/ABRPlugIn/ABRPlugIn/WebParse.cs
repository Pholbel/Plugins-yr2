using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ABRPlugIn
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
                /*
                MatchCollection physicians = Regex.Matches(response.Content, "id=\"ctl00_cphMain_rptrPhysician_ctl00_lblLicense\"", RegOpt);
                MatchCollection assistants = Regex.Matches(response.Content, "id=\"ctl00_cphMain_rptrAssistant_ctl00_lblLicense\"", RegOpt);

                if (physicians.Count > 1 || assistants.Count > 1 || (physicians.Count > 0 && assistants.Count > 0))
                {
                    return Result<string>.Failure(ErrorMsg.MultipleProvidersFound);
                }
                else if (Regex.Match(response.Content, "No Physicians Match That License", RegOpt).Success 
                    && Regex.Match(response.Content, "No Physician Assistants Match That License", RegOpt).Success)
                {
                    return Result<string>.Failure(ErrorMsg.NoResultsFound);
                }
                else // Returned successful query
                {
                    CheckLicenseDetails(response.Content);
                    return ParseResponse(response.Content);
                }*/
                return ParseResponse(response.Content);
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }

        private void CheckLicenseDetails(string response)
        {
            Match exp = Regex.Match(response, "Expiration date: </span>( |\t|\r|\v|\f|\n)*<span.*?>(?<EXP>.*?)</span>", RegOpt);
            if (exp.Success)
            {
                Expiration = exp.Groups["EXP"].ToString();
            }

            //Does not support sanctions

        }

        private Result<string> ParseResponse(string response)
        {
            try
            {
                // get div with result
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                var startNode = doc.DocumentNode.SelectNodes("//comment()[contains(., 'End Pages')]").First();
                var endNode = doc.DocumentNode.SelectNodes("//comment()[contains(., 'Pages')]").Reverse().Skip(1).FirstOrDefault();
                int startNodeIndex = startNode.ParentNode.ChildNodes.IndexOf(startNode);
                int endNodeIndex = endNode.ParentNode.ChildNodes.IndexOf(endNode);
                var nodes = startNode.ParentNode.ChildNodes.Where((n, index) => index >= startNodeIndex && index <= endNodeIndex).Select(n => n);
                HtmlNode resultTab = nodes.Where((n) => n.Name.Contains("div")).FirstOrDefault();

                // gather data
                string fullName = Regex.Match(resultTab.InnerHtml, "(?<=strong><span.*>).*(?=</span></strong>)", RegOpt).ToString();
                string practiceLocations = Regex.Match(resultTab.InnerHtml, "(?<=Locations.*>).*(?=<br>.*<strong)", RegOpt).ToString();
                string participatingS = string.Empty;
                Match participating = Regex.Match(resultTab.InnerHtml, "Participating", RegOpt);
                if (participating.Success)
                {
                    participatingS = participating.ToString();
                }
                var headerMatches = Regex.Matches(resultTab.InnerHtml, "(?<=<th.*;.>)[\\w,\\s]*(?=)", RegOpt);
                var valueMatches = Regex.Matches(resultTab.InnerHtml, "(?<=td>)[^<]*(?=</td>)", RegOpt);

                // form return table
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat(TdPair, "Full Name", fullName);
                builder.AppendFormat(TdPair, "Practice Locations", practiceLocations);
                if (participatingS != string.Empty)
                {
                    builder.AppendFormat(TdPair, "Participating in MOC", "yes");
                }
                else
                {
                    builder.AppendFormat(TdPair, "Participating in MOC", "no");
                }
                for (var i = 0; i < valueMatches.Count; i++)
                {
                    // if a license is expired
                    if (valueMatches[i].ToString().Contains("Expired")) Sanction = SanctionType.Red;

                    // append key value pairs
                    builder.AppendFormat(TdPair, headerMatches[i % headerMatches.Count].ToString(), valueMatches[i].ToString());
                }


                return Result<string>.Success(builder.ToString());
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }
    }
}
