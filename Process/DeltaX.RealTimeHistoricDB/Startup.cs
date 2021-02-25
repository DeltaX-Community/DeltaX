using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting; 
using Microsoft.Extensions.Hosting; 
using Microsoft.Extensions.Options; 

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<RealTimeHistoricDBConfiguration> options)
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

        // HTTP Cors        
        var origins = options?.Value.CorsUrls
            ?? new[] { "http://127.0.0.1:8080", "https://127.0.0.1:8081", "http://localhost:8080" };

        app.UseCors(x => x
            .WithOrigins(origins)
            .AllowAnyMethod()
            .AllowCredentials()
            .AllowAnyHeader());

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
