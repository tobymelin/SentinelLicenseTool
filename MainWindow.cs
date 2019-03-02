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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LicenseParser;

namespace LicenseManager
{
    public partial class MainWindow : Form
    {
        public SentinelParser software;
        public string SrvAddress = "DC01";

        List<string> noLicenses = new List<string> { "No licenses found." };
        List<string> emptyList = new List<string> { };
        string keySequence = string.Empty;
        DateTime lastRefreshed;

        AboutDialog dlg_about = new AboutDialog();
        ServerDialog dlg_srvconfig = new ServerDialog();

        Thread licNotifierThread;
        bool threadRunning = false;
        string checkLicense;

        private readonly string[] softwareFilter = { "Safe", "EtabNL", "EtabPL", "SAPPL", "SAP", "T.TD.User", "T.SD.Design.U", "CSC.FT.CON.All", "CSIxR" };

        public MainWindow()
        {
            InitializeComponent();

            // Automatically generate the window title based on project properties
            this.Text = this.ProductName + " v" + this.ProductVersion;

            dlg_about.Owner = this;
            
            this.Load += new System.EventHandler(Form1_Load);
            
            this.KeyPress += new KeyPressEventHandler(Konami);
            softwareListBox.KeyPress += new KeyPressEventHandler(Konami);
            userListBox.KeyPress += new KeyPressEventHandler(Konami);

            // TODO: Implement isolated storage handler to provide
            //       SentinelParser with a user-specified server address.
        }

        /* InitConnection()
         * 
         * Define a separate method to handle list updates
         * after initialisation. Allows for catching any propagated errors
         * due to missing lsmon.exe.
         */
        private void InitConnection()
        {
            try
            {
                if (SrvAddress != "")
                    software = new SentinelParser(SrvAddress);
                else
                    software = new SentinelParser();

                RefreshSoftwareList();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("ERROR: Could not find lsmon.exe. Please ensure this executable is in " +
                    "the same folder as the main program. License monitor will now exit.",
                    "lsmon.exe not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            catch (System.Net.WebException)
            {
                MessageBox.Show("ERROR: Could not reach destination server. Please ensure the computer " +
                    "has a working internet connection and the license server is online",
                    "License server unavailable", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RefreshSoftwareList()
        {
            if (software == null)
            {
                InitConnection();
                return;
            }

            try
            {
                software.ParseLicenses();
            }
            catch (System.Net.WebException)
            {
                MessageBox.Show("ERROR: Could not access the license server. Please try again.",
                    "Server not available", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            lastRefreshed = DateTime.Now;
            refreshedLabel.Text = "Last updated: " + lastRefreshed.ToShortTimeString();
            UpdateLists(true);
        }

        /* UpdateLists
         * @refreshed : Boolean indicating whether the software list should be
         *      updated in addition to the user list.
         * 
         * Used to update the listboxes in the UI.
         */
        public void UpdateLists(bool refreshed = false)
        {
            if (software.licenseInfo.Count == 0)
            {
                softwareListBox.DataSource = noLicenses;
                userListBox.DataSource = emptyList;
                licenseLabel.Text = "No licenses.";
            }
            else
            {
                /* If the licensing information has just been refreshed, update
                 * the software listbox as well. Uses a LINQ expression to filter
                 * only the most commonly used software unless the 'Show All...'
                 * menu option has been checked.
                 * 
                 * Attempts to maintain the previously selected item if possible.
                 */
                if (refreshed)
                {
                    var swList = software.licenseInfo.Keys.ToList();
                    var prevSelection = softwareListBox.SelectedValue;

                    if (showAllLicensesToolStripMenuItem.Checked)
                        softwareListBox.DataSource = swList;
                    else
                        softwareListBox.DataSource = swList.Where(x => softwareFilter.Any(y => x.Contains(y))).ToList();

                    if (prevSelection != null)
                    {
                        var selectionIdx = softwareListBox.Items.IndexOf(prevSelection.ToString());
                        if (selectionIdx != -1)
                            softwareListBox.SetSelected(selectionIdx, true);
                    }
                }

                userListBox.DataSource = software.LicensesInUse(softwareListBox.Text);

                licenseLabel.Text = software.licenseInfo[softwareListBox.Text].users.Count.ToString();
                licenseLabel.Text += " / " + software.licenseInfo[softwareListBox.Text].licensesAvailable;
                licenseLabel.Text += " licenses in use.";
            }
        }

        /* RefreshButton_click
         * 
         * Button click event handler which manages the refresh process. 
         * Refreshes are limited to once every minute.
         */
        private void RefreshButton_click(object sender, EventArgs e)
        {
            if (DateTime.Now.Subtract(lastRefreshed).Minutes < 1)
                return;

            RefreshSoftwareList();
        }

        /* listBox1_SelectedIndexChanged()
         * 
         * Update listboxes in the UI whenever selection changes.
         */
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLists();
        }

        /* Konami()
         * 
         * KeyPressEvent handler used to initiate a thread which
         * checks for available licenses. May become an openly available
         * feature in the long term.
         */
        void Konami(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Back)
            {
                keySequence = string.Empty;

                if (licNotifierThread != null && licNotifierThread.IsAlive)
                {
                    licNotifierThread.Abort();
                    threadRunning = false;
                    refreshButton.Enabled = true;
                }

                return;
            }

            if (e.KeyChar == (char)Keys.Escape)
                Close();

            if (e.KeyChar >= (int)'A' && e.KeyChar <= (int)'z')
                keySequence += e.KeyChar;

            if (keySequence.ToUpper() == "UUDDLRLRBA")
            {
                // Reset keySequence automatically after succesful input
                keySequence = string.Empty;

                if (software.licenseInfo[softwareListBox.Text].licensesAvailable > software.licenseInfo[softwareListBox.Text].users.Count)
                    return;

                if (licNotifierThread == null || !licNotifierThread.IsAlive)
                {
                    licNotifierThread = new Thread(new ThreadStart(CheckLicense));
                    licNotifierThread.IsBackground = true;
                    threadRunning = true;
                }
                else
                    return;

                checkLicense = softwareListBox.Text;
                licNotifierThread.Start();

                if (licNotifierThread != null)
                {
                    refreshButton.Enabled = false;
                    MessageBox.Show("Waiting for license: " + checkLicense);
                }
            }
        }

        /* CheckLicense()
         * 
         * Thread which runs after Konami() has been successfully invoked.
         * Continuously checks for an available license every minute and
         * notifies the user once one is available.
         */
        private void CheckLicense()
        {
            if (!software.licenseInfo.ContainsKey(checkLicense))
                return;

            while (threadRunning)
            {
                if (software.licenseInfo[checkLicense].licensesAvailable > software.licenseInfo[checkLicense].users.Count)
                {
                    MessageBox.Show("License available!");
                    break;
                }
                Thread.Sleep(TimeSpan.FromMinutes(60));

                Invoke(new Action(() => RefreshSoftwareList()));
            }

            Invoke(new Action(() => { refreshButton.Enabled = true; }));
            threadRunning = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitConnection();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dlg_about.ShowDialog();
        }

        // When the 'Show All Licenses' menu item is clicked,
        // update the lists in the UI to reflect the altered setting.
        private void showAllLicensesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateLists(true);
        }

        private void changeServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dlg_srvconfig.srvTextBox.Text = SrvAddress;

            if (dlg_srvconfig.ShowDialog() == DialogResult.OK)
            {
                if (dlg_srvconfig.srvTextBox.Text != "") {
                    SrvAddress = dlg_srvconfig.srvTextBox.Text;
                    InitConnection();
                }
            }
        }
    }
}
