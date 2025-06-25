using RTWWebServer;
using RTWWebServer.Configuration;
using RTWWebServer.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;
builder.Services.Configure<DatabaseConfiguration>(configuration.GetSection(nameof(DatabaseConfiguration)));

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomServices();
builder.Services.AddRepositories();
builder.Services.AddConfigurations();


WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<UserAuthenticationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// DependencyInjectionExtensions는 별도 파일로 분리되었습니다.
