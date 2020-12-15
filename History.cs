using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;

namespace Cognizant.History
{
    public static class History
    {
        [FunctionName("History")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<SearchInput>(requestBody);
            var strConnString = Environment.GetEnvironmentVariable("SqlServerConnection");
            var history = JsonConvert.SerializeObject(GetMedicationHistory(data,strConnString, log));

            return new OkObjectResult(history);
        }

        public static List<MedicationRecordHistory> GetMedicationHistory(SearchInput data, string strConnString, ILogger log)
        {
            using (SqlConnection connection = new SqlConnection(strConnString))
            {
                SqlCommand command = new SqlCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Get_HistoryRecords";
                command.Parameters.Add("@patiend_id", SqlDbType.Int).Value = data.PatientId;
                command.Parameters.Add("@BeginDate", SqlDbType.DateTime).Value = data.BeginDate;
                command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = data.EndDate;
                command.Connection = connection;
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                List<MedicationRecordHistory> medicationRecordHistory = new List<MedicationRecordHistory>();

                // Call Read before accessing data.
                while (reader.Read())
                {
                    var recordHistory = ReadSingleRow((IDataRecord)reader, log, strConnString);
                    medicationRecordHistory.Add(recordHistory);
                }
                //var serializedSensorNotify = JsonConvert.SerializeObject(sensorNotifyList);
                //log.LogInformation(serializedSensorNotify);
                // Call Close when done reading.
                reader.Close();
                return medicationRecordHistory;
            }
        }
        private static MedicationRecordHistory ReadSingleRow(IDataRecord record, ILogger log, string strConnString)
        {
            return new MedicationRecordHistory()
            {
                RecordId = Convert.ToInt32(record[0]),
                Temperature = Convert.ToDecimal(record[1]),
                Pressure = Convert.ToDecimal(record[2]),
                CreatedDate = Convert.ToDateTime(record[3]),
                Usage = Convert.ToString(record[4]),
                IsNotified = Convert.ToBoolean(record[5]),
                SelectedQuestionIds = GetSelectedQuestions(Convert.ToBoolean(record[5]), Convert.ToInt32(record[0]), strConnString)
            };
        }

        private static List<int> GetSelectedQuestions(bool isNotified, int recordId, string strConnString)
        {
            if(isNotified == false)
                return null;
            
            using (SqlConnection connection = new SqlConnection(strConnString))
            {
                SqlCommand command = new SqlCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Get_SelectedQuestions";
                command.Parameters.Add("@record_id", SqlDbType.Int).Value = recordId;
                command.Connection = connection;
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                List<int> questionList = new List<int>();

                // Call Read before accessing data.
                while (reader.Read())
                {
                    questionList.Add(Convert.ToInt32(reader[0]));
                }
                reader.Close();
                return questionList;
            }


        }
    }

    public class SearchInput
    {
        public int PatientId { get; set; }
        public string BeginDate { get; set;}
        public string EndDate { get; set;}
    }

    public class MedicationRecordHistory
    {
        public int RecordId { get; set; }
        public decimal Temperature { get; set; }
        public decimal Pressure { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Usage { get; set; }
        public bool IsNotified { get; set; }
        public List<int> SelectedQuestionIds { get; set; }
    }
}
