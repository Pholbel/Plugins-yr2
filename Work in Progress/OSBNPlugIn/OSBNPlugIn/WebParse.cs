using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OSBNPlugIn
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
            StringBuilder builder = new StringBuilder();
            HtmlDocument doc = new HtmlDocument();
            List<string> hList = new List<string>();
            List<string> vList = new List<string>();

            try
            {

                // GET HEADER AND VALUES FOR FIRST SECTION
                doc.LoadHtml(response);
                hList.Add("SECTION");
                vList.Add("PERSONAL INFORMATION");
                hList.Add(doc.GetElementbyId("Label1").InnerText.Trim(':'));
                vList.Add(doc.GetElementbyId("lblLicenseeName").InnerText.Trim(':'));
                hList.Add(doc.GetElementbyId("Label2").InnerText.Trim(':'));
                vList.Add(doc.GetElementbyId("lblGender").InnerText.Trim(':'));
                hList.Add(doc.GetElementbyId("Label5").InnerText.Trim(':'));
                vList.Add(doc.GetElementbyId("lblCity").InnerText.Trim(':'));
                hList.Add(doc.GetElementbyId("Label7").InnerText.Trim(':'));
                vList.Add(doc.GetElementbyId("lblState").InnerText.Trim(':'));

                // GET HEADER AND VALUES FOR SECOND SECTION
                hList.Add("SECTION");
                vList.Add("LICENSES");
                var licTable = doc.GetElementbyId("gvLicenses");
                var th = licTable.Descendants("tr").First().Descendants("th");
                var dataRows = licTable.Descendants("tr");
                foreach (var row in dataRows)
                {
                    if (row != th)
                    {
                        var dataCols = row.Descendants("td");
                        for (var i = 0; i < dataCols.Count(); i++)
                        {
                            hList.Add(th.ElementAt(i).InnerText);
                            vList.Add(dataCols.ElementAt(i).InnerText);
                        }
                    }
                }

                // GET HEADER VALUES FOR DISCIPLINE
                if (!Regex.Match(response, "No disciplinary actions on record.", RegOpt).Success)
                {
                    Sanction = SanctionType.Red;
                    hList.Add("SECTION");
                    vList.Add("BOARD ORDERS");
                    var boardTable = doc.GetElementbyId("gvDiscipline");
                    var bth = boardTable.Descendants("tr").First().Descendants("th");
                    var bdataRows = boardTable.Descendants("tr");
                    foreach (var row in bdataRows)
                    {
                        if (row != bth)
                        {
                            var bdataCols = row.Descendants("td");
                            for (var i = 0; i < bdataCols.Count(); i++)
                            {
                                hList.Add(bth.ElementAt(i).InnerText);
                                vList.Add(bdataCols.ElementAt(i).InnerText);
                            }
                        }
                    }
                }

                // GET HEADER VALUES FOR ABUSE
                if (Regex.Match(response, "FINDINGS OF ABUSE", RegOpt).Success)
                {
                    hList.Add("SECTION");
                    vList.Add("FINDINGS OF ABUSE");
                    var abuseTable = doc.GetElementbyId("gvAbuse");
                    var ath = abuseTable.Descendants("tr").First().Descendants("th");
                    var adataRows = abuseTable.Descendants("tr");
                    foreach (var row in adataRows)
                    {
                        if (row != ath)
                        {
                            var adataCols = row.Descendants("td");
                            for (var i = 0; i < adataCols.Count(); i++)
                            {
                                hList.Add(ath.ElementAt(i).InnerText);
                                vList.Add(adataCols.ElementAt(i).InnerText);
                            }
                        }
                    }
                }

                // GATHER DATA for return
                for (var i = 0; i < hList.Count; i++)
                {
                    builder.AppendFormat(TdPair, hList[i], vList[i]);
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
