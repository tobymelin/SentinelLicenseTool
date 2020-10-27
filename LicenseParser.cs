/* Sentinel License Query Tool
 * Copyright (C) 2018-2020  Tobias Melin
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
using System.Globalization;
using System.IO;

namespace LicenseParser
{
    public class LicenseParser
    {
        private SentinelInterface.SentinelInterface LicSrv;
        //private readonly string[] software = { "Safe", "EtabNL", "EtabPL", "SAPPL", "SAP", "T.TD.User", "T.SD.Design.U", "CSC.FT.CON.User", "CSIxR" };
        public Dictionary<string, License> licenseInfo = new Dictionary<string, License>();

        public LicenseParser() {
            LicSrv = new SentinelInterface.SentinelInterface("");
        }

        public LicenseParser(string SrvAddress)
        {
            LicSrv = new SentinelInterface.SentinelInterface(SrvAddress);
        }

        /* ParseLicenses
         * 
         * Function used as the main license parser. Updates the instance
         * dictionary licenseInfo which contains all the necessary license
         * information for parsing by the main UI.
         */
        public void ParseLicenses() {
            int lvl;

            LicSrv.QueryServer();

            // Wait until licensing information finishes loading. Recheck every 250ms
            //while (LicSrv.isLoading)
            //    Thread.Sleep(250);

            // Start parsing the licensing output line by line.
            // TODO: Refactor to use a dictionary parser instead?
            using (StringReader reader = new StringReader(LicSrv.SrvOutput))
            {
                string line;
                string licenseName = string.Empty;
                string userName = string.Empty;
                string[] tmpline;
                int id = 0;
                int noLicenses = 0;
                DateTime licenseExpiry;
                bool licenseLines = false;
                CultureInfo dateProvider = new CultureInfo("en-GB");

                // Clear entire Dictionary<> before repopulating it (in case of license removals)
                licenseInfo.Clear();

                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.Contains("|-")) {
                        if (line.Contains("Failed to resolve the server host"))
                            throw new System.Net.WebException("Failed to resolve the server host.");

                        continue;
                    }

                    lvl = (line.IndexOf("|-") - 1) / 2;

                    if (line.IndexOf(':') == -1 && LineMatcher(line)[0] == "Feature Information")
                    {
                        id++;
                        noLicenses = 0;
                        licenseLines = false;

                        // Remove previous license entry if it turns out there are no unexpired licenses
                        if (licenseInfo.ContainsKey(licenseName) && licenseInfo[licenseName].licensesAvailable == 0)
                            licenseInfo.Remove(licenseName);
                    }
                    else if (line.IndexOf(':') == -1 && LineMatcher(line)[0] == "License Information")
                    {
                        licenseLines = true;
                    }
                    else
                    {
                        tmpline = LineMatcher(line);
                        if (tmpline[0] == "Feature name")
                            licenseName = tmpline[1];
                        else if (tmpline[0] == "Feature name")
                            licenseName = string.Empty;

                        if (licenseLines && lvl == 1)
                        {
                            licenseLines = false;
                            noLicenses = 0;
                        }

                        if (licenseName.Length > 0)
                        {
                            if (tmpline[0] == "Feature version" && !licenseInfo.ContainsKey(licenseName) && licenseName.Length > 0)
                            {
                                licenseName += " " + tmpline[1];
                                licenseInfo.Add(licenseName, new License(licenseName));
                            }
                            else if (tmpline[0] == "Maximum concurrent user(s)" && licenseLines)
                            {
                                noLicenses += int.Parse(tmpline[1]);
                            }
                            else if (tmpline[0] == "Expiration date" && licenseInfo.ContainsKey(licenseName) && licenseLines)
                            {
                                if (tmpline[1] != "License has no expiration")
                                {
                                    licenseExpiry = DateTime.ParseExact(tmpline[1].Trim(), "ddd MMM dd HH:mm:ss yyyy", dateProvider);

                                    if (DateTime.Today > licenseExpiry)
                                    {
                                        licenseLines = false;
                                        continue;
                                    }
                                }

                                licenseInfo[licenseName].licensesAvailable += noLicenses;
                                noLicenses = 0;
                            }
                            else if (tmpline[0] == "User name" && licenseInfo.ContainsKey(licenseName))
                            {
                                userName = tmpline[1];

                                if (!licenseInfo[licenseName].users.ContainsKey(userName))
                                    licenseInfo[licenseName].users.Add(userName, new LicenseUser(userName));
                                else
                                    licenseInfo[licenseName].users[userName].licensesInUse += 1;
                            }
                            else if (tmpline[0] == "Status" && licenseInfo.ContainsKey(licenseName) && licenseInfo[licenseName].users.ContainsKey(userName))
                            {
                                licenseInfo[licenseName].users[userName].dateOpened = DateTime.ParseExact(tmpline[1].Replace("Running since ", "").Trim(), "ddd MMM dd HH:mm:ss yyyy", dateProvider);
                            }
                        }
                    }
                }
            }
        }

        /* LineMatcher
         * @line : String representing a single licensing information line
         * 
         * Helper function used to parse individual lines in the licensing
         * information.
         * 
         * Returns: an array of strings. The first value will always be the 
         * field name, and the second value (if existing) is the field value.
         * Lines without a field value are categories, generally indicating
         * the start of a new license.
         */
        private string[] LineMatcher(string line)
        {
            string[] dictVals = line.Split(new[] { ':' }, 2);
            dictVals[0] = dictVals[0].Replace("|- ", string.Empty).Trim();

            if (dictVals.Length > 1)
                dictVals[1] = dictVals[1].Replace("\"", string.Empty).Trim();

            return dictVals;
        }

        /* LicensesInUse
         * @softwareName : string corresponding to a software license
         * 
         * Helper function used to parse information about users using a
         * specific piece of software.
         * 
         * Returns: a List<string> containing information in the format
         * "Username [hh:mm]", where the time in the brackets is the amount of
         * time since the user checked out the license from the server.
         */
        public List<string> LicensesInUse(string softwareName)
        {
            if (!licenseInfo.ContainsKey(softwareName) || licenseInfo[softwareName].users.Count == 0)
                return new List<string> { "No licenses in use." };

            List<string> userList = new List<string>();

            foreach (KeyValuePair<string, LicenseUser> userInfo in licenseInfo[softwareName].users)
            {
                var userDate = userInfo.Value.dateOpened;
                var timeDiff = DateTime.Now.Subtract(userDate);
                int minutesUsed = (int)timeDiff.TotalMinutes % 60;
                int hoursUsed = (int)timeDiff.TotalMinutes / 60;

                string timeString = "[";
                timeString += hoursUsed + "h ";
                timeString += minutesUsed + "m]";

                userList.Add(userInfo.Key + " " + timeString);
            }

            return userList;
        }
    }

    /* License
     * 
     * Class providing information for a specific license.
     * Currently limited to the license name, how many licenses are available,
     * and a dictionary of users currently using the software which in turn implements
     * the LicenseUser class (a List<LicenserUser> could be used, but
     * would add unnecessary code verbosity since a .Find function would
     * need to be used instead of .ContainsKey in LicenseParser())
     */
    public class License
    {
        public string softwareName;
        public int licensesAvailable = 0;
        public Dictionary<string, LicenseUser> users = new Dictionary<string, LicenseUser> { };

        public License(string SoftwareName)
        {
            this.softwareName = SoftwareName;
        }
    }

    /* LicenseUser
     * 
     * Class containing details about the user of a specific license.
     * Includes the UserName for easy reference even though this is 
     * technically handled by the dictionary key. 
     */
    public class LicenseUser
    {
        public string userName;
        public int licensesInUse = 1;
        public DateTime dateOpened;

        public LicenseUser(string UserName)
        {
            this.userName = UserName;
        }
    }
}
