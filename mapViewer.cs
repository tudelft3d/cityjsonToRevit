using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cityjsonToRevit
{
    public partial class mapViewer : Form
    {
        public bool _loc = false;
        public mapViewer(double lat0, double lon0, double lat1, double lon1)
        {
            InitializeComponent(lat0, lon0, lat1, lon1);
        }

        public bool NewLoc(bool loca)
        {
            _loc = loca;
            return _loc;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool loc = false;
            NewLoc(loc);
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            bool loc = true;
            NewLoc(loc);
            this.Close();
        }
    }
}
