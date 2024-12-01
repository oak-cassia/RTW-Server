using RTWWebServer.Authentication;
using RTWWebServer.Configuration;
using RTWWebServer.Database;
using RTWWebServer.Database.Cache;
using RTWWebServer.Database.Repository;
using RTWWebServer.Middleware;
using RTWWebServer.Service;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;
builder.Services.Configure<DatabaseConfiguration>(configuration.GetSection(nameof(DatabaseConfiguration)));

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

InjectDependencies();

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

void InjectDependencies()
{
    // TODO: 가독성 올릴 방법 생각
    builder.Services.AddSingleton<IGuidGenerator, GuidGenerator>();
    builder.Services.AddSingleton<IAuthTokenGenerator, AuthTokenGenerator>();
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

    builder.Services.AddSingleton<IRemoteCache, RedisRemoteCache>(); // thread safe 함
    builder.Services.AddSingleton<IRemoteCacheKeyGenerator, RemoteCacheKeyGenerator>();

    builder.Services.AddScoped<IDatabaseContextProvider, DatabaseContextProvider>();

    builder.Services.AddScoped<IRequestScopedLocalCache, RequestScopedLocalCache>();

    builder.Services.AddScoped<IGuestRepository, GuestRepository>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();

    builder.Services.AddTransient<ILoginService, LoginService>();
    builder.Services.AddTransient<IAccountService, AccountService>();
}