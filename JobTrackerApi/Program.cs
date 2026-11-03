using JobTrackerApi.Services;

namespace JobTrackerApi
{
    public class Program
    {
        // Path to the MCP server project. Can be overridden with "ServerProject" in appsettings.json.
        private const string DefaultServerProject =
            @"E:\Tech\2026\Projects\AI\MyMCP\JobTrackerMcp\JobTrackerMcp.csproj";

        // The Angular dev server runs here. Only this address may call the API from a browser.
        private const string AngularOrigin = "http://localhost:4200";

        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string connection = RequireEnvironment("JOBTRACKER_CONNECTION");
            string apiKey = RequireEnvironment("OPENAI_API_KEY");
            string model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
            string serverProject = builder.Configuration["ServerProject"] ?? DefaultServerProject;

            // One shared chat service for every request.
            ChatService chatService = await ChatService.CreateAsync(connection, apiKey, model, serverProject);
            builder.Services.AddSingleton(chatService);

            builder.Services.AddControllers();

            // Builds the API description that the Swagger page reads.
            builder.Services.AddOpenApi();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Angular", policy =>
                {
                    policy.WithOrigins(AngularOrigin)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            WebApplication app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                // /openapi/v1.json is the description; /swagger is the page that shows it.
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "JobTracker API");
                });
            }

            app.UseCors("Angular");
            app.MapControllers();

            await app.RunAsync();

            await chatService.DisposeAsync();
        }

        private static string RequireEnvironment(string name)
        {
            string? value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(name + " is not set.");
            }

            return value;
        }
    }
}
