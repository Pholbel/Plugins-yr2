using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OBEPPlugIn
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
            Match exp = Regex.Match(response, "Status: </b>(?<EXP>.*?)<br>", RegOpt);
            if (exp.Success)
            {
                Expiration = exp.Groups["EXP"].ToString();
                if (Expiration != "Active")
                {
                    Sanction = SanctionType.Red;
                }
            }
        }

        private Result<string> ParseResponse(string response)
        {
            MatchCollection fields1 = Regex.Matches(response, "(?<=(<td>)).*(?=</td)");
            MatchCollection fields2 = Regex.Matches(response, "(?<=(<b>)).*(?=\n)");
            List<string> headers1 = new List<string>(new string[] { "Name", "Address Line1", "Address Line2", "Phone #" });
            //List<string> headers = new List<string>(new string[] {"Area", "License #", "Status", "Issued", "HSP", "Special Certification", "University", "Department", "Graduated"});

            if (fields1.Count > 3)
            {
                StringBuilder builder = new StringBuilder();

                for (int idx=3;idx<fields1.Count-5;idx++)
                {
                    string header = headers1[idx-3];
                    string text = CleanString(fields1[idx].ToString());

                    builder.AppendFormat(TdPair, header, text);
                    builder.AppendLine();
                }

                for (int idx=2;idx<fields2.Count-1;idx++)
                {
                    string exp = fields2[idx].ToString();
                    if (exp.Contains("<u>")) { continue; }
                    List<string> pair = exp.Split(':').ToList();
                    string header = CleanString(pair[0]);
                    string text = CleanString(pair[1]);

                    builder.AppendFormat(TdPair, header, text);
                    builder.AppendLine();
                }

                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }

        private string CleanString(string str)
        {
            str = str.Replace("<b>", "");
            str = str.Replace("</b>", "");
            str = str.Replace("<u>", "");
            str = str.Replace("<br>", "");
            str = str.Replace("</td>", "");

            return str;
        }
    }
}
