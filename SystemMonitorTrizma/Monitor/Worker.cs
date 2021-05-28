using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Diagnostics;
using WorkerLibrary;
using WorkerLibrary.Domain;
using System.Configuration;

namespace Monitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private List<Record> _records;
        private HardwareInfo _hardwareInfo;

        //Get delay for service in seconds
        private readonly int _delayInSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["SecondsDelay"]) * 1000;


        private DataAccess dataAccess = new DataAccess();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            _records = new List<Record>();
            _hardwareInfo = new HardwareInfo();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Running while loop with delay defined at the botton
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                //Add records for all hardware types in one list
                _records.AddRange(_hardwareInfo.GetProcessorRecords());
                _records.AddRange(_hardwareInfo.GetRamRecords());
                _records.AddRange(_hardwareInfo.GetDiskRecords());

                foreach (var r in _records)
                {
                    //Dispaly values in console
                    Console.WriteLine("HardwareId => " + r.Hardware.Id);
                    Console.WriteLine("Model => " + r.Hardware.Model);
                    Console.WriteLine("Additional info => " + r.Hardware.AdditionalInfo);
                    Console.WriteLine("Value => " + r.Value);
                    Console.WriteLine("CreateDate => " + r.CreatedAt);
                    Console.WriteLine("================================================");

                    //Check if in Hardwares table there is already row with this primary key
                    //Primary key is serial number of hardware
                    //I made this descision because serial number is unique for each individual hardware and it's the easiest way of checking if this hardware is already inserted
                    //Other option could be to set primary key as int autoincrement and then define sparate column in table for serial numbers
                    if (!dataAccess.HardwareIdAlreadyExists(r.Hardware.Id))
                    {
                        dataAccess.InsertHardware(r.Hardware);
                    }

                    //Inserts record in database
                    dataAccess.InsertRecord(r);
                }

                //Writng records in csv format
                //CsvWriter.WriteToCsv(_records);

                //Clear list for next iteration
                _records.Clear();

                await Task.Delay(_delayInSeconds, stoppingToken);
            }
        }
    }
}
