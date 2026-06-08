using Amazon;
using Amazon.CognitoIdentityProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Repositories;
using Vamserlike.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Cognito 설정 바인딩
// appsettings.json 또는 appsettings.Development.json의 "Cognito" 섹션을 읽음
builder.Services.Configure<CognitoOptions>(
    builder.Configuration.GetSection("Cognito"));

// Game 설정 바인딩
// 캐릭터 해금 비용 CharacterUnlockCosts 유지
builder.Services.Configure<GameOptions>(
    builder.Configuration.GetSection("Game"));

// MySQL 설정 바인딩
// appsettings.json 또는 환경변수 MySql__ConnectionString 값을 읽음
builder.Services.Configure<MySqlOptions>(
    builder.Configuration.GetSection("MySql"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger 설정
// Swagger 우측 상단 Authorize 버튼에서 Bearer 토큰 입력 가능하게 유지
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vamserlike.Api",
        Version = "v1",
        Description = "Vamserlike backend API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Authorization 헤더에 Bearer {token} 입력",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
var awsRegion =
    builder.Configuration["AWS:Region"] ??
    cognitoOptions.Region ??
    "ap-northeast-2";

var regionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);

// Cognito 클라이언트 등록
// 로컬에서는 aws configure 자격증명 사용
// EC2/EKS에서는 IAM Role 사용
// 기존 AuthService / DevService 흐름 유지
builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
    new AmazonCognitoIdentityProviderClient(regionEndpoint));

// Cognito JWT 인증 설정
// 기존 토큰 검증 흐름 유지
if (!string.IsNullOrWhiteSpace(cognitoOptions.UserPoolId))
{
    var issuer = $"https://cognito-idp.{awsRegion}.amazonaws.com/{cognitoOptions.UserPoolId}";

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = issuer;
            options.RequireHttpsMetadata = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateLifetime = true,

                // Cognito access token/id token 구조를 둘 다 받을 수 있게 기존 방식 유지
                ValidateAudience = false,
                NameClaimType = "sub"
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var principal = context.Principal;

                    var tokenUse = principal?.FindFirst("token_use")?.Value;

                    var clientId =
                        principal?.FindFirst("client_id")?.Value ??
                        principal?.FindFirst("aud")?.Value;

                    if (string.IsNullOrWhiteSpace(tokenUse))
                    {
                        context.Fail("token_use claim is missing.");
                        return Task.CompletedTask;
                    }

                    if (tokenUse != "access" && tokenUse != "id")
                    {
                        context.Fail("Only Cognito access/id tokens are allowed.");
                        return Task.CompletedTask;
                    }

                    if (!string.IsNullOrWhiteSpace(cognitoOptions.ClientId) &&
                        !string.Equals(clientId, cognitoOptions.ClientId, StringComparison.Ordinal))
                    {
                        context.Fail("Invalid Cognito app client.");
                        return Task.CompletedTask;
                    }

                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    // UserPoolId가 비어있는 개발 환경에서 앱 자체는 뜨도록 기본 JWT 설정
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();
}

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

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vamserlike.Api v1");
    });
}

// Docker/EKS/ALB 환경에서는 HTTP Health Check를 받을 수 있으므로 비활성화
// app.UseHttpsRedirection();

app.UseCors("UnityWeb");

// JWT 인증 미들웨어
// 이게 빠지면 [Authorize] 붙은 Player API가 토큰을 못 읽음
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();