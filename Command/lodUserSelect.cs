using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace cityjsonToRevit
{
    public partial class lodUserSelect : Form
    {
        public String _level = "";
        public lodUserSelect(List<string> lods)
        {
            List<string> levels = lods;
            InitializeComponent(levels);
        }
        public string GetLevel(string name)
        {
            _level = name;
            return _level;
        }
        private void setBtn_Click(object sender, EventArgs e)
        {
            string selected = comboBox1.GetItemText(comboBox1.SelectedItem);
            GetLevel(selected);
            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                setBtn_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
