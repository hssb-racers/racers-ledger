using System;
using System.Collections.Generic;
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
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using RACErsLedger;

namespace RACErsCompanion
{
    /// <summary>
    /// Interaction logic for SalvageGraph.xaml
    /// </summary>
    public partial class SalvageGraph : UserControl
    {
        public SalvageGraph()
        {
            InitializeComponent();

            var seriesConfig = Mappers.Xy<SalvageEntryData>().X(e => e.GameTime).Y(e => e.CumulativeValue);

            SeriesCollection = new SeriesCollection(seriesConfig)
            {

            };

            Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May" };
            YFormatter = value => value.ToString("C");

            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }
    }
}
