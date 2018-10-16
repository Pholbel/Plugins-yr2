using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using PlugInWebScraper.Helpers;

namespace PlugInWebScraper.Models
{
    public class PSV
    {
        public string Action
        {
            get;
            set;
        }

        public string AppletFlag
        {
            get;
            set;
        }
        public string DecodeAction
        {
            get
            {
                return Decode(this.Action);
            }
        }

        public Credential Credent
        {
            get;
            set;
        }

        public byte[] ImagePDF
        {
            get;
            set;
        }

        public bool IsError
        {
            get
            {
                return Regex.IsMatch(Result, @"Error:", RegexOptions.IgnoreCase);
            }
        }

        public string Result
        {
            get;
            set;
        }

        public bool SendBack
        {
            get;
            set;
        }

        private string Decode(string action)
        {
            switch (action != null ? action.ToUpper() : "")
            {
                case "E":
                    action = "Expirables";
                    break;

                case "S":
                    action = "Sanctions";
                    break;

                default:
                    action = "Normal";
                    break;
            }

            return action;
        }

        public string GetMeta()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine("------Result------");
            output.AppendLineFormat("Action: {0}", this.Action);
            output.AppendLineFormat("SendBack: {0}", this.SendBack.ToString());
            output.AppendLineFormat("Flag: {0}", this.AppletFlag);
            output.AppendLine("------Credential------");
            output.AppendLineFormat("Name: {0} {1} {2}", this.Credent.FirstName, this.Credent.MiddleName, this.Credent.LastName);
            output.AppendLineFormat("Title: {0}", this.Credent.Title);
            output.AppendLineFormat("License Number: {0}", this.Credent.LicenseNumber);
            output.AppendLineFormat("License Expiration: {0}", this.Credent.LicenseExpiration);
            output.AppendLineFormat("NPI: {0}", this.Credent.NPI);
            output.AppendLine(String.Empty);

            return output.ToString();
        }
    }
}
