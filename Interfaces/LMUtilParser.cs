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
using System.Text.RegularExpressions;
using LicenseManager;

namespace LMUtil
{
    public class LMUtilLicenseParser : LicenseParser
    {
        private LMUtilInterface LicSrv;
        //public Dictionary<string, License> licenseInfo = new Dictionary<string, License>();
        private readonly Regex licenseNameMatcher = new Regex(@"^Users of [0-9]{5}(\w+)(_[0-9]+_0F)*:.*");
        private readonly Regex licenseNumberMatcher = new Regex(@".* ([0-9]+) licenses issued.*");
        private readonly Regex userLineMatcher = new Regex(@"\s+([A-Za-z0-9- ]+) ([A-Za-z0-9-]+) ([A-Za-z0-9-]+) \(.*\), start (.*)");

        public LMUtilLicenseParser(string SrvAddress = "")
        {
            LicSrv = new LMUtilInterface(SrvAddress);
        }

        /* ParseLicenses
         * 
         * Function used as the main license parser. Updates the instance
         * dictionary licenseInfo which contains all the necessary license
         * information for parsing by the main UI.
         */
        public override void ParseLicenses() {
            int lvl;

            LicSrv.QueryServer();

            // Wait until licensing information finishes loading. Recheck every 250ms
            //while (LicSrv.isLoading)
            //    Thread.Sleep(250);

            // Start parsing the licensing output line by line.
            using (StringReader reader = new StringReader(LicSrv.SrvOutput))
            {
                string line;
                string licenseName = string.Empty;
                string userName = string.Empty;
                // string[] tmpline;
                // int id = 0;
                int noLicenses = 0;
                DateTime licenseExpiry;
                // bool licenseLines = false;
                CultureInfo dateProvider = new CultureInfo("en-GB");
                Match match;

                // Clear entire Dictionary<> before repopulating it (in case of license removals)
                licenseInfo.Clear();

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("Cannot connect to license server")) {
                        throw new System.Net.WebException("Failed to resolve the server host.");
                    }

                    lvl = Regex.Match(line, @"^\s+").Length / 2;


                    if (lvl == 0 && line.IndexOf("Users of") != -1) {
                        licenseName = licenseNameMatcher.Replace(line, "$1");
                        licenseName = Regex.Replace(licenseName, @"_[0-9]{4}_0F", "");

                        if (AutodeskProducts.ContainsKey(licenseName)) {
                            licenseName = AutodeskProducts[licenseName];
                        }

                        noLicenses = int.Parse(licenseNumberMatcher.Replace(line, "$1"));

                        if (!licenseInfo.ContainsKey(licenseName)) {
                            licenseInfo.Add(licenseName, new License(licenseName, noLicenses));
                        }
                    }
                    else if (lvl == 2 && licenseInfo.ContainsKey(licenseName)) {
                        // User details
                        match = userLineMatcher.Match(line);
                        userName = match.Groups[1].Value;


                        string timestamp = match.Groups[4].Value;
                        int month = Int32.Parse(timestamp.Split(' ')[1].Split('/')[0]);

                        if (month > DateTime.Now.Month)
                        {
                            timestamp += " " + (DateTime.Now.Year - 1);
                        }
                        else
                        {
                            timestamp += " " + DateTime.Now.Year.ToString();
                        }

                        licenseExpiry = DateTime.ParseExact(timestamp, "ddd M/d H:mm yyyy", dateProvider);

                        if (userName != String.Empty && !licenseInfo[licenseName].users.ContainsKey(userName)) {
                            licenseInfo[licenseName].users.Add(userName, new LicenseUser(userName));
                            licenseInfo[licenseName].users[userName].dateOpened = licenseExpiry;
                        }

                        userName = String.Empty;
                    }
                }
            }
        }
        
        private readonly Dictionary<string, string> AutodeskProducts = new Dictionary<string, string>() {
            { "AECCOL_T_F", "AEC Collection" },
            { "ARNOL", "Arnold"},
            { "3DSMAX", "3DS Max"},
            { "3DSMXS", "3DS Max with Softimage"},
            { "MXECS", "3DS Max Entertainment Creation Suite"},
            { "MAXIO", "3DS Max IO"},
            { "ADSTPR", "Advance Steel"},
            { "ALAUST", "Alias AutoStudio"},
            { "ALSCPT", "Alias Concept"},
            { "DESNST", "Alias Design"},
            { "SURFST", "Alias Surface"},
            { "ACD", "AutoCAD"},
            { "ACDMAC", "AutoCAD for Mac"},
            { "ARCHDESK", "AutoCAD Architecture"},
            { "DSPRM", "AutoCAD Design Suite Premium"},
            { "DSSTD", "AutoCAD Design Suite Standard"},
            { "ACAD_E", "AutoCAD Electrical"},
            { "INVLTS", "AutoCAD Inventor LT Suite"},
            { "ACDLT", "AutoCAD LT"},
            { "ACDLTM", "AutoCAD LT for Mac"},
            { "ACDLTC", "AutoCAD LT Civil Suite"},
            { "MAP", "AutoCAD Map 3D"},
            { "AMECH_PP", "AutoCAD Mechanical"},
            { "BLDSYS", "AutoCAD MEP"},
            { "PLNT3D", "AutoCAD Plant 3D"},
            { "ARDES", "AutoCAD Raster Design"},
            { "RVTLTS", "AutoCAD Revit LT Suite"},
            { "BDSPRM", "Building Design Suite Premium"},
            { "BDSS", "Building Design Suite Standard"},
            { "BDSADV", "Building Design Suite Ultimate"},
            { "C300", "Burn"},
            { "SCDSE", "CFD Design Study Environment"},
            { "SCFDA", "CFD Premium"},
            { "SCFDM", "CFD Ultimate"},
            { "CIV3D", "Civil 3D"},
            { "ENCSU", "Entertainment Creation Suite Ultimate"},
            { "CADMEP", "Fabrication CADmep"},
            { "CAMDCT", "Fabrication CAMduct"},
            { "ESTMEP", "Fabrication ESTmep"},
            { "FDSPRM", "Factory Design Suite Premium"},
            { "FDSS", "Factory Design Suite Standard"},
            { "FDSADV", "Factory Design Suite Ultimate"},
            { "FDU", "Factory Design Utilities"},
            { "FCAMP", "FeatureCAM Premium"},
            { "FCAMS", "FeatureCAM Standard"},
            { "FCAMU", "FeatureCAM Ultimate"},
            { "A250", "Flame"},
            { "FLMEDU", "Flame - Education"},
            { "A350", "Flame Assist"},
            { "A280", "Flame Premium"},
            { "A400", "Flare"},
            { "ACMPAN", "Helius PFA"},
            { "HSMPREM", "HSM Premium"},
            { "HSMULT", "HSM Ultimate"},
            { "IDSP", "Infrastructure Design Suite Premium"},
            { "IDSS", "Infrastructure Design Suite Standard"},
            { "IDSU", "Infrastructure Design Suite Ultimate"},
            { "IW360P", "InfraWorks"},
            { "INVNTOR", "Inventor"},
            { "INVHSM", "Inventor CAM Premium"},
            { "INHSMP", "Inventor CAM Ultimate"},
            { "IETODEV", "Inventor Engineer-to-Order - Developer"},
            { "INVETO", "Inventor Engineer-to-Order Series"},
            { "INTSER", "Inventor Engineer-to-Order Server"},
            { "INVLT", "Inventor LT"},
            { "NINCAD", "Inventor Nastran"},
            { "TNNUINV", "Inventor Nesting"},
            { "INVOEM", "Inventor OEM"},
            { "INVPROSA", "Inventor Professional"},
            { "INVTOL", "Inventor Tolerance Analysis"},
            { "K100", "Lustre"},
            { "LSTRBRN", "Lustre Burn"},
            { "LSTRSR", "Lustre ShotReactor"},
            { "MPPUS", "Manufacturing Automation Utility"},
            { "MFGDEXUTLP", "Manufacturing Data Exchange Utility Premium"},
            { "MAYA", "Maya"},
            { "MAYAS", "Maya with Softimage"},
            { "MYECS", "Maya Entertainment Creation Suite Standard"},
            { "MAYAIO", "Maya IO"},
            { "MAYALT", "Maya LT"},
            { "MEPCS", "MEP Fabrication Suite"},
            { "MFAM", "Moldflow Adviser Premium"},
            { "MFAA", "Moldflow Adviser Ultimate"},
            { "MFIP", "Moldflow Insight Premium"},
            { "MFIB", "Moldflow Insight Standard"},
            { "MFIA", "Moldflow Insight Ultimate"},
            { "MFIAT", "Moldflow Insight Ultimate TFLEX"},
            { "MFS", "Moldflow Synergy"},
            { "MFST", "Moldflow Synergy TFLEX"},
            { "MOBPRO", "MotionBuilder"},
            { "MBXPRO", "Mudbox"},
            { "NAVMAN", "Navisworks Manage"},
            { "NAVSIM", "Navisworks Simulate"},
            { "NTFABLS", "Netfabb Local Simulation"},
            { "NETFA", "Netfabb Premium"},
            { "NETFS", "Netfabb Standard"},
            { "NETFE", "Netfabb Ultimate"},
            { "PMAKER", "PartMaker"},
            { "PDSPRM", "Plant Design Suite Premium"},
            { "PLTDSS", "Plant Design Suite Standard"},
            { "PDSADV", "Plant Design Suite Ultimate"},
            { "PNTLAY", "Point Layout"},
            { "PWRIP", "PowerInspect Premium"},
            { "PWRIS", "PowerInspect Standard"},
            { "PWRIU", "PowerInspect Ultimate"},
            { "PWRMMOD", "PowerMill Modeling"},
            { "PWRMP", "PowerMill Premium"},
            { "PWRMS", "PowerMill Standard"},
            { "PWRMU", "PowerMill Ultimate"},
            { "PWRSP", "PowerShape Premium"},
            { "PWRSS", "PowerShape Standard"},
            { "PWRSU", "PowerShape Ultimate"},
            { "PDSP", "Product Design Suite Premium"},
            { "PDSU", "Product Design Suite Ultimate"},
            { "RECAP", "ReCap Pro"},
            { "RVT", "Revit"},
            { "RVTLT", "Revit LT"},
            { "RSAPRO", "Robot Structural Analysis"},
            { "SBPNL", "SketchBook Pro"},
            { "SBRDES", "Structural Bridge Design"},
            { "SFS", "Structural Fabrication Suite"},
            { "TRCMPS", "TruComposites Standard"},
            { "TRCMPU", "TruComposites Ultimate"},
            { "TRUNEST", "TruNest"},
            { "TRUNST", "TruNest - Nesting Engine"},
            { "PCOFFI", "Vault Office"},
            { "VLTM", "Vault Professional"},
            { "VLTWG", "Vault Workgroup"},
            { "VEHTRK", "Vehicle Tracking"},
            { "VRDDES", "VRED Design"},
            { "VRDPRS", "VRED Presenter"},
            { "VRDPRO", "VRED Professional"},
            { "RCMVRD", "VRED Render Node"},
            { "VRDSRV", "VRED Server"},
            { "WTGTWY", "Wiretap Gateway"},
            { "EMFEB", "Ent Multi-Flex Enhanced Bndl"},
            { "VLTEAD", "Enterprise Add-On for Vault"},
            { "FCMPLH", "FeatureCAM Premium"},
            { "FCMSLH", "FeatureCAM Standard"},
            { "FCMULH", "FeatureCAM Ultimate"},
            { "HSMPRO", "HSMWorks Premium"},
            { "HSMPRM", "HSMWorks Ultimate"},
            { "PWIPLH", "PowerInspect Premium"},
            { "PWISLH", "PowerInspect Standard"},
            { "PWIULH", "PowerInspect Ultimate"},
            { "PMPLHD", "PowerMill Premium"},
            { "PMSLHD", "PowerMill Standard"},
            { "PMULHD", "PowerMill Ultimate"},
            { "PWSPLH", "PowerShape Premium"},
            { "PWSSLH", "PowerShape Standard"},
            { "PWSULH", "PowerShape Ultimate"},
            { "RRI2ADD", "Revit - with RIB iTWO add-on"},
            { "T1MF", "T1 Enterprise Multi-flex"},
            { "T1MFPV", "T1 Enterprise Multi-flex Prior Version"},
            { "T1MFSB", "T1 Enterprise Multi-flex Standard Bundle"},
            { "T1MFPB", "T1 Enterprise Multi-Flex Standard Prior Version Bundle"}
        };
    }
}
