using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cityjsonToRevit.Command
{
    public partial class BasePoints : Form
    {
        public bool _sp = false;
        public bool _pp = false;

        public BasePoints()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _sp = checkBox1.Checked;
            _pp = checkBox2.Checked;
            this.Close();
        }
    }
}
