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
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                _records.AddRange(_hardwareInfo.GetProcessorRecords());
                _records.AddRange(_hardwareInfo.GetRamRecords());
                _records.AddRange(_hardwareInfo.GetDiskRecords());

                foreach (var r in _records)
                {
                    Console.WriteLine("HardwareId => " + r.Hardware.Id);
                    Console.WriteLine("Model => " + r.Hardware.Model);
                    Console.WriteLine("Additional info => " + r.Hardware.AdditionalInfo);
                    Console.WriteLine("Value => " + r.Value);
                    Console.WriteLine("CreateDate => " + r.CreatedAt);
                    Console.WriteLine("================================================");

                    if (!dataAccess.HardwareIdAlreadyExists(r.Hardware.Id))
                    {
                        dataAccess.InsertHardware(r.Hardware);
                    }

                    dataAccess.InsertRecord(r);
                }

                //CsvWriter.WriteToCsv(_records);

                _records.Clear();

                await Task.Delay(_delayInSeconds, stoppingToken);
            }
        }
    }
}
