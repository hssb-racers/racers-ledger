using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using CsvHelper;
using RACErsLedger;

namespace RACErsCompanion
{

    public struct SalvageEntryData
    {
        public double CumulativeValue;
        public double GameTime;

        public SalvageEntryData(double cumulativeValue, double gameTime)
        {
            CumulativeValue = cumulativeValue;
            GameTime = gameTime;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                CheckPathExists = true,
                Filter = "Ledger files (*.csv)|*.csv"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                using(var reader = new StreamReader(openFileDialog.FileName))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<ShiftSalvageLogEntryMap>();
                    var records = csv.GetRecords<ShiftSalvageLogEntry>();
                    var entryLog = records.ToList();
                    var salvaged = entryLog.FindAll(entry => !entry.Destroyed);
                    var destroyed = entryLog.FindAll(entry => entry.Destroyed);

                    Graphhhh.SeriesCollection.Clear();
                    // TODO(sariya): LineSeries is probably not the right thing to use here. We'd probably want a CartesianChart 
                    Graphhhh.SeriesCollection.Add(new LineSeries()
                    {
                        Title = "salvage",
                        Values = new ChartValues<SalvageEntryData>(ConvertToSalvageEntryData(salvaged))
                            // hacky running total display
 
                    });
                    Graphhhh.SeriesCollection.Add(new LineSeries()
                    {
                        Title = "destroyed",
                        Values = new ChartValues<SalvageEntryData>(ConvertToSalvageEntryData(destroyed))
                    });
                }
            }
        }
        static IEnumerable<SalvageEntryData> ConvertToSalvageEntryData(IList<ShiftSalvageLogEntry> sequence)
        {
            float cumulative = 0;
            return sequence.Select((item, i) =>
            {
                cumulative += item.Value;
                return new SalvageEntryData(cumulative, item.GameTime);
            });
        }
    }

}
