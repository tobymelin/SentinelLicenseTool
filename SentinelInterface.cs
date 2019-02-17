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
using System.Threading;

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
                QueryServer();
                File.WriteAllText("lsmon-latest.txt", SrvOutput);
            }
        }

        public SentinelInterface(string SrvAddress)
        {
            this.FileOverride = false;
            this.SrvAddress = SrvAddress;
            QueryServer();
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

            ProcessStartInfo lsmon = new ProcessStartInfo();
            lsmon.FileName = "lsmon.exe";
            lsmon.Arguments = SrvAddress;
            lsmon.UseShellExecute = false;
            lsmon.CreateNoWindow = true;
            lsmon.RedirectStandardInput = true;
            lsmon.RedirectStandardOutput = true;

            try
            {
                using (Process process = Process.Start(lsmon))
                {
                    isLoading = true;
                    // Sleep for 250ms to allow the query to get passed to the server
                    // and return licensing information.
                    Thread.Sleep(250);
                    process.StandardInput.WriteLine();
                    isLoading = false;

                    using (StreamReader reader = process.StandardOutput)
                    {
                        SrvOutput = reader.ReadToEnd();
                    }

                    if (SrvOutput.Contains("Error[3]"))
                        throw new System.Net.WebException("Failed to resolve the server host.");

                    Console.WriteLine("Finished reading license information.");
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
