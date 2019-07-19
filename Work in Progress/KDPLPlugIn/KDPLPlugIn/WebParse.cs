using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KDPLPlugIn
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
            // DECLARATIONS
            HtmlDocument doc = new HtmlDocument();
            HtmlDocument parent = new HtmlDocument();
            HtmlDocument table = new HtmlDocument();
            List<string> hList = new List<string>();
            List<string> vList = new List<string>();
            StringBuilder builder = new StringBuilder();

            // GET ROWS WITH HEADERS AND VALUES
            doc.LoadHtml(response);
            parent.LoadHtml(doc.GetElementbyId("ContentPlaceHolder2_LData").InnerHtml);
            table.LoadHtml(parent.DocumentNode.SelectNodes("//table[@class='tablestyle13']").Last().InnerHtml);
            var rows = table.DocumentNode.SelectNodes("//tr");

            // ISOLATE HEADERS AND DATA
            foreach (var row in rows)
            {
                if (row.ChildNodes.Count > 1)
                {
                    if (row.Attributes.Count > 0 && row.Attributes["class"].Value.Contains("trstyle3"))
                    {
                        foreach (var header in row.ChildNodes)   // is the header row
                        {
                            if (header.InnerText != string.Empty) hList.Add(header.InnerText);
                        }
                    }
                    else
                    {
                        foreach (var value in row.ChildNodes)   // is the value row
                        {
                            vList.Add(value.InnerText);
                        }
                    }
                }
            }

            // FORM THE DATA
            for (var i = 0; i < hList.Count; i++)
            {
                if (hList[i].Contains("Disciplinary") && vList[i].Contains("Yes")) Sanction = SanctionType.Red;
                builder.AppendFormat(TdPair, hList[i], vList[i]);
                builder.AppendLine();
            }


            return Result<string>.Success(builder.ToString());
        }
    }
}
