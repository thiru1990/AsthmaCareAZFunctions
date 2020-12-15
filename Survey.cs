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

namespace Cognizant.Survey
{
    public static class Survey
    {
        [FunctionName("Survey")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string recordId = null;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            recordId = recordId ?? data?.recordId;
            string responseMessage = string.Empty;
            if (!String.IsNullOrEmpty(recordId))
            {
                var strConnString = Environment.GetEnvironmentVariable("SqlServerConnection");
                using (SqlConnection con = new SqlConnection(strConnString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "InsertSurvey";
                    cmd.Parameters.Add("@Record_id", SqlDbType.Int).Value = Convert.ToInt32(recordId);

                    cmd.Connection = con;

                    con.Open();
                    cmd.ExecuteNonQuery();
                    log.LogInformation("Survey updated record");
                }
                responseMessage =  "Medical record successfully";
            }
            return new OkObjectResult(responseMessage);
            
        }
    }
}
