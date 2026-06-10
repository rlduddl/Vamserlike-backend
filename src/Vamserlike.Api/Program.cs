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
// Swagger 우측 상단 Authorize 버튼에서 JWT 토큰 입력 가능하게 유지
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vamserlike.Api",
        Version = "v1",
        Description = "Vamserlike backend API"
    });

    // 현재 설정은 Swagger Authorize 입력칸에 토큰값만 넣으면 됨
    // 예: eyJraWQiOi...
    // Swagger가 실제 요청에는 Authorization: Bearer {token} 형태로 붙여줌
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT 토큰만 입력하세요. 예: eyJraWQiOi...",
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

// 현재 앱이 읽은 Cognito 설정값 출력
// 토큰값은 출력하지 않고, UserPoolId / ClientId만 확인용으로 출력
Console.WriteLine("========== COGNITO CONFIG ==========");
Console.WriteLine($"AWS Region  = {awsRegion}");
Console.WriteLine($"UserPoolId  = {cognitoOptions.UserPoolId}");
Console.WriteLine($"ClientId    = {cognitoOptions.ClientId}");

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

    Console.WriteLine($"JWT Issuer  = {issuer}");
    Console.WriteLine("====================================");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Cognito User Pool issuer
            options.Authority = issuer;

            // Cognito OpenID 설정 주소 명시
            options.MetadataAddress = $"{issuer}/.well-known/openid-configuration";

            // Cognito는 HTTPS라 true 유지
            options.RequireHttpsMetadata = true;

            // 401 원인을 조금 더 자세히 확인하기 위한 옵션
            options.IncludeErrorDetails = true;

            // Cognito 원본 claim 이름을 최대한 그대로 사용
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                // issuer 검증
                ValidateIssuer = true,
                ValidIssuer = issuer,

                // access token은 aud가 없고 client_id가 있음
                // 그래서 audience 검증은 끄고, 아래 OnTokenValidated에서 client_id 직접 검증
                ValidateAudience = false,

                // 만료 시간 검증
                ValidateLifetime = true,

                // 시간 오차 허용
                ClockSkew = TimeSpan.FromMinutes(5),

                // 사용자 식별자는 Cognito sub 사용
                NameClaimType = "sub"
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();

                    if (!string.IsNullOrWhiteSpace(authHeader))
                    {
                        var previewLength = Math.Min(authHeader.Length, 40);
                        //Console.WriteLine("========== JWT MESSAGE RECEIVED ==========");
                        //Console.WriteLine($"Authorization header preview = {authHeader[..previewLength]}...");
                    }
                    else
                    {
                        //Console.WriteLine("========== JWT MESSAGE RECEIVED ==========");
                        //Console.WriteLine("Authorization header is empty.");
                    }

                    return Task.CompletedTask;
                },

                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine("========== JWT AUTH FAILED ==========");
                    Console.WriteLine($"Exception Type = {context.Exception.GetType().FullName}");
                    Console.WriteLine($"Message        = {context.Exception.Message}");

                    if (context.Exception.InnerException != null)
                    {
                        Console.WriteLine($"Inner Type     = {context.Exception.InnerException.GetType().FullName}");
                        Console.WriteLine($"Inner Message  = {context.Exception.InnerException.Message}");
                    }

                    return Task.CompletedTask;
                },

                OnTokenValidated = context =>
                {
                    var principal = context.Principal;

                    var tokenUse = principal?.FindFirst("token_use")?.Value;

                    var clientId =
                        principal?.FindFirst("client_id")?.Value ??
                        principal?.FindFirst("aud")?.Value;

                    var subject = principal?.FindFirst("sub")?.Value;
                    var issuerClaim = principal?.FindFirst("iss")?.Value;


                    if (string.IsNullOrWhiteSpace(tokenUse))
                    {
                        //Console.WriteLine("JWT FAIL REASON = token_use claim is missing.");
                        context.Fail("token_use claim is missing.");
                        return Task.CompletedTask;
                    }

                    if (tokenUse != "access" && tokenUse != "id")
                    {
                        //Console.WriteLine("JWT FAIL REASON = token_use is not access or id.");
                        context.Fail("Only Cognito access/id tokens are allowed.");
                        return Task.CompletedTask;
                    }

                    if (!string.IsNullOrWhiteSpace(cognitoOptions.ClientId) &&
                        !string.Equals(clientId, cognitoOptions.ClientId, StringComparison.Ordinal))
                    {
                        //Console.WriteLine("JWT FAIL REASON = client_id mismatch.");
                        context.Fail("Invalid Cognito app client.");
                        return Task.CompletedTask;
                    }

                    //Console.WriteLine("JWT VALIDATION SUCCESS.");

                    return Task.CompletedTask;
                },

                OnChallenge = context =>
                {
                    Console.WriteLine("========== JWT CHALLENGE ==========");
                    Console.WriteLine($"Error            = {context.Error}");
                    Console.WriteLine($"ErrorDescription = {context.ErrorDescription}");

                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    Console.WriteLine("WARNING: Cognito UserPoolId is empty. Default JWT bearer config is used.");

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

// Production / EKS / ALB 환경에서도 Swagger 표시
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vamserlike.Api v1");
    options.RoutePrefix = "swagger";
});

// Backend ALB 루트 주소로 접속하면 Swagger로 이동
// http://Backend_ALB/ -> http://Backend_ALB/swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Docker/EKS/ALB 환경에서는 HTTP Health Check를 받을 수 있으므로 비활성화
// app.UseHttpsRedirection();

app.UseCors("UnityWeb");

// JWT 인증 미들웨어
// 이게 빠지면 [Authorize] 붙은 Player API가 토큰을 못 읽음
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();