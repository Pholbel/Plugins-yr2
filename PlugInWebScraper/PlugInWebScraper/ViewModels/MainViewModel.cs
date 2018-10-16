using PlugInWebScraper.Command;
using PlugInWebScraper.Helpers;
using PlugInWebScraper.Helpers.Loaders;
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
    public enum Operation
    {
        RUNSCRAPE,
        GENERATETESTS,
    }
    public class MainViewModel : ViewModelBase
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        public ICommand CmdStartScrape { get; set; }
        public ICommand CmdGenerateTests { get; set; }
        public ICommand CmdCheckedTheme { get; set; }

        public MainViewModel()
        {
            CmdStartScrape = new RelayCommand(StartScrape);
            CmdGenerateTests = new RelayCommand(GenerateTests);
            //CmdCheckedTheme = new RelayCommand(ToggleTheme);
            SelectedDocument = Properties.Settings.Default["SelectedDocument"].ToString();
            SelectedPlugIn = Properties.Settings.Default["SelectedPlugIn"].ToString();

            worker.DoWork += worker_StartOperation;
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
                var files = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "/TestDocuments", "*", SearchOption.AllDirectories)
                    .Where(file => file.ToLower().EndsWith(".json") || file.ToLower().EndsWith(".xml"))
                    .Select(file => new FileInfo(file))
                    .ToList();

                return new ObservableCollection<string>(files.Select(file => file.Name).ToList());
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

                    ILoader loader = Loader.GetLoader(value);

                    TestPlugIns = new ObservableCollection<string>(loader.GetAssemblies(value));
                    PSVResult = null;
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
                PlugInWebScraper.Properties.Settings.Default.Save();
                PSVResult = null;
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
        public bool IsVisibleDetails
        {
            get
            {
                return isVisibleDetails;
            }
            set
            {
                SetProperty(ref isVisibleDetails, value);
            }
        }
        private bool isVisibleDetails = true;
        public bool IsDarkMode
        {
            get
            {
                return isDarkMode;
            }
            set
            {
                Properties.Settings.Default["IsDarkMode"] = value;
                PlugInWebScraper.Properties.Settings.Default.Save();
                App.ToggleTheme(value);
                SetProperty(ref isDarkMode, value);
            }
        }
        private bool isDarkMode = Convert.ToBoolean(Properties.Settings.Default["IsDarkMode"]);

        
        private void StartScrape()
        {
            IsVisibleDetails = true;
            PSVResult = null;
            ShowLoading = true;
            StatusMessage = "Running...";
            worker.RunWorkerAsync(Operation.RUNSCRAPE);
        }

        private void GenerateTests()
        {
            IsVisibleDetails = false;
            PSVResult = null;
            ShowLoading = true;
            StatusMessage = "Running...";
            worker.RunWorkerAsync(Operation.GENERATETESTS);
        }


        /// <summary>
        /// Begins background task operation
        /// </summary>
        private void worker_StartOperation(object sender, DoWorkEventArgs e)
        {
            switch ((Operation)e.Argument)
            {
                case Operation.RUNSCRAPE:
                    StartScrapeAction();
                    break;
                case Operation.GENERATETESTS:
                    GenerateTestAction();
                    break;
            }
        }

        /// <summary>
        /// Generates test cases
        /// </summary>
        private void GenerateTestAction()
        {

            providers = new WebCrawler(this.SelectedPlugIn, this.SelectedDocument, this.PlugInSupportsImage);
            providers.GetTests();
        }

        /// <summary>
        /// Using the selected settings, load referenced data & begin web scrape
        /// </summary>
        private void StartScrapeAction()
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
