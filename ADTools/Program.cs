using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADTools
{
    class Program
    {
        /*
         * Command arguments
         * 
         * -L -> List Users
         * -LG -> List Groups
         * -DA -> List Domain Admins
         * -C -> Attempt Account Creation, format:  -C user=<>:pass=<>
         * -CG -> Attempt adding created account to specified group, form at:  -CG user=<>:pass=<>:group=<>
         * -UG -> Print User Groups
         * -GM -> List group members
         * -DC -> List DC's
         * -DS -> List all domain systems
         * -B -> Get User
         * -h > List Help/usage/flags/etc
         * -u user
         * -d domain
         * -p password
         * */
        static void Main(string[] args)
        {
            try
            {
                Arguments CommandLine = new Arguments(args);
                if (CommandLine["h"] != null)
                    Help();
                else if (CommandLine["u"] != null && CommandLine["p"] != null && CommandLine["d"] != null)
                {
                    ActiveDirectory ad = new ActiveDirectory();
                    ad.sDomain = CommandLine["d"];
                    ad.sServiceUser = CommandLine["u"];
                    ad.sServicePassword = CommandLine["p"];

                    if (CommandLine["L"] != null)
                        ad.PrintAllUsers();
                    if (CommandLine["LG"] != null)
                        ad.PrintGroups();
                    if (CommandLine["DA"] != null)
                        ad.PrintDomainAdmins();
                    if (CommandLine["UG"] != null)
                        ad.PrintUserGroups(CommandLine["UG"]);
                    if (CommandLine["GG"] != null)
                        ad.PrintGroupGroups(CommandLine["GG"]);
                    if (CommandLine["GM"] != null)
                        ad.PrintGroupMembers(CommandLine["GM"]);
                    if (CommandLine["DC"] != null)
                        ad.PrintDCSystems();
                    if (CommandLine["DS"] != null)
                        ad.PrintDomainSystems();
                    if (CommandLine["B"] != null)
                        ad.PrintUser(CommandLine["B"]);
                    if (CommandLine["UN"] != null)
                        ad.UnlockUser(CommandLine["UN"]);
                    if (CommandLine["CP"] != null)
                        ad.ChangeUserPassword(CommandLine["CP"]);
                    if (CommandLine["AddToDA"] != null)
                        ad.AddToDA(CommandLine["AddToDA"]);
                    //if (CommandLine["RunMimikatz"] != null)
                    //    ad.InvokeMimikatz(CommandLine["RunMimikatz"], ad.sServiceUser, ad.sServicePassword);
                    if (CommandLine["C"] != null)
                    {
                        //NEED TO FIX THIS.  CommandLine["C"] comes out as a boolean instead of text....
                        try
                        {
                            //Console.WriteLine(CommandLine["C"].ToString());
                            string[] data = CommandLine["C"].Split(':');
                            Console.WriteLine(data.Count());
                            string userName = data[0].Replace("user=", string.Empty);
                            string password = data[1].Replace("pass=", string.Empty);
                            string first = data[2].Replace("first=", string.Empty);
                            string last = data[3].Replace("last=", string.Empty);
                            ad.AttemptUserCreation(userName, password, first, last, string.Empty);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                            Console.WriteLine("Invalid argument for user creation.  Use following format: -C user=username:pass=password:first=firstname:last=lastname");
                        }
                    }
                    if (CommandLine["CG"] != null)
                    {
                        try
                        {
                            string[] data = CommandLine["CG"].Split(':');
                            string userName = data[0].Replace("user=", string.Empty);
                            string password = data[1].Replace("pass=", string.Empty);
                            string first = data[2].Replace("first=", string.Empty);
                            string last = data[3].Replace("last=", string.Empty);
                            string group = data[4].Replace("group=", string.Empty);
                            ad.AttemptUserCreation(userName, password, first, last, group);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Invalid argument for user creation.  Use following format: -C user=username:pass=password:first=firstname:last=lastname:group=groupname");
                        }
                    }

                    if (CommandLine["PowerView"] != null)
                    {
                        Console.Clear();
                        var exit = false;
                        while (!exit)
                        {
                            Console.WriteLine();
                            Console.Write(">> ");
                            var command = Parser.Parse(Console.ReadLine());
                            exit = command.Execute();
                        }
                    }
                }
                else
                    Console.WriteLine("Usage:  ADTools.exe -u UserName -p Password -d Domain <options>");
                Console.WriteLine("Process Completed.");
            }
            catch (Exception ex)
            {
                string Message = string.Format("There was an unexpected error.\r\nMessage: {0}\r\n", ex.Message);
                Console.WriteLine(Message);
            }
        }

        public static void Help()
        {
            Console.WriteLine("Usage:  ADTools.exe -u UserName -p Password -d Domain <options>");
            Console.WriteLine("\n\nOptional Flags\n");
            Console.WriteLine("-L:  List all users in active directory domain");
            Console.WriteLine("-LG:  List all groups in active directory domain");
            Console.WriteLine("-DA:   List all domain administrators");
            Console.WriteLine("-UG:  List all groups a user belongs to.  Format: -UG username");
            Console.WriteLine("-GG:  List all groups a group belongs to.  Format: -GG groupname");
            Console.WriteLine("-GM:  List all members of specified AD group.  Format:  -GM groupname");
            Console.WriteLine("-DC:  List all domain controllers");
            Console.WriteLine("-DS:  List all domain systems");
            Console.WriteLine("-B:   List User details.  Format:  -B username");
            Console.WriteLine("-C:   Attempt account creation.  Format:  -C user=username:pass=password:first=firstname:last=lastname");
            Console.WriteLine("-CG:  Attempt account creation and add to group.  Format:  -CG user=username:pass=password:first=firstname:last=lastname:group=groupname");
            Console.WriteLine("-UN:  Unlocks the specified account.  Format: -UN username");
            Console.WriteLine("-CP:  Changes the specified accounts passwoord to \"B3h0ldTh3C0nqu3r1ngH3r0#1\".  Format: -CP username");
            Console.WriteLine("-h:   Display this screen :)");
            Console.WriteLine("\n\nRequired Flags\n");
            Console.WriteLine("-u:   Username used to authenticate to active directory");
            Console.WriteLine("-p:   Password used to authenticate to active directory");
            Console.WriteLine("-d:   Domain to connect to");
        }
    }
}
