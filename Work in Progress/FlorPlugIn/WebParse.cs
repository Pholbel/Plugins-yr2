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

                string previousHeader = string.Empty;
                bool containsInt = false;
                bool qualificationsDoubled = false;
                int i;
                int k;
                //added logic because some providers were having multiple "Qualifications values being returned"
                //this updated logic will handle it and after the for loop add the last remaining key value pair if there were two qualification values
                for ( i= 0; i < headerNodes.Count(); i++)
                {
                    if (valueNodes.Count > i)
                    {
                        containsInt = valueNodes[i].InnerText.Any(char.IsDigit);
                        if (!String.IsNullOrEmpty(previousHeader) && previousHeader.Contains("Qualifications"))
                        {                     
                            if (!containsInt)
                            {
                                qualificationsDoubled = true;
                                h_list.Add(previousHeader);
                                v_list.Add(valueNodes[i].InnerText);
                            }

                        }
                        else if (qualificationsDoubled)
                        {
                            k = i - 1;
                            h_list.Add(headerNodes[k].InnerText);
                            v_list.Add(valueNodes[i].InnerText);
                            
                        }
                        else
                        {
                            h_list.Add(headerNodes[i].InnerText);
                            v_list.Add(valueNodes[i].InnerText);
                        }

                        previousHeader = headerNodes[i].InnerText;
                    }
                }

                if (qualificationsDoubled && headerNodes.Count > 0)
                {
                    i = headerNodes.Count() - 1;
                    k = valueNodes.Count() - 1;
                    h_list.Add(headerNodes[i].InnerText);
                    v_list.Add(valueNodes[i].InnerText);
                }

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
