using ScriptInterpreter.RunTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            RunEnvironment.Instance.Init();
            if (args.Length > 0)
            {
                string scriptFileName = args[0];
                using (StreamReader objReader = new StreamReader(scriptFileName))
                {
                    StringBuilder sb = new StringBuilder();

                    string s = objReader.ReadLine();
                    while (s != null)
                    {
                        sb.Append(s);
                        s = objReader.ReadLine();    
                    }

                    RunEnvironment.Instance.Interprete(sb.ToString()); 
                }
            }
        }
    }
}
