﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.Properties;

namespace UlrikHovsgaardWpf.ViewModels
{
    public delegate void DcrGraphLoaded(DcrGraph graph);

    public delegate void LogLoaded(Log log);

    public delegate void AlphabetSizeSelected(int alphabetSize);

    public class StartOptionsWindowViewModel : CloseableViewModel, INotifyPropertyChanged
    {
        public event DcrGraphLoaded DcrGraphLoaded;
        public event LogLoaded LogLoaded;
        public event AlphabetSizeSelected AlphabetSizeSelected;


        #region Fields

        private const string OwnFileSelected = "Select your own file";
        private const string HospitalLog = "Hospital workflow log";
        private const string BpiChallenge2015 = "BPI Challenge 2015";
        private const string BpiChallenge2015Mini = "BPI Challenge 2015, smaller";

        #endregion

        #region Properties

        private ObservableCollection<string> _logChoices;
        private string _logChosen;
        private string _alphabetSize;
        private bool _isWaiting;

        public ObservableCollection<string> LogChoices { get { return _logChoices; } set { _logChoices = value; OnPropertyChanged(); } }
        public string LogChosen { get { return _logChosen; }
            set
            {
                _logChosen = value;
                OnPropertyChanged();
                LogChosenConfirmed();
            }
        }
        public string AlphabetSize { get { return _alphabetSize; } set { _alphabetSize = value; OnPropertyChanged(); } }
        public bool IsWaiting { get { return _isWaiting; } set { _isWaiting = value; OnPropertyChanged(); ProcessUITasks(); } }
        

        #region Command properties

        private ICommand _logChosenConfirmedCommand;
        private ICommand _alphabetSizeChosenConfirmedCommand;
        private ICommand _dcrGraphChosenConfirmedCommand;

        public ICommand LogChosenConfirmedCommand { get { return _logChosenConfirmedCommand; } set { _logChosenConfirmedCommand = value; OnPropertyChanged(); } }
        public ICommand AlphabetSizeChosenConfirmedCommand { get { return _alphabetSizeChosenConfirmedCommand; } set { _alphabetSizeChosenConfirmedCommand = value; OnPropertyChanged(); } }
        public ICommand DcrGraphChosenConfirmedCommand { get { return _dcrGraphChosenConfirmedCommand; } set { _dcrGraphChosenConfirmedCommand = value; OnPropertyChanged(); } }

        #endregion

        #endregion

        public StartOptionsWindowViewModel()
        {
            LogChoices = new ObservableCollection<string> { OwnFileSelected, HospitalLog, BpiChallenge2015Mini, BpiChallenge2015 };
            AlphabetSize = "";
            LogChosenConfirmedCommand = new ButtonActionCommand(LogChosenConfirmed);
            AlphabetSizeChosenConfirmedCommand = new ButtonActionCommand(AlphabetSizeChosenConfirmed);
            DcrGraphChosenConfirmedCommand = new ButtonActionCommand(DcrGraphChosenConfirmed);
        }


        #region Event handling methods

        private void LogChosenConfirmed()
        {
            switch (LogChosen)
            {
                case OwnFileSelected:
                    var filePath = BrowseLog();
                    if (filePath != null)
                    {
                        var fileContents = File.ReadAllText(filePath);
                        try
                        {
                            IsWaiting = true;
                            var log =
                                XmlParser.ParseLog(
                                new LogStandard("", "trace",
                                    new LogStandardEntry(DataType.String, "id"), "event",
                                    new LogStandardEntry(DataType.String, "id"),
                                    new LogStandardEntry(DataType.String, "name"),
                                    new LogStandardEntry(DataType.String, "roleName")), fileContents); // TODO: Verify role name identifier correspondence
                            IsWaiting = false;
                            // Fire event
                            LogLoaded?.Invoke(log);
                            // Close view
                            OnClosingRequest();
                        }
                        catch
                        {
                            MessageBox.Show("Parsing of the file failed.\n\nPlease ensure that the file chosen was previously\ngenerated by the UlrikHøvsgaard Algorithm program.");
                        }
                    }
                    break;
                case HospitalLog:
                    try
                    {
                        var res = MessageBox.Show("Are you sure, that you wish to parse this log?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (res == DialogResult.No)
                        {
                            return;
                        }
                        IsWaiting = true;
                        var log =
                        XmlParser.ParseLog(
                                new LogStandard("http://www.xes-standard.org/", "trace",
                                    new LogStandardEntry(DataType.String, "conceptName"), "event",
                                    new LogStandardEntry(DataType.String, "ActivityCode"),
                                    new LogStandardEntry(DataType.String, "conceptName"),
                                    new LogStandardEntry(DataType.String, "org:group")), Resources.Hospital_log);
                        IsWaiting = false;
                        //TODO: choose the max size of traces, or at least tell the user that we filter.

                        log = log.FilterByNoOfActivities(8);
                        
                        var actors = log.Traces.SelectMany(trace => trace.Events.Select(a => a.ActorName));

                        Dictionary<string, Log> departmentTracesAlphabeth = new Dictionary<string, Log>();

                        foreach (var actor in log.Traces.SelectMany(trace => trace.Events.Select(a => a.ActorName)))
                        {

                            departmentTracesAlphabeth.Add(actor, log.FilterByActor(actor));

                        } 
                        

                            //TODO: let the user select the Department/actor
                        log = log.FilterByActor("Nursing ward");

                        // Fire event
                        LogLoaded?.Invoke(log);
                        // Close view
                        OnClosingRequest();
                    }
                    catch
                    {
                        MessageBox.Show("An error occured when trying to parse the log", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
                case BpiChallenge2015Mini:
                    try
                    {
                        var res = MessageBox.Show("Are you sure, that you wish to parse this log?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (res == DialogResult.No)
                        {
                            return;
                        }
                        IsWaiting = true;
                        var log =
                            XmlParser.ParseLog(
                                new LogStandard("http://www.xes-standard.org/", "trace",
                                    new LogStandardEntry(DataType.String, "conceptName"), "event",
                                    new LogStandardEntry(DataType.String, "conceptName"),
                                    new LogStandardEntry(DataType.String, "activityNameEN"),
                                    new LogStandardEntry(DataType.String, "")), Resources.BPIC15_small);
                        IsWaiting = false;


                        // Fire event
                        LogLoaded?.Invoke(log);
                        // Close view
                        OnClosingRequest();
                    }
                    catch
                    {
                        MessageBox.Show("An error occured when trying to parse the log", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
                case BpiChallenge2015:
                    try
                    {
                        var res = MessageBox.Show("Are you sure, that you wish to parse this log?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (res == DialogResult.No)
                        {
                            return;
                        }
                        IsWaiting = true;
                        var log =
                            XmlParser.ParseLog(
                                new LogStandard("http://www.xes-standard.org/", "trace",
                                    new LogStandardEntry(DataType.String, "conceptName"), "event",
                                    new LogStandardEntry(DataType.String, "conceptName"),
                                    new LogStandardEntry(DataType.String, "activityNameEN"),
                                    new LogStandardEntry(DataType.String, "")), Resources.BPIC15_1_xes);
                        IsWaiting = false;
                  

                        log.Traces = new List<LogTrace>(log.Traces.Where(t => t.Events.Distinct().Count() < 8));


                        // Fire event
                        LogLoaded?.Invoke(log);
                        // Close view
                        OnClosingRequest();
                    }
                    catch
                    {
                        MessageBox.Show("An error occured when trying to parse the log", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;
                default:
                    MessageBox.Show("Unexpected choice occured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void AlphabetSizeChosenConfirmed()
        {
            if (string.IsNullOrEmpty(AlphabetSize))
                MessageBox.Show("Please enter an integer value.");
            int amount;
            if (int.TryParse(AlphabetSize, out amount))
            {
                if (amount < 0 || amount > 26) // A-Z
                {
                    MessageBox.Show("Please enter a value in the interval [0-26].", "Whoops!");
                    return;
                }
                // Fire event
                AlphabetSizeSelected?.Invoke(amount);
                // Close view
                OnClosingRequest();
            }
            else
            {
                MessageBox.Show("Value is not an integer. Please enter a valid integer value.", "Error");
            }
        }

        private void DcrGraphChosenConfirmed()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a graph XML-file";
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK) // They selected a file
            {
                var filePath = dialog.FileName;
                var xml = File.ReadAllText(filePath);

                try
                {
                    IsWaiting = true;
                    var graphFromXml = XmlParser.ParseDcrGraph(xml); // Throws exception if failure
                    IsWaiting = false;

                    // Fire event
                    DcrGraphLoaded?.Invoke(graphFromXml);
                    // Close view
                    OnClosingRequest();
                }
                catch
                {
                    MessageBox.Show("Could not parse DCR-graph.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        private string BrowseLog()
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a log file";
            dialog.Filter = "XML files (*.xml)|*.xml";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        public static void ProcessUITasks()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate (object parameter) {
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
