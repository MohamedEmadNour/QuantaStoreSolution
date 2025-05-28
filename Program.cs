using Microsoft.EntityFrameworkCore;
using QuantaStore.Controllers;

namespace QuantaStore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure the database connection
            builder.Services.AddDbContext<MyDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            // Add services to the container
            builder.Services.AddControllers();

            // Add Swagger/OpenAPI for documentation
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add HttpClient and CORS policy for Angular
            builder.Services.AddHttpClient();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngular",
                    builder => builder.WithOrigins("http://localhost:4200")
                                      .AllowAnyHeader()
                                      .AllowAnyMethod());
            });

            var app = builder.Build();

            // Enable CORS for Angular client
            app.UseCors("AllowAngular");

            // Configure middleware to use in development
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Register custom middleware (SQL Injection checker)
            // app.UseMiddleware<SqlCheckerMiddleware>();

            // Authorization middleware
            app.UseAuthorization();

            // Configure endpoints for controllers
            app.MapControllers();

            // The following line `app.UseEndpoints()` is unnecessary and should be removed
            // app.UseEndpoints();  // Remove this line

            // Run the application
            app.Run();
        }
    }
}
