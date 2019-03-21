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
using System.IO;
using System.Text;

namespace SentinelInterface
{
    /* SrvReader : manages the interface to the RMS License Server
     *      Allows for fallbacks to use a cached file for troubleshooting
     *      and development purposes.
     */
    public class SentinelInterface
    {
        public string SrvOutput;
        public bool isLoading = false;
        public bool FileOverride = false;
        public string SrvAddress = string.Empty;
        private ProcessStartInfo lsmon;

        public SentinelInterface()
        {
            if (File.Exists("lsmon.txt"))
            {
                this.FileOverride = true;
                this.SrvOutput = File.ReadAllText("lsmon.txt");
                Console.WriteLine("lsmon.txt override active");
            }
            else
            {
                this.FileOverride = false;
                this.SrvAddress = "DC01";
            }
        }

        public SentinelInterface(string SrvAddress)
        {
            if (File.Exists("lsmon.txt"))
            {
                this.FileOverride = true;
                this.SrvOutput = File.ReadAllText("lsmon.txt");
                Console.WriteLine("lsmon.txt override active");
            }
            else
            {
                this.FileOverride = false;
                this.SrvAddress = SrvAddress;
                ProcessSetup();
            }
        }

        private void ProcessSetup()
        {
            lsmon = new ProcessStartInfo
            {
                FileName = "lsmon.exe",
                Arguments = SrvAddress,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
        }

        /* QueryServer
         * 
         * Queries the license server for the latest information.
         * Stores the results in the SrvOutput instance variable.
         */
        public void QueryServer()
        {
            if (FileOverride)
                return;

            StringBuilder output = new StringBuilder();

            try
            {
                using (Process process = Process.Start(lsmon))
                {
                    isLoading = true;

                    // Create a new EventHandler which appends any output received from
                    // lsmon to output, which is then used to update SrvOutput when the
                    // license query finishes. 
                    process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        if (!String.IsNullOrEmpty(e.Data))
                        {
                            // Check for end of output, otherwise append output to StringBuilder
                            if (e.Data.ToLower().Contains("press enter to continue"))
                            {
                                process.CancelOutputRead();
                                process.StandardInput.WriteLine();
                            } else
                            {
                                output.Append("\n" + e.Data);
                            }
                        }
                    });

                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    isLoading = false;

                    SrvOutput = output.ToString();

                    if (SrvOutput.Contains("Error[3]"))
                        throw new System.Net.WebException("Failed to resolve the server host.");
                    if (SrvOutput.Contains("Error[5]"))
                        throw new System.Net.WebException("Timed out while attempting to reach the license server.");

                    Console.WriteLine("Finished reading license information.");
                    //File.WriteAllText("lsmon-latest.txt", SrvOutput);
                }
            }
            // Catch errors related to missing lsmon.exe or lsmon.dll
            // Propagate the error upwards for handling by the application
            catch (System.ComponentModel.Win32Exception e)
            {
                SrvOutput = "";
                throw new System.ComponentModel.Win32Exception("Error", e);
            }
        }
    }
}
