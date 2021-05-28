using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerLibrary.Domain;

namespace WorkerLibrary
{
    public static class CsvWriter
    {
        public static void WriteToCsv(List<Record> records)
        {
            string path = "./Data/Test.csv";

            using (var w = new StreamWriter(path, File.Exists(path))) // writer will either create new file or append on existing one if is found on given location
            {
                //If doesn't exist then add header values for csv
                if (!File.Exists(path)) 
                { 
                    w.WriteLine("Value,CreateDate,Model,AdditionalInfo");
                    w.Flush();
                }

                foreach (var r in records)
                {
                    var line = string.Format("{0},{1},{2},{3}", r.Value, r.CreatedAt, r.Hardware.Model, r.Hardware.AdditionalInfo);
                    w.WriteLine(line);
                    w.Flush();
                }
            }
        }
    }
}
