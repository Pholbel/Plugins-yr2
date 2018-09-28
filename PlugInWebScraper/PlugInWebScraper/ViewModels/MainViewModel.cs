using PlugInWebScraper.Command;
using PlugInWebScraper.Helpers;
using PlugInWebScraper.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;

namespace PlugInWebScraper.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        public ICommand CmdStartScrape { get; set; }

        public MainViewModel()
        {
            CmdStartScrape = new RelayCommand(StartScrape);
            SelectedDocument = Properties.Settings.Default["SelectedDocument"].ToString();
            SelectedPlugIn = Properties.Settings.Default["SelectedPlugIn"].ToString();

            worker.DoWork += worker_StartScrape;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        public WebCrawler providers { get; set; }
        private string PlugInSupportsImage
        {
            get
            {
                return "2";
            }
        }

        public ObservableCollection<string> TestDocuments
        {
            get
            {
                DirectoryInfo d = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "/TestDocuments");
                FileInfo[] Files = d.GetFiles("*.xml");
                return new ObservableCollection<string>(Files.Select(p => p.Name).ToList());
            }
        }

        public string SelectedDocument
        {
            get
            {
                return selectedDocument;
            }
            set
            {
                SetProperty(ref selectedDocument, value);
                if (!String.IsNullOrEmpty(value))
                {
                    Properties.Settings.Default["SelectedDocument"] = value;
                    TestPlugIns = new ObservableCollection<string>(XmlLoader.GetAssemblies(value));
                }
            }
        }
        private string selectedDocument;

        public ObservableCollection<string> TestPlugIns
        {
            get
            {
                return testPlugIns;
            }
            set
            {
                SetProperty(ref testPlugIns, value);
            }
        }
        private ObservableCollection<string> testPlugIns;

        public string SelectedPlugIn
        {
            get
            {
                return selectedPlugIn; 
            }
            set
            {
                SetProperty(ref selectedPlugIn, value);
                Properties.Settings.Default["SelectedPlugIn"] = value;
            }
        }
        private string selectedPlugIn;

        public ObservableCollection<PSV> PSVResult
        {
            get
            {
                return psvResult;
            }
            set
            {
                SetProperty(ref psvResult, value);
            }
        }
        private ObservableCollection<PSV> psvResult;

        public string StatusMessage
        {
            get
            {
                return statusMessage;
            }
            set
            {
                SetProperty(ref statusMessage, value);
            }
        }
        private string statusMessage;

        public bool ShowLoading
        {
            get
            {
                return showLoading;
            }
            set
            {
                SetProperty(ref showLoading, value);
            }
        }
        private bool showLoading;

        private void StartScrape()
        {
            ShowLoading = true;
            StatusMessage = "Running...";
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Using the selected settings, load referenced data & begin web scrape
        /// </summary>
        private void worker_StartScrape(object sender, DoWorkEventArgs e)
        {
            providers = new WebCrawler(this.SelectedPlugIn, this.SelectedDocument, this.PlugInSupportsImage);
            providers.GetResult();
        }

        /// <summary>
        /// Set the result to PSV so it can bind to UI
        /// </summary>
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Result<List<PSV>> result = providers.ScrapeResult;

            if (result.IsValid)
            {
                PSVResult = new ObservableCollection<PSV>(result.Value);
                StatusMessage = "Finished";
            }
            else
            {
                //show error message
                StatusMessage = result.Message;
            }
            ShowLoading = false;
        }

    }
}
