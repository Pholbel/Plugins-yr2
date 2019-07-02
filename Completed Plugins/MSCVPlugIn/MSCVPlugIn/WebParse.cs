using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MSCVPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<td>{0}</td>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        private RegexOptions RegOpt2 = RegexOptions.Singleline;

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
            Match exp = Regex.Match(response, "LPC Renewal Date</b><b>:</b>(?<EXP>.*?)<br>", RegOpt);
            Expiration = exp.Groups["EXP"].ToString();

            //Does not support sanctions
            Match GoodDiscipline = Regex.Match(response, "There is no current disciplinary action.", RegOpt);
            if (!GoodDiscipline.Success)
            {
                Sanction = SanctionType.Red;
            }
            // if Status on detail page is not ACTIVE, set flag
            Match Status = Regex.Match(response, "Status</b><b>:</b>(?<Status>.*?)<br>", RegOpt);
            string status = Status.Groups["Status"].ToString().Trim();
            if (status != "Active")
            {
                Sanction = SanctionType.Red;
            }
        }

        private Result<string> ParseResponse(string response)
        {
            MatchCollection fields = Regex.Matches(response, "<b>(?!:)(?<HEAD>.*?)</b>", RegOpt);
            MatchCollection fields2 = Regex.Matches(response, "</b>(?::|<b>:</b>|<b>/Board Certified-TeleMental Health .BC-TMH.:</b>)(?<VALUE>.*?)<br>", RegOpt);
            

            if (fields.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int idx=0;idx<fields2.Count;idx++)
                {
                    // special case for the last HEAD parsed, they are seperated in two matches
                    if (idx == fields2.Count - 1)
                    {
                        string header = fields[idx].Groups["HEAD"].ToString().Trim() + fields[idx+1].Groups["HEAD"].ToString().Trim();
                        string text = fields2[idx].Groups["VALUE"].ToString().Trim();

                        builder.AppendFormat(TdPair, header, text);
                        builder.AppendLine();
                    }
                    else
                    {
                        string header = fields[idx].Groups["HEAD"].ToString().Trim();
                        string text = fields2[idx].Groups["VALUE"].ToString().Trim();

                        builder.AppendFormat(TdPair, header, text);
                        builder.AppendLine();
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
