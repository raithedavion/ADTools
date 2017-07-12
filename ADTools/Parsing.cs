using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADTools
{
    public interface ICommand
    {
        bool Execute();
    }

    public class ExitCommand : ICommand
    {
        public bool Execute()
        {
            return true;
        }
    }

    public class ContinueCommand : ICommand
    {
        public bool Execute()
        {
            return false;
        }
    }

    public static class Parser
    {
        public static ICommand Parse(string commandString)
        {
            if (commandString.Contains("|"))
            {
                return PipedParse(commandString);
            }
            else
            {
                return SingleParse(commandString);
            }
        }

        public static ICommand SingleParse(string commandString)
        {
            var commandParts = Utilities<string>.ToList(commandString.Split(' '));
            var commandName = commandParts[0];
            var args = Utilities<string>.Skip(1, commandParts).ToArray();//commandParts.Skip(1).ToArray();
            if (commandName == "exit")
                return new ExitCommand();
            else
            {
                string cmd = ExecuteCommand(commandName, args);
                //Console.WriteLine(cmd);
                return new ContinueCommand();
            }
        }

        public static string SingleParse(string commandName, string arg)
        {
            List<string> args = new List<string> { arg };
            return ExecuteCommand(commandName, args.ToArray());
        }

        public static ICommand PipedParse(string commandString)
        {
            var initialParts = Utilities<string>.ToList(commandString.Split('|'));
            initialParts = Utilities<string>.TrimAll(initialParts);//(from temp in initialParts select temp.Trim()).ToList();
            int Total = initialParts.Count;
            string Results = string.Empty;
            for (int i = 0; i < Total; i++)
            {
                var commandParts = Utilities<string>.ToList(commandString.Split(' '));
                var commandName = commandParts[0];
                var args = Utilities<string>.Skip(1, commandParts).ToArray();//commandParts.Skip(1).ToArray();
                args = Utilities<string>.TrimAll(args);//(from temp in args select temp.Trim()).ToArray();
                if (i < (Total - 1))
                {
                    if (Results != string.Empty)
                    {
                        Results = SingleParse(commandName, Results);
                    }
                    else
                        Results = ExecuteCommand(commandName, args);
                }
                else
                {
                    string cmd = BuildCommand(commandName, Results);
                    return SingleParse(cmd);
                }
            }
            return new ContinueCommand();
        }

        public static string BuildCommand(string command, string argument)
        {
            return string.Format("{0} {1}", command, argument);
        }

        public static string BuildCommand(string command, List<string> arguments)
        {
            string commandString = command;
            foreach (string s in arguments)
            {
                commandString += " " + s;
            }
            return commandString;
        }

        public static string BuildCommand(string command, string[] arguments)
        {
            string commandString = command;
            foreach (string s in arguments)
            {
                commandString += " " + s;
            }
            return commandString;
        }

        public static string ExecuteCommand(string command, string[] arguments)
        {
            string result = string.Empty;
            switch (command)
            {
                case "exit":
                    return "true";
                case "Get-IPAddress":
                    result = new PowerView().GetIPAddress(arguments[0]);
                    return result;
                case "Get-Content":
                    result = new PowerView().GetContent(arguments);
                    return result;
                case "System":
                    result = new PowerView().RunSystem(arguments);
                    return result;

            }
            return string.Empty;
        }
    } 
}
