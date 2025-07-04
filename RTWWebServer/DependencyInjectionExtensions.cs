using Microsoft.EntityFrameworkCore;
using RTWWebServer.Authentication;
using RTWWebServer.Cache;
using RTWWebServer.Database;
using RTWWebServer.Repository;
using RTWWebServer.Service;

namespace RTWWebServer;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        services.AddSingleton<IGuidGenerator, GuidGenerator>();
        services.AddSingleton<IAuthTokenGenerator, AuthTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRemoteCache, RedisRemoteCache>(); // Redis 클라이언트는 스레드 안전
        services.AddSingleton<IRemoteCacheKeyGenerator, RemoteCacheKeyGenerator>();

        // EF Core로 마이그레이션 완료 - IDatabaseContextProvider 제거
        services.AddScoped<IRequestScopedLocalCache, RequestScopedLocalCache>(); // 요청 범위 캐시
        services.AddScoped<ICacheManager, CacheManager>(); // 캐시 관리자, 요청 범위 캐시 의존

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILoginService, LoginService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddConfigurations(this IServiceCollection services)
    {
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

        return services;
    }
}