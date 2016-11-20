using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MCEBuddy.GenLocalised
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("GENLOCALISED - Generate localised translation strings for MCEBuddy");
            if (args.Length != 1)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: GenLocalised <localisation path>");
                Console.WriteLine();
                return;
            }
            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine();
                Console.WriteLine("Directory not found " + args[0]);
                Console.WriteLine();
                return;
            }
            TranslateAll transAll = new TranslateAll(args[0]);
            transAll.Run();
            //Translate trans = new Translate(args[0], "nl");
            //trans.TranslateFile();
        }
    }
}
