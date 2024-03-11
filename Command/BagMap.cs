using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace cityjsonToRevit.Command
{
    public partial class BagMap : Form
    {
        public double side = -1;
        public BagMap(double lat, double lon)
        {
            InitializeComponent(lat, lon);
        }

        private void sliderMove_ValueChanged(object sender, EventArgs e)
        {
            value.Text = "Side: " + sliderMove.Value.ToString() + " meters";
            polygons.Polygons.Clear();
            List<PointLatLng> points = Square(gMap.Position.Lat, gMap.Position.Lng);
            GMapPolygon polygon = new GMapPolygon(points, "sqr");
            polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Red));
            polygon.Stroke = new Pen(Color.Red, 1);
            polygons.Polygons.Add(polygon);
        }
        private List<PointLatLng> Square(double lat, double lon)
        {
            double a = sliderMove.Value / 2;
            List<PointLatLng> points = new List<PointLatLng>();
            double[] xy = { lon, lat };
            Program.PointProjectorRev(7415, xy);
            double xmax = xy[0] + a;
            double ymax = xy[1] + a;
            double xmin = xy[0] - a;
            double ymin = xy[1] - a;
            double[] max = { xmax, ymax };
            Program.PointProjector(7415, max);
            double[] min = { xmin, ymin };
            Program.PointProjector(7415, min);
            PointLatLng p1 = new PointLatLng(max[1], max[0]);
            PointLatLng p2 = new PointLatLng(min[1], max[0]);
            PointLatLng p3 = new PointLatLng(min[1], min[0]);
            PointLatLng p4 = new PointLatLng(max[1], min[0]);
            points.Add(p1);
            points.Add(p2);
            points.Add(p3);
            points.Add(p4);
            return points;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            side = sliderMove.Value / 2;
            this.Close();

        }
    }
}
