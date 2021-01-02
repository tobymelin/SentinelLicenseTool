using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LicenseManager
{
    public class LicenseParser
    {
        //private SentinelInterface LicSrv;
        //private readonly string[] software = { "Safe", "EtabNL", "EtabPL", "SAPPL", "SAP", "T.TD.User", "T.SD.Design.U", "CSC.FT.CON.User", "CSIxR" };
        public Dictionary<string, License> licenseInfo = new Dictionary<string, License>();

        public virtual void ParseLicenses() {
            return;
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

        public License(string SoftwareName, int LicensesAvailable)
        {
            this.softwareName = SoftwareName;
            this.licensesAvailable = LicensesAvailable;
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
