using RTWWebServer;
using RTWWebServer.Middlewares;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add JSON files for master data before getting configuration
var characterMasterPath = Path.Combine(builder.Environment.ContentRootPath, "MasterDatas", "CharacterMaster.json");
builder.Configuration.AddJsonFile(characterMasterPath, optional: false, reloadOnChange: true);

var furnitureMasterPath = Path.Combine(builder.Environment.ContentRootPath, "MasterDatas", "FurnitureMaster.json");
builder.Configuration.AddJsonFile(furnitureMasterPath, optional: false, reloadOnChange: true);

var roomGradeMasterPath = Path.Combine(builder.Environment.ContentRootPath, "MasterDatas", "RoomGradeMaster.json");
builder.Configuration.AddJsonFile(roomGradeMasterPath, optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Configuration;

builder.Services.AddRedisCache(configuration);
builder.Services.AddWebApiServices();
builder.Services.AddJwtAuthentication(configuration, builder.Environment.IsDevelopment());
builder.Services.AddCustomServices();
builder.Services.AddRepositories();
builder.Services.AddConfigurations(configuration);
builder.Services.AddEntityFramework(configuration, builder.Environment.IsDevelopment());
builder.Services.AddMasterDataSystem(configuration);
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

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