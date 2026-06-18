using RTWWebServer;
using RTWWebServer.Middlewares;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

var masterDataDirectory = Path.Combine(builder.Environment.ContentRootPath, "MasterDatas");

builder.Services.AddRedisCache(configuration);
builder.Services.AddWebApiServices();
builder.Services.AddJwtAuthentication(configuration, builder.Environment.IsDevelopment());
builder.Services.AddCustomServices();
builder.Services.AddRepositories();
builder.Services.AddConfigurations(configuration);
builder.Services.AddEntityFramework(configuration, builder.Environment.IsDevelopment());
builder.Services.AddMasterDataSystem(masterDataDirectory);
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// 마스터 데이터를 시작 시점에 로드·검증한다. 싱글톤 provider는 지연 생성되므로 여기서 해석해
// 잘못된 데이터/누락 파일이면 요청을 받기 전에 기동을 실패시킨다(fail-fast).
app.Services.GetRequiredService<RTWWebServer.Providers.MasterData.IMasterDataProvider>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(opt => { });
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLockingMiddleware>();
// 오케스트레이터/로드밸런서용 라이브니스 프로브. FallbackPolicy(전역 인증)를 우회하도록 익명 허용.
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.Run();