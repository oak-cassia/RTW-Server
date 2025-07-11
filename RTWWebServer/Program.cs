using RTWWebServer;
using RTWWebServer.Middlewares;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

builder.Services.AddRedisCache(configuration);
builder.Services.AddWebApiServices();
builder.Services.AddCustomServices();
builder.Services.AddRepositories();
builder.Services.AddConfigurations(configuration);
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