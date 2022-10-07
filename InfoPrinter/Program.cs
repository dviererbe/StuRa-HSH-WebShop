using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bpac;

namespace InfoPrinter
{
    class Program
    {
        static void Main(string[] args)
        {
            string id = args[0];

            string item1 = args.Length > 1 ? args[1] : string.Empty;
            string item2 = args.Length > 2 ? args[2] : string.Empty;
            string item3 = args.Length > 3 ? args[3] : string.Empty;

            string templatePath = @"C:\Users\Dominik\Documents\Eigene Etiketten\Test2.lbx";

            bpac.IDocument doc = new Document();
            if (doc.Open(templatePath))
            {
                doc.GetObject("A").Text = id;
                doc.GetObject("B").Text = id;
                doc.GetObject("C").Text = item1;
                doc.GetObject("D").Text = item2;
                doc.GetObject("E").Text = item3;


                PrintSuccess(doc.SetMediaById(doc.Printer.GetMediaId(), true), "SetMediaById", doc);
                PrintSuccess(doc.SetPrinter("Brother QL-700", true), "SetPrinter", doc);
                PrintSuccess(doc.StartPrint("", PrintOptionConstants.bpoDefault), "StartPrint", doc);
                PrintSuccess(doc.PrintOut(1, PrintOptionConstants.bpoDefault), "PrintOut", doc);
                PrintSuccess(doc.EndPrint(), "EndPrint", doc);
                PrintSuccess(doc.Close(), "Close", doc);
            }
            else
            {
                Console.WriteLine("Open() Error: " + doc.ErrorCode);
            }

		}

        private static void PrintSuccess(bool state, string description, IDocument document)
        {
            Console.WriteLine($"{description}: {state}");

            if (!state)
                Debug.WriteLine($" - Error: {document.ErrorCode}");
        }
    }
}
