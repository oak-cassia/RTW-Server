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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using RTWWebServer.Authentication;
using RTWWebServer.Providers.MasterData;

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
        services.AddSingleton<IUserSessionProvider, UserSessionProvider>();

        // 기존 캐시 서비스들을 어댑터 기반으로 교체
        services.AddScoped<IRequestScopedLocalCache, RequestScopedLocalCache>(); // 요청 범위 캐시
        services.AddScoped<ICacheManager, CacheManager>(); // 캐시 관리자, 요청 범위 캐시 의존

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IGameEntryService, GameEntryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICharacterGachaService, CharacterGachaService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlayerCharacterRepository, PlayerCharacterRepository>();

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
        services.AddSwaggerGen(options =>
        {
            // Swagger UI에서 인증이 필요한 API를 테스트할 수 있도록 보안 스킴을 정의한다
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT 토큰 (/Login/login 또는 /Login/guestLogin에서 발급)"
            });
            options.AddSecurityDefinition("SessionUserId", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = SessionAuthenticationDefaults.UserIdHeaderName,
                Description = "세션 인증 사용자 ID (/Game/enter에서 발급)"
            });
            options.AddSecurityDefinition("SessionAuthToken", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = SessionAuthenticationDefaults.AuthTokenHeaderName,
                Description = "세션 인증 토큰 (/Game/enter에서 발급)"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "SessionUserId" } }] = [],
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "SessionAuthToken" } }] = []
            });
        });
        services.AddExceptionHandler<GlobalGameExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT Secret is not configured.");
        }

        // JwtTokenProvider(토큰 발급)와 동일하게 UTF8 사용 — 인코딩 불일치 방지
        var key = Encoding.UTF8.GetBytes(secretKey);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !isDevelopment; // 개발 환경에서만 false
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
            })
            .AddScheme<SessionAuthenticationOptions, SessionAuthenticationHandler>(SessionAuthenticationDefaults.SchemeName, null);

        services.AddAuthorization(options =>
        {
            // [Authorize]/[AllowAnonymous]가 없는 엔드포인트는 기본적으로 인증을 요구한다 (deny by default).
            // 새 컨트롤러를 추가할 때 인증 등록을 잊어도 공개 API가 되지 않는다.
            options.FallbackPolicy = new AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme,
                    SessionAuthenticationDefaults.SchemeName)
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    // EF Core를 위한 확장 메서드
    public static IServiceCollection AddEntityFramework(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // 데이터베이스 연결 문자열 설정
        services.AddDbContext<AccountDbContext>(options =>
            options.UseMySql(
                configuration["DatabaseConfiguration:AccountDatabase"],
                ServerVersion.AutoDetect(configuration["DatabaseConfiguration:AccountDatabase"])
            ));

        // GameDbContext 추가
        services.AddDbContext<GameDbContext>(options =>
        {
            options.UseMySql(
                configuration["DatabaseConfiguration:GameDatabase"],
                ServerVersion.AutoDetect(configuration["DatabaseConfiguration:GameDatabase"])
            );

            // SQL 콘솔 출력과 민감 데이터(파라미터 값) 로깅은 개발 환경에서만 사용
            if (isDevelopment)
            {
                options.LogTo(Console.WriteLine, LogLevel.Information)
                    .EnableSensitiveDataLogging();
            }
        });

        return services;
    }

    // Master Data 시스템을 위한 확장 메서드
    public static IServiceCollection AddMasterDataSystem(this IServiceCollection services, IConfiguration configuration)
    {
        // Master data options 설정
        services.AddOptions<MasterDataOptions>()
            .Bind(configuration)
            .ValidateOnStart();

        // 옵션 시스템이 검증기를 인식하려면 IValidateOptions<T>로 등록해야 한다.
        // 구체 타입으로만 등록하면 ValidateOnStart()가 아무것도 검증하지 않는다.
        services.AddSingleton<IValidateOptions<MasterDataOptions>, MasterDataOptionsValidator>();
        services.AddSingleton<IMasterDataProvider, MasterDataProvider>();

        return services;
    }
}