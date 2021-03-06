﻿using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDBPPlugIn
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

        private Result<string> ParseResponse(string response)
        {

            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            var body = doc.DocumentNode.SelectSingleNode("//body");
            
            if (body.InnerHtml != String.Empty)
            {

                //declarations
                StringBuilder builder = new StringBuilder();
                MatchCollection dataRgx = Regex.Matches(body.InnerHtml, @"(?<=ctl.*>+).*(?=</span)", RegOpt);
                List<string> data = new List<string>();
                
                for (var i = 0; i < dataRgx.Count-1; i+=2)
                {
                    if (dataRgx[i+1].ToString() != "")
                    {
                        data.Add(dataRgx[i].ToString());
                        data.Add(dataRgx[i+1].ToString());
                    }
                }


                //handle data
                for (var j = 0; j < data.Count-1; j+=2)
                {
                    if (data[j].Contains("Expiry"))
                    {
                        Expiration = data[j + 1];
                    }
                    builder.AppendFormat(TdPair, data[j], data[j+1]);
                    builder.AppendLine();
                }


                //handle sanctions
                if (Regex.Match(response, "Has Discipline").Success)
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
