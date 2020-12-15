using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

namespace Cognizant.SubmitQuestions
{
    public static class SubmitAnswers
    {
        [FunctionName("SubmitAnswers")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

           
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<QuestionAndRecords>(requestBody);
            var strConnString = Environment.GetEnvironmentVariable("SqlServerConnection");

            foreach(var que in data.QuestionIds)
            {
                InsertSelectedQuestions(que,data.RecordID,strConnString);
            }
            UpdateNotifiedEvent(data.RecordID, strConnString);

            return new OkObjectResult("Successfully Inserted");
        }

        private static void InsertSelectedQuestions(int questionId,int recordId, string strConnString)
        {
            using (SqlConnection con = new SqlConnection(strConnString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Insert_SelectedQuestions";
                cmd.Parameters.Add("@QuestionId", SqlDbType.Int).Value = questionId;
                cmd.Parameters.Add("@record_Id", SqlDbType.Int).Value = recordId;
                cmd.Connection = con;

                con.Open();
                var result = cmd.ExecuteScalar();                
            }
        }

        private static void UpdateNotifiedEvent(int recordId, string strConnString)
        {
           using (SqlConnection con = new SqlConnection(strConnString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "Update_NotifiedEvent";
                cmd.Parameters.Add("@record_id", SqlDbType.Int).Value = recordId;
                cmd.Connection = con;

                con.Open();
                cmd.ExecuteNonQuery();                
            } 
        }
    }

    public class QuestionAndRecords
    {
        public int RecordID { get; set; }
        public List<int> QuestionIds { get; set; }
    }
}
