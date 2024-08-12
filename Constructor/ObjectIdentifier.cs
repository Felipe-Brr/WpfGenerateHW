using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using listbox = System.Windows.Controls.ListBox;

namespace WpfGenerateHW.Constructor
{
    class ObjectIdentifier
    {
        public static Dictionary<string, string> Identifier;
        private static listbox lbMessage;

        public static void ReadSettings(listbox _lbMessage)
        {
            lbMessage = _lbMessage;
            Identifier = new Dictionary<string, string>();

            string appBaseDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            string filename = appBaseDir + "\\objects.csv";

            if (!File.Exists(filename)) 
            {
                lbMessage.Items.Insert(0, DateTime.Now.ToString() + " Could Not locate File: " + filename);
                return;
            }

            try
            {
                var reader = new StreamReader(filename);
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(';');
                    Identifier.Add(values[0], values[1]);
                }
                lbMessage.Items.Insert(0, DateTime.Now.ToString() + " File objects.cvs was read");
            }
            catch(Exception ex) 
            {
                lbMessage.Items.Insert(0, DateTime.Now.ToString() + " " + ex.Message);
            }
        }
    }
}
