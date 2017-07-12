using ADTools.Computers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ADTools
{
    public class PowerView
    {
        public string GetIPAddress()
        {
            return "127.0.0.1";
        }

        public string GetIPAddress(string computerName)
        {
            if (computerName == null || computerName == string.Empty)
                return GetIPAddress();
            else if (computerName.Contains("\n"))
            {
                string results = string.Empty;
                string[] names = computerName.Split('\n');
                List<System.Net.IPAddress> addresses = new List<System.Net.IPAddress>();
                List<ComputerData> Data = new List<ComputerData>();
                foreach (string s in names)
                {
                    ComputerData current = new ComputerData();
                    current.ComputerName = s;
                    current.IPAddresses = System.Net.Dns.GetHostEntry(s).AddressList.ToList();
                    Data.Add(current);
                    //addresses.AddRange(Utilities<System.Net.IPAddress>.ToList(System.Net.Dns.GetHostEntry(s).AddressList));
                    foreach (System.Net.IPAddress ip in addresses)
                    {
                        results += ip.ToString() + "\n";
                        //System.Management.Automation.PSObject Out = new System.Management.Automation.PSObject();
                        //System.Management.Automation.PSNoteProperty Name = new System.Management.Automation.PSNoteProperty("Computer Name", s);
                        //System.Management.Automation.PSNoteProperty IP = new System.Management.Automation.PSNoteProperty("IP Address", ip.ToString());
                        //Out.Members.Add(Name);
                        //Out.Members.Add(IP);
                        //Console.WriteLine(Out);
                    }
                }
                Console.WriteLine(Data.ToStringTable(new[] { "Computer Name", "IP Address" }, a => a.ComputerName, a => a.IPAddresses.ToStringTable(new [] { "" }, b => b.ToString())));
                return results;
            }
            else
            {
                string results = string.Empty;
                List<ComputerData> Data = new List<ComputerData>();
                List<System.Net.IPAddress> addresses = Utilities<System.Net.IPAddress>.ToList(System.Net.Dns.GetHostEntry(computerName).AddressList);
                ComputerData current = new ComputerData();
                current.ComputerName = computerName;
                current.IPAddresses = System.Net.Dns.GetHostEntry(computerName).AddressList.ToList();
                Data.Add(current);
                foreach (System.Net.IPAddress ip in addresses)
                {
                    results += ip.ToString() + "\n";
                    //System.Management.Automation.PSObject Out = new System.Management.Automation.PSObject();
                    //System.Management.Automation.PSNoteProperty Name = new System.Management.Automation.PSNoteProperty("Computer Name", computerName);
                    //System.Management.Automation.PSNoteProperty IP = new System.Management.Automation.PSNoteProperty("IP Address", ip.ToString());
                    //Out.Members.Add(Name);
                    //Out.Members.Add(IP);
                    //Console.WriteLine(Out);
                }
                Console.WriteLine(Data.ToStringTable(new[] { "Computer Name", "IP Address" }, a => a.ComputerName, a => a.IPAddresses.ToStringTable(new[] { "", "" } , b => b.ToString() ,b => b.ToString())));
                return results;
            }
            //return "xxx.xxx.xxx.xxx";
        }

        public string GetContent(string[] args)
        {
            string Result = string.Empty;
            Arguments Args = new Arguments(args);
            if (Args["path"] != null)
            {
                System.IO.StreamReader file = new System.IO.StreamReader(Args["path"]);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    Result += line + "\n";
                }
                return Result;
            }//return System.IO.File.ReadAllLines(fileName).ToList();
            return string.Empty;
        }

        public string RunSystem(string[] args)
        {
            string command = args[0];
            string[] commandArgs = Utilities<string>.Skip(1, args).ToArray();//args.Skip(1).ToArray();
            string Result = string.Empty;
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = command.Trim();
            if (args.Length > 0)
                p.StartInfo.Arguments = BuildArgumentString(commandArgs);
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            Result = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return Result;
        }

        private string BuildArgumentString(string[] args)
        {
            string argument = string.Empty;
            foreach (string s in args)
            {
                argument += s + " ";
            }
            argument = argument.Trim();
            return argument;
        }
    }
}
