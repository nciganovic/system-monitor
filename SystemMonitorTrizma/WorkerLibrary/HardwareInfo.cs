using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using WorkerLibrary.Domain;

namespace WorkerLibrary
{
    public class HardwareInfo
    {
        private PerformanceCounter ramCounter;
        private PerformanceCounter diskCounter;

        public HardwareInfo()
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes"); //get total available ram in megabytes
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"); //get total disk utilization in % 
        }

        private int GetAvailableRam()
        {
            return (int)ramCounter.NextValue();
        }

        private int GetAvailableDisk()
        {
            return (int)diskCounter.NextValue();
        }

        public List<Record> GetProcessorRecords()
        {
            List<Record> records = new List<Record>();

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

                records.Add(r);
            }

            return records;
        }

        public List<Record> GetRamRecords()
        {
            List<Record> records = new List<Record>();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");

            int ramAvailableValue = GetAvailableRam();
            int ramTotalValue = GetTotalRam();

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
                    Value = CalculateAvailableRamPercent(ramAvailableValue, ramTotalValue),
                    CreatedAt = DateTime.Now
                };

                records.Add(r);
            }

            return records;
        }

        public List<Record> GetDiskRecords()
        {
            List<Record> records = new List<Record>();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select Model, SerialNumber, DeviceID from Win32_DiskDrive");

            int availableDisk = GetAvailableDisk(); //I couldn't get utilization value for each disk so i added total value on every record

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

                records.Add(r);
            }

            return records;
        }

        private int GetTotalRam() {
            int ram = 0;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize from Win32_OperatingSystem");
            
            foreach (ManagementObject obj in searcher.Get()) {
                ram += Convert.ToInt32(obj["TotalVisibleMemorySize"].ToString());
            }

            return ram / 1024;
        }

        private int CalculateAvailableRamPercent(int available, int total) {
            return (available * 100) / total;
        }
    }
}
