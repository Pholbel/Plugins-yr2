using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARBECPlugIn
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
            //MatchCollection fields = Regex.Matches(response, "(?<=(id=\"ctl.*\">)).*(?=</s)");
            List<string> headers = new List<string>(new string[] {"Name", "License Number", "License Type", "Status", "City", "Zip", "Date of issue", "Date of expiration", "Standing"});

            Match licenseContentTag = Regex.Match(response, @"LICENSEE CONTENT HERE");

            if (licenseContentTag.Success)
            {
                StringBuilder builder = new StringBuilder();

                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var nodes = doc.DocumentNode.SelectNodes("//*[contains(@class,'col-xs-12')]");
                var wrapper = nodes[nodes.Count - 1];
                var tags = wrapper.ChildNodes;
                var count = 0;

                var licInfo = new string[6];


                foreach (var tag in tags)
                {
                    if (!tag.Name.Contains("#"))
                    {
                        if (headers[count].Contains("Number"))
                        {
                            licInfo = tag.InnerText.Split(new Char[] { ':', '|' });
                            var number = licInfo[1];
                            builder.AppendFormat(TdPair, headers[count], number);
                            count++;
                        }
                        else if (headers[count].Contains("Type"))
                        {
                            var type = licInfo[3];
                            builder.AppendFormat(TdPair, headers[count], type);
                            count++;
                        }
                        else if (headers[count].Contains("Status"))
                        {
                            var status = licInfo[5];
                            builder.AppendFormat(TdPair, headers[count], status);
                            count++;
                        }
                        else
                        {
                            builder.AppendFormat(TdPair, headers[count], tag.InnerText);
                            count++;
                        }
                    }
                    
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
