using Microsoft.AspNetCore.Hosting; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 
using DeltaX.Process.RealTimeHistoricDB.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;

public class Startup
{ 
    public void ConfigureServices(IServiceCollection services)
    {        
        services.AddControllers();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DeltaX.Process.RealTimeHistoricDB", Version = "v1" });
        });
         
        var cors =  new[] { "http://127.0.0.1:8080", "https://127.0.0.1:8081", "http://localhost:8080" };
        services.AddCors(options =>
        { 
            options.AddPolicy("DefaultCors", builder =>
            {
                builder
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .WithOrigins(cors);
            });
        });
    }
     
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<RealTimeHistoryDBConfiguration> options)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        if (env.IsDevelopment() || options.Value.UseSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DeltaX.Process.RealTimeHistoricDB"));
        }

        app.UseCors("DefaultCors"); 
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        }); 
    }
}
