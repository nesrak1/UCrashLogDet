using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UCrashLogDet
{
    class Program
    {
        static void Main(string[] args)
        {
            string errorLogPath = null;

            try
            {
                foreach (string arg in args)
                {
                    if (arg.Contains("error.log"))
                    {
                        errorLogPath = arg;
                        break;
                    }
                }

                if (errorLogPath == null)
                {
                    File.WriteAllText("ucld.txt", "couldn't find error.log in args");
                }

                long errorRip = -1;
                string[] errorLines = File.ReadAllLines(errorLogPath);
                foreach (string errorLine in errorLines)
                {
                    if (errorLine.StartsWith("RIP:"))
                    {
                        errorRip = long.Parse(errorLine.Substring(10, 8), NumberStyles.HexNumber);
                    }
                }

                if (errorRip == -1)
                {
                    File.WriteAllText("ucld.txt", "couldn't find error.log rip");
                }

                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                BinaryReader br = new BinaryReader(File.OpenRead(Path.Combine(exeDir, @"..\\unity_x64.pdb")));
                PdbFile pdbFile = new PdbFile(br);
                long lastAddr = -1;
                for (int i = 0; i < pdbFile.funcAddrs.Count; i++)
                {
                    long addr = pdbFile.funcAddrs[i];
                    if ((addr + 0x140001000) > errorRip + 0x100000000)
                    {
                        Console.WriteLine("crashed on " + pdbFile.funcLookup[0x100000000 + lastAddr].name);
                        break;
                    }
                    lastAddr = addr;
                }
                Console.WriteLine("done");
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception thrown:");
                Console.WriteLine(ex.ToString());
            }
            Console.ReadKey();
        }
    }
}
