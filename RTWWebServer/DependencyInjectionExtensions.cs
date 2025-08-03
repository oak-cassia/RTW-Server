using Microsoft.EntityFrameworkCore;
using RTWWebServer.Cache;
using RTWWebServer.Data;
using RTWWebServer.Data.Repositories;
using RTWWebServer.Providers.Authentication;
using RTWWebServer.Services;
using RTWWebServer.Configuration;
using RTWWebServer.Exceptions;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace RTWWebServer;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        services.AddSingleton<IGuidGenerator, GuidGenerator>();
        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();


        // IDistributedCache 어댑터 등록
        services.AddSingleton<IDistributedCacheAdapter, DistributedCacheAdapter>();
        services.AddSingleton<IRemoteCacheKeyGenerator, RemoteCacheKeyGenerator>();

        // 기존 캐시 서비스들을 어댑터 기반으로 교체
        services.AddScoped<IRequestScopedLocalCache, RequestScopedLocalCache>(); // 요청 범위 캐시
        services.AddScoped<ICacheManager, CacheManager>(); // 캐시 관리자, 요청 범위 캐시 의존

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IUserSessionProvider, UserSessionProvider>();
        services.AddScoped<IGameEntryService, GameEntryService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountUnitOfWork, AccountUnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGameUnitOfWork, GameUnitOfWork>();

        return services;
    }

    public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseConfiguration>(configuration.GetSection(nameof(DatabaseConfiguration)));
        return services;
    }

    // Redis 설정을 위한 확장 메서드
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfiguration = configuration.GetSection("DatabaseConfiguration:Redis").Value;
        if (string.IsNullOrEmpty(redisConfiguration))
        {
            throw new InvalidOperationException("Redis configuration is not found.");
        }

        var multiplexer = ConnectionMultiplexer.Connect(redisConfiguration);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddStackExchangeRedisCache(options => { options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer); });

        return services;
    }

    // Web API 기본 서비스들을 위한 확장 메서드
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddExceptionHandler<GlobalGameExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT Secret is not configured.");
        }

        var key = Encoding.ASCII.GetBytes(secretKey);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // 개발 환경에서는 false
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }

    // EF Core를 위한 확장 메서드
    public static IServiceCollection AddEntityFramework(this IServiceCollection services, IConfiguration configuration)
    {
        // 데이터베이스 연결 문자열 설정
        services.AddDbContext<AccountDbContext>(options =>
            options.UseMySql(
                configuration["DatabaseConfiguration:AccountDatabase"],
                ServerVersion.AutoDetect(configuration["DatabaseConfiguration:AccountDatabase"])
            ));

        // GameDbContext 추가
        services.AddDbContext<GameDbContext>(options =>
                options.UseMySql(
                        configuration["DatabaseConfiguration:GameDatabase"],
                        ServerVersion.AutoDetect(configuration["DatabaseConfiguration:GameDatabase"])
                    )
                    // 아래 로깅 코드를 추가합니다.
                    .LogTo(Console.WriteLine, LogLevel.Information)
                    .EnableSensitiveDataLogging() // 개발 중에만 사용
        );

        return services;
    }
}