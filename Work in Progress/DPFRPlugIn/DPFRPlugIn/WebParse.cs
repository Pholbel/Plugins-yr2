using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DPFRPlugIn
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
                return ParseResponse(response.Content);
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }
        
        private Result<string> ParseResponse(string response)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'DetailGroup')]");
            

            if (nodes.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                
                //Handle general details
                foreach (var k in nodes[0].ChildNodes)
                {
                    if (k.Name.Contains("div"))
                    {
                        builder.AppendFormat(TdPair, k.ChildNodes[1].InnerText, k.ChildNodes[3].InnerText);
                    }
                }

                //Handle history
                var histTable = nodes[1].ChildNodes["table"].ChildNodes["tbody"];
                foreach (var tr in histTable.ChildNodes)
                {
                    if (tr.Name.Contains("tr"))
                    {
                        for (var j = 0; j < tr.ChildNodes.Count; j++)
                        {
                            if (j == 1)
                            {
                                builder.AppendFormat(TdPair, "License Type", tr.ChildNodes[j].InnerText);
                            } else if (j == 3)
                            {
                                builder.AppendFormat(TdPair, "Start Date", tr.ChildNodes[j].InnerText);
                            } else if (j == 5)
                            {
                                builder.AppendFormat(TdPair, "End Date", tr.ChildNodes[j].InnerText);
                            }
                        }
                    }
                }

                //Handle Authority Section
                var authTable = nodes[3].ChildNodes["table"].ChildNodes["tbody"];
                foreach (var c in authTable.ChildNodes)
                {
                    if (c.Name.Contains("tr"))
                    {
                        for (var k = 0; k < c.ChildNodes.Count; k++)
                        {
                            if (k == 1)
                            {
                                builder.AppendFormat(TdPair, "Description", c.ChildNodes[k].InnerText);
                            } else if (k == 3)
                            {
                                builder.AppendFormat(TdPair, "Issue Date", c.ChildNodes[k].InnerText);
                            } else if (k == 5)
                            {
                                builder.AppendFormat(TdPair, "Termination Date", c.ChildNodes[k].InnerText);
                            } else if (k == 7)
                            {
                                builder.AppendFormat(TdPair, "Status", c.ChildNodes[k].InnerText);
                            }
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
