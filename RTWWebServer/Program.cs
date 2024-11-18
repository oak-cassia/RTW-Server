using Microsoft.Extensions.Options;
using MySqlConnector;
using RTWWebServer.Authentication;
using RTWWebServer.Configuration;
using RTWWebServer.Database.Repository;
using RTWWebServer.Service;

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;
builder.Services.Configure<DatabaseConfiguration>(configuration.GetSection(nameof(DatabaseConfiguration)));

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

InjectDependencies();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

void InjectDependencies()
{
    builder.Services.AddScoped<MySqlConnection>(provider =>
    {
        var config = provider.GetRequiredService<IOptions<DatabaseConfiguration>>();
        var connection = new MySqlConnection(config.Value.AccountDatabase);
        connection.Open();
        return connection;
    });

    builder.Services.AddSingleton<IGuidGenerator, GuidGenerator>();
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
    builder.Services.AddTransient<IAccountRepository, AccountRepository>();
    builder.Services.AddTransient<IGuestRepository, GuestRepository>();
    builder.Services.AddTransient<ILoginService, LoginService>();
}