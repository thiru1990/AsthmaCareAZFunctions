using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;

namespace Cognizant.Notification
{
    public static class Notification
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("Notification")]
        public static void Run([IoTHubTrigger("messages/events", Connection = "IoTHubTriggerConnection")] EventData message, ILogger log)
        {
            log.LogInformation($"IoT Hub trigger processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");
            string telemetry = Encoding.UTF8.GetString(message.Body.Array);

            var sensor = JsonConvert.DeserializeObject<RawTelemetry>(telemetry);
            var strConnString = Environment.GetEnvironmentVariable("SqlServerConnection");

            using (SqlConnection con = new SqlConnection(strConnString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Insert_Telemetry";
                cmd.Parameters.Add("@Chip_id", SqlDbType.Int).Value = sensor.ChipId;
                cmd.Parameters.Add("@Patient_id", SqlDbType.Int).Value = sensor.PatientId;
                cmd.Parameters.Add("@Pressure", SqlDbType.Decimal).Value = sensor.Pressure;
                cmd.Parameters.Add("@Temperetaure", SqlDbType.Decimal).Value = sensor.Temperature;
                cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                cmd.Connection = con;

                con.Open();
                var result = cmd.ExecuteScalar();
                log.LogInformation("Telemetry data from IOT inserted - RecordId:" + result.ToString());
            }
            using (SqlConnection connection = new SqlConnection(strConnString))
            {
                SqlCommand command = new SqlCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Notification";
                command.Parameters.Add("@Patient_id", SqlDbType.Int).Value = sensor.PatientId;
                command.Connection = connection;
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                IList<MedicalRecord> sensorNotifyList = new List<MedicalRecord>();

                // Call Read before accessing data.
                while (reader.Read())
                {
                    var sensorNotifyRecord = ReadSingleRow((IDataRecord)reader, log);
                    sensorNotifyList.Add(sensorNotifyRecord);
                }
                var serializedSensorNotify = JsonConvert.SerializeObject(sensorNotifyList);
                log.LogInformation(serializedSensorNotify);
                // Call Close when done reading.
                reader.Close();
            }
        }

        private static MedicalRecord ReadSingleRow(IDataRecord record, ILogger log)
        {
            MedicalRecord sensorNotify = new MedicalRecord()
            {
                RecordId = Convert.ToInt32(record[0]),
                PatientName = Convert.ToString(record[1]),
                Temperature = Convert.ToDecimal(record[2]),
                Pressure = Convert.ToDecimal(record[3]),
                CreatedTime = record[4].ToString().Substring(0, 8)
            };

            log.LogInformation(String.Format("{0}, {1}, {2}, {3}, {4}", record[0], record[1], record[2], record[3], record[4]));
            return sensorNotify;
        }
    }

    public class RawTelemetry
    {
        public string ChipId { get; set; }
        public int PatientId { get; set; }
        public decimal Temperature { get; set; }
        public decimal Pressure { get; set; }
    }
    public class MedicalRecord
    {
        public int RecordId { get; set; }
        public string PatientName { get; set; }
        public string ChipId { get; set; }
        public int PatientId { get; set; }
        public decimal Temperature { get; set; }
        public decimal Pressure { get; set; }
        public string CreatedTime { get; set; }
    }
}