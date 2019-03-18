using System;
using System.Windows.Forms;

namespace LicenseManager
{
    public partial class ServerDialog : Form
    {
        public ServerDialog()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
