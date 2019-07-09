using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace FlorPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }
        public string PrinterFriendlyUrl { get; set; }

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
            //expirables
            Match exp = Regex.Match(response, "Expiration Date.*?</dt>( |\t|\r|\v|\f|\n)*<dd.*?>(?<EXP>.*?)</dd>", RegOpt);

            if (exp.Success)
            {
                Expiration = exp.Groups["EXP"].ToString();
            }

            //sanctions
            Match sanc = Regex.Match(response, "Discipline on File.*?</dt>.*?<span.*?>(?<EXP>.*?)</span>", RegOpt);
            if (sanc.Success)
            {
                Sanction = (sanc.Groups["EXP"].ToString() == "No") ? SanctionType.None : SanctionType.Red;                
            }
        }

        private Result<string> ParseResponse(string response)
        {
            try
            {
                //get printer friendly url
                Match printMatch = Regex.Match(response, "<a href=\"/MQASearchServices/(?<LINK>.*?)\".*?>printer friendly", RegexOptions.IgnoreCase);
                PrinterFriendlyUrl = printMatch.Success ? string.Format("https://appsmqa.doh.state.fl.us/MQASearchServices/{0}", HttpUtility.HtmlDecode(printMatch.Groups["LINK"].ToString())) : string.Empty;

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);

                var fieldset = doc.DocumentNode.SelectSingleNode("//fieldset");
                var headerNodes = fieldset.SelectNodes("//dt").ToList();
                var valueNodes = fieldset.SelectNodes("//dd").Where(y => y.ParentNode.ParentNode.ParentNode.Id == "General").ToList();

                List<string> h_list = new List<string>();
                List<string> v_list = new List<string>();

                int i;
                HtmlNode qHeader = new HtmlNode(HtmlNodeType.Element, doc,0);
                int qidx = 0;
                int addridx = 0;
                int addrcnt = 1;
                char[] white = { ' ' , '\r', '\n' };

                //set index of qualifications header and blank header
                for (var j = 0; j < headerNodes.Count; j++)
                {
                    if (headerNodes[j].InnerText.Contains("Qualifications"))
                    {
                        qidx = j+1;
                        headerNodes[j].InnerHtml = "Qualification";
                    }
                    if (headerNodes[j].InnerText == string.Empty)
                    {
                        qHeader = headerNodes[j];
                    }
                }

                //set qheader text and address headers texts
                qHeader.InnerHtml = "Qualification";

                


                //coerce lengths of keys and values to be equal
                while (headerNodes.Count < valueNodes.Count)
                {
                    headerNodes.Insert(qidx, qHeader);
                }
                
                for ( i= 0; i < valueNodes.Count(); i++)
                {
                    h_list.Add(headerNodes[i].InnerText);
                    v_list.Add(valueNodes[i].InnerText);
                }

                //remove empty header value pairs
                for (var x = 0; x < h_list.Count; x++)
                {
                    if (h_list[x].Trim(white) == string.Empty && v_list[x].Trim(white) == string.Empty)
                    {
                        h_list.RemoveAt(x); v_list.RemoveAt(x);
                        x--;
                    }
                }

                //find first address line
                for (var k = 0; k < h_list.Count; k++)
                {
                    if (h_list[k].Contains("Address"))
                    {
                        addridx = k;
                    }
                }
                
                //add header address lines
                while (h_list[addridx].Trim(white) == string.Empty || h_list[addridx].Contains("Address"))
                {
                    if (!h_list[addridx].Contains("Record"))
                    {
                        h_list[addridx] = "Address Line " + addrcnt.ToString();
                    }
                    addridx++;
                    addrcnt++;
                }


                // Gather results
                if (v_list.Count > 0)
                {
                    StringBuilder builder = new StringBuilder();
                    Match name = Regex.Match(response, "License Verification</h2>.*?<h3>(?<EXP>.*?)</h3>", RegOpt);

                    if (!name.Success)
                    {
                        Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
                    }

                    builder.AppendFormat(TdPair, "Full Name", name.Groups["EXP"].ToString());
                    builder.AppendLine();

                    for (int idx = 0; idx < h_list.Count; idx++)
                    {
                        builder.AppendFormat(TdPair, h_list[idx], v_list[idx]);
                        builder.AppendLine();
                    }

                    return Result<string>.Success(builder.ToString());
                }
                else // Error parsing table
                {
                    return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
                }
            }
            catch (Exception e)
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }
    }
}
