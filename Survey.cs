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

namespace Cognizant.Question
{
    public static class GetQuestion
    {
        [FunctionName("GetQuestion")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var strConnString = Environment.GetEnvironmentVariable("SqlServerConnection");

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //recordId = recordId ?? data?.recordId;
            string responseMessage = string.Empty;
            using (SqlConnection connection = new SqlConnection(strConnString))
            {
                SqlCommand command = new SqlCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetQuestions";
                command.Connection = connection;
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                IList<Questions> questions = new List<Questions>();

                // Call Read before accessing data.
                while (reader.Read())
                {
                    var question = ReadSingleRow((IDataRecord)reader, log);
                    questions.Add(question);
                }
                responseMessage = JsonConvert.SerializeObject(questions);
                log.LogInformation(responseMessage);
                // Call Close when done reading.
                reader.Close();
            }
            return new OkObjectResult(responseMessage);
            
        }

        private static Questions ReadSingleRow(IDataRecord record, ILogger log)
        {
            Questions question = new Questions()
            {
                Question = Convert.ToString(record[0]),
                ID = Convert.ToInt32(record[1]),
                Type = Convert.ToString(record[2])
            };

            //log.LogInformation(String.Format("{0}, {1}, {2)", record[0], record[1], record[2]));
            return question;
        }
    }
    public class Questions
    {
        public string Question { get; set; }
        public int ID { get; set; }
        public string Type { get; set; }
    }
}
