using Microsoft.Extensions.DependencyInjection;
using RTWWebServer.Authentication;
using RTWWebServer.Cache;
using RTWWebServer.Database;
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

        services.AddScoped<IDatabaseContextProvider, MySqlDatabaseContextProvider>(); // DB 컨텍스트는 요청별 관리
        services.AddScoped<IRequestScopedLocalCache, RequestScopedLocalCache>(); // 요청 범위 캐시
        services.AddScoped<ICacheManager, CacheManager>(); // 캐시 관리자, 요청 범위 캐시 의존

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILoginService, LoginService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddConfigurations(this IServiceCollection services)
    {
        return services;
    }
}