using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QuantaStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseController : ControllerBase
    {
        private readonly MyDbContext _dbContext;
        private readonly HttpClient _httpClient;

        public DatabaseController(MyDbContext dbContext , HttpClient httpClient)
        {
            _dbContext = dbContext;
            _httpClient = httpClient;
        }

        [HttpPost("execute-query")]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.query))
                return BadRequest("Query cannot be empty.");

            try
            {
                var isMalicious = await checkQueryAtAi(request.query);
                if (isMalicious)
                {
                    return BadRequest(new { message = "Query flagged as malicious by AI. Execution blocked." });
                }

                var result = await ExecuteDynamicSql(request.query);
                return Ok(new { message = "Query executed successfully!", rows = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error executing query", error = ex.Message });
            }
        }


        private async Task<List<Dictionary<string, object>>> ExecuteDynamicSql(string query)
        {
            var result = new List<Dictionary<string, object>>();

            using (var connection = _dbContext.Database.GetDbConnection())
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            result.Add(row);
                        }
                    }
                }
            }

            return result;
        }

        private async Task<bool> checkQueryAtAi(string context)
        {
            if (!context.IsNullOrEmpty())
            {

                var response = await _httpClient.PostAsJsonAsync("http://localhost:5001/check_query", new { Query = context });

                var result = await response.Content.ReadFromJsonAsync<SqlCheckResponse>();

                if (result?.IsMalicious == true)
                {
                    return true;
                }
                return false;

            }
            return false;
        }
    }

    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
    }

    public class QueryRequest
    {
        public string query { get; set; }
    }
    public class SqlCheckResponse
    {
        [JsonPropertyName("is_malicious")]
        public bool IsMalicious { get; set; }
    }
}
