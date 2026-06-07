using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Repositories;
using Vamserlike.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Cognito 설정 바인딩
// appsettings.json 또는 appsettings.Development.json의 "Cognito" 섹션을 읽음
builder.Services.Configure<CognitoOptions>(
    builder.Configuration.GetSection("Cognito"));

// MySQL 설정 바인딩
// appsettings.json 또는 환경변수 MySql__ConnectionString 값을 읽음
builder.Services.Configure<MySqlOptions>(
    builder.Configuration.GetSection("MySql"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("UnityWeb", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Cognito 옵션 읽기
var cognitoOptions =
    builder.Configuration.GetSection("Cognito").Get<CognitoOptions>()
    ?? new CognitoOptions();

// AWS 리전 설정
// AWS:Region 값이 있으면 우선 사용하고, 없으면 Cognito.Region 사용
var awsRegion =
    builder.Configuration["AWS:Region"] ??
    cognitoOptions.Region ??
    "ap-northeast-2";

var regionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);

// Cognito 클라이언트 등록
// 기존 로그인/회원가입/인증코드 확인 기능은 그대로 Cognito 사용
builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
    new AmazonCognitoIdentityProviderClient(
        new AnonymousAWSCredentials(),
        regionEndpoint));

// 서비스 등록
builder.Services.AddScoped<IAuthService, AuthService>();

// 기존 DynamoPlayerRepository 대신 MySqlPlayerRepository 사용
builder.Services.AddScoped<IPlayerRepository, MySqlPlayerRepository>();

builder.Services.AddScoped<IPlayerService, PlayerService>();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Docker/EKS 환경에서는 ALB 뒤에서 HTTP로 Health Check를 받을 수 있으므로
// HTTPS 강제 리다이렉트는 일단 비활성화
// app.UseHttpsRedirection();

app.UseCors("UnityWeb");

app.UseAuthorization();

app.MapControllers();

app.Run();