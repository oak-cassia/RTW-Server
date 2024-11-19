using Microsoft.Extensions.Options;
using MySqlConnector;
using RTWWebServer.Authentication;
using RTWWebServer.Configuration;
using RTWWebServer.Database;
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

app.UseAuthorization();

app.MapControllers();

app.Run();

void InjectDependencies()
{
    builder.Services.AddSingleton<IGuidGenerator, GuidGenerator>();
    builder.Services.AddSingleton<IAuthTokenGenerator, AuthTokenGenerator>();
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
    builder.Services.AddSingleton<IRedisRepository, RedisRepository>(); // thread safe 함
    builder.Services.AddScoped<IMySqlConnectionFactory, MySqlConnectionFactory>(); // thread safe 하지 않음
    builder.Services.AddScoped<IGuestRepository, GuestRepository>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
    builder.Services.AddTransient<ILoginService, LoginService>();
    builder.Services.AddTransient<IAccountService, AccountService>();
}