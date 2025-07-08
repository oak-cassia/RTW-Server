using RTWWebServer;
using RTWWebServer.Configuration;
using RTWWebServer.Exceptions;
using RTWWebServer.Middlewares;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;
builder.Services.Configure<DatabaseConfiguration>(configuration.GetSection(nameof(DatabaseConfiguration)));

builder.Services.AddStackExchangeRedisCache(options =>
{
    DatabaseConfiguration? dbConfig = configuration.GetSection(nameof(DatabaseConfiguration)).Get<DatabaseConfiguration>();
    options.Configuration = dbConfig?.Redis;
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalGameExceptionHandler>();
builder.Services.AddCustomServices();
builder.Services.AddRepositories();
builder.Services.AddConfigurations();
builder.Services.AddEntityFramework(configuration);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(opt => { });
app.UseMiddleware<UserAuthenticationMiddleware>();
app.UseMiddleware<RequestLockingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();