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

namespace Monitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private List<Record> _records;

        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private PerformanceCounter diskCounter;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _records = new List<Record>();
        }

        public int getCurrentCpuUsage()
        {
            return (int)cpuCounter.NextValue();
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
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
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
                    Console.WriteLine("Model => " + r.Hardware.Model);
                    Console.WriteLine("Additional info => " + r.Hardware.AdditionalInfo);
                    Console.WriteLine("Value => " + r.Value);
                    Console.WriteLine("CreateDate => " + r.CreatedAt);
                    Console.WriteLine("================================================");
                }

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
                    Model = obj["Name"].ToString(),
                    AdditionalInfo = obj["DeviceId"].ToString() + " " + obj["SerialNumber"].ToString()
                };

                Record r = new Record
                {
                    Hardware = hardware,
                    Value = Convert.ToInt32(obj["LoadPercentage"].ToString()),
                    CreatedAt = DateTime.Now
                };

                _records.Add(r);
            }
        }

        private void GetRamInfo()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");

            double ramUtil = CalculateRamUtil();

            foreach (ManagementObject obj in searcher.Get())
            {
                Hardware hardware = new Hardware
                {
                    Model = obj["Manufacturer"].ToString() + " " + obj["PartNumber"].ToString(),
                    AdditionalInfo = obj["Name"].ToString() + " " + obj["SerialNumber"].ToString()
                };

                Record r = new Record
                {
                    Hardware = hardware,
                    Value = Convert.ToInt32(ramUtil),
                    CreatedAt = DateTime.Now
                };

                _records.Add(r);
            }
        }


        private void GetDiskInfo()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select Model, SerialNumber, DeviceID from Win32_DiskDrive");

            foreach (ManagementObject obj in searcher.Get())
            {
                Hardware hardware = new Hardware
                {
                    Model = obj["Model"].ToString(),
                    AdditionalInfo = obj["DeviceId"].ToString() + " " + obj["SerialNumber"].ToString()
                };

                Record r = new Record
                {
                    Hardware = hardware,
                    Value = getAvailableDISK(),
                    CreatedAt = DateTime.Now
                };

                _records.Add(r);
            }
        }

        private double CalculateRamUtil() {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory, TotalVisibleMemorySize FROM Win32_OperatingSystem");

            double freeMemory = 0;
            double totalMemory = 0;
            
            foreach (ManagementObject obj in searcher.Get()) 
            {
                freeMemory += Convert.ToDouble(obj["FreePhysicalMemory"].ToString());
                totalMemory += Convert.ToDouble(obj["TotalVisibleMemorySize"].ToString());
            }

            return CalculateFreeMemoryPercentage(freeMemory, totalMemory);
        }

        private double CalculateFreeMemoryPercentage(double freeMemory, double totalMemory) 
        {
            return (freeMemory * 100) / totalMemory;
        }

    }
}
