using System;
using System.Windows.Forms;

namespace cityjsonToRevit
{
    public partial class mapViewer : Form
    {
        public bool _loc = false;
        public bool _cancel = true;

        public mapViewer(double lat0, double lon0, double lat1, double lon1)
        {
            InitializeComponent(lat0, lon0, lat1, lon1);
        }



        private void button1_Click(object sender, EventArgs e)
        {
            _cancel = false;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _cancel = false;
            _loc = true;
            this.Close();
        }
    }
}
