using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerLibrary.Domain;

namespace WorkerLibrary
{
    public class DataAccess
    {
        public void InsertHardware(Hardware hardware)
        {
            using (SqliteConnection cnn = new SqliteConnection(LoadConnectionString()))
            {
                SqliteCommand command = new SqliteCommand("INSERT INTO Hardwares (Id, Model, AdditionalInfo) VALUES (@id, @model, @info);", cnn);

                command.Parameters.AddWithValue("id", hardware.Id);
                command.Parameters.AddWithValue("model", hardware.Model);
                command.Parameters.AddWithValue("info", hardware.AdditionalInfo);

                cnn.Open();

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        public void InsertRecord(Record record)
        {
            using (SqliteConnection cnn = new SqliteConnection(LoadConnectionString()))
            {
                SqliteCommand command = new SqliteCommand("INSERT INTO Records (HardwareId, Value, CreateDate) VALUES (@hid, @value, @date);", cnn);

                command.Parameters.AddWithValue("hid", record.Hardware.Id);
                command.Parameters.AddWithValue("value", record.Value);
                command.Parameters.AddWithValue("date", record.CreatedAt);

                cnn.Open();

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        public bool HardwareIdAlreadyExists(string hardwareId)
        {
            using (SqliteConnection cnn = new SqliteConnection(LoadConnectionString()))
            {
                SqliteCommand command = new SqliteCommand("SELECT Model FROM Hardwares WHERE Id = @id;", cnn);

                try
                {
                    command.Parameters.AddWithValue("@id", hardwareId);

                    cnn.Open();

                    SqliteDataReader reader = command.ExecuteReader();

                    return reader.HasRows;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        private string LoadConnectionString(string id = "Default")
        {
            //Loading connection string from App.config file in Monitor project.
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
