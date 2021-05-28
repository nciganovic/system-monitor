using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using System.Management;
using System.Diagnostics;
using Database;
using System.IO;

namespace Monitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private List<Record> _records;

        private PerformanceCounter ramCounter;
        private PerformanceCounter diskCounter;

        private DataAccess dataAccess = new DataAccess();

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _records = new List<Record>();
        }

        public int getAvailableRAM()
        {
            return (int)ramCounter.NextValue();
        }

        public int getAvailableDISK()
        {
            return (int)diskCounter.NextValue();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                
                GetProcessorInfo();
                GetRamInfo();
                GetDiskInfo();

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

                CsvWriter.WriteToCsv(_records);

                _records.Clear();

                await Task.Delay(10000, stoppingToken);
            }
        }

        private void GetProcessorInfo()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select Name, SerialNumber, LoadPercentage, DeviceID from Win32_Processor");

            foreach (ManagementObject obj in searcher.Get())
            {
                Hardware hardware = new Hardware
                {
                    Id = obj["SerialNumber"].ToString().Trim(),
                    Model = obj["Name"].ToString().Trim(),
                    AdditionalInfo = obj["DeviceId"].ToString().Trim() + " " + obj["SerialNumber"].ToString().Trim()
                };

                Record r = new Record
                {
                    Hardware = hardware,
                    Value = Convert.ToInt32(obj["LoadPercentage"].ToString().Trim()),
                    CreatedAt = DateTime.Now
                };

                _records.Add(r);
            }
        }

        private void GetRamInfo()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");

            int ramValue = getAvailableRAM();

            foreach (ManagementObject obj in searcher.Get())
            {
                Hardware hardware = new Hardware
                {
                    Id = obj["SerialNumber"].ToString().Trim(),
                    Model = obj["Manufacturer"].ToString().Trim() + " " + obj["PartNumber"].ToString().Trim(),
                    AdditionalInfo = obj["Name"].ToString().Trim() + " " + obj["SerialNumber"].ToString().Trim()
                };

                Record r = new Record
                {
                    Hardware = hardware,
                    Value = ramValue,
                    CreatedAt = DateTime.Now
                };

                _records.Add(r);
            }
        }


        private void GetDiskInfo()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select Model, SerialNumber, DeviceID from Win32_DiskDrive");

            int availableDisk = getAvailableDISK();

            foreach (ManagementObject obj in searcher.Get())
            {
                Hardware hardware = new Hardware
                {
                    Id = obj["SerialNumber"].ToString().Trim(),
                    Model = obj["Model"].ToString().Trim(),
                    AdditionalInfo = obj["DeviceId"].ToString().Trim() + " " + obj["SerialNumber"].ToString().Trim()
                };

                Record r = new Record
                {
                    Hardware = hardware,
                    Value = availableDisk,
                    CreatedAt = DateTime.Now
                };

                _records.Add(r);
            }
        }       

    }
}
