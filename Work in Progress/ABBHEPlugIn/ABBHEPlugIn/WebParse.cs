using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ABBHEPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<td>{0}</td>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase;

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

        private void CheckLicenseDetails(string response)
        {

            MatchCollection exp = Regex.Matches(response, @"ACTIVE", RegOpt);
            if (exp.Count != 0)
            {
               Expiration = exp.ToString();
            }

            //Does not support sanctions

        }

        private Result<string> ParseResponse(string response)
        {

            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            var body = doc.DocumentNode.SelectSingleNode("//body");
            
            if (body.InnerHtml != String.Empty)
            {
                StringBuilder builder = new StringBuilder();
                HtmlNode sec_info = body.SelectSingleNode("//div/table");
                HtmlNode sec_licenses = body.SelectNodes("//table")[1];



                //Handle info section
                foreach (var detail in sec_info.ChildNodes)
                {
                    if (detail.Name == "tr")
                    {
                        HtmlNodeCollection cells = detail.ChildNodes;
                        string h = cells[1].InnerText;
                        string t = cells[3].InnerText.Replace("&nbsp;", " ");
                        builder.AppendFormat(TdPair, h, t);
                        builder.AppendLine();
                    }
                }

                //Handle licenses section

                //Get headers
                HtmlNodeCollection headers = sec_licenses.ChildNodes[1].ChildNodes;
                List<string> hList = new List<string>();
                foreach (var header in headers)
                {
                    if (header.Name == "td")
                    {
                        hList.Add(header.InnerText.Replace("\n  ", "").Substring(2));
                    }
                }
                hList.RemoveAt(0);

                //get each license
                HtmlNodeCollection licenses = sec_licenses.ChildNodes;
                foreach (var license in licenses)
                {
                    if (license.Name == "tr" && license.PreviousSibling.PreviousSibling != null)
                    {
                        int count = 0;
                        bool isActive = false;
                        foreach (var cell in license.ChildNodes)
                        {
                            if (cell.Name == "td" && cell.PreviousSibling.PreviousSibling != null)
                            {
                                if (cell.InnerText.Contains("ACTIVE"))
                                {
                                    isActive = true;
                                }
                                if (count == 4 && isActive)
                                {
                                    Expiration = cell.InnerText;
                                }
                                builder.AppendFormat(TdPair, hList[count], cell.InnerText);
                                builder.AppendLine();
                                count++;
                            }
                        }
                        isActive = false;
                        count = 0;
                    }
                }

                //handle sanctions
                if (!response.Contains("There are no Board actions"))
                {
                    Sanction = SanctionType.Red;
                }
                


                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }
    }
}
