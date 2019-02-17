/* Sentinel License Query Tool
 * Copyright (C) 2018-2019  Tobias Melin
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LicenseManager
{
    public partial class AboutDialog : Form
    {
        public AboutDialog()
        {
            InitializeComponent();

            label1.Text = this.ProductName + " v" + ProductVersion;
            label1.Text += "\n\n© Tobias Melin, 2018 - 2019";

            LinkLabel.Link githubLink = new LinkLabel.Link();
            githubLink.LinkData = "https://github.com/tobymelin";
            gitLinkLabel.Links.Add(githubLink);
            gitLinkLabel.Text = githubLink.LinkData as string;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void gitLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData as string);
        }
    }
}
