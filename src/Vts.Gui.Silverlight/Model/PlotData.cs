using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Vts.SiteVisit.Model
{
    /// <summary>
    /// Class to hold all data necessary for describing a single line plot
    /// Needs to be bolstered to allow for multiple descriptors, powerful enough
    /// to support a view of a particular data set
    /// </summary>
    public class PlotData
    {
        public PlotData() 
            : this(null, null)
        {
        }

        public PlotData(IEnumerable<Point> points, string title)
            : this(points, title, Colors.Blue)
        {
        }

        public PlotData(IEnumerable<Point> points, string title, Color c)
        {
            Points = points.ToList();
            Title = title;
            plotColor = c;
        }

        public IList<Point> Points { get; set; }
        public string Title { get; set; }
        public Color plotColor { get; set; }

    }
}