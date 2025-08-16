using RTWWebServer;
using RTWWebServer.Middlewares;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add JSON file for master data before getting configuration
builder.Configuration.AddJsonFile("MasterData/CharacterMaster.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Configuration;

builder.Services.AddRedisCache(configuration);
builder.Services.AddWebApiServices();
builder.Services.AddJwtAuthentication(configuration);
builder.Services.AddCustomServices();
builder.Services.AddRepositories();
builder.Services.AddConfigurations(configuration);
builder.Services.AddEntityFramework(configuration);
builder.Services.AddMasterDataSystem(configuration);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(opt => { });
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserAuthenticationMiddleware>();
app.UseMiddleware<RequestLockingMiddleware>();
app.MapControllers();
app.Run();