using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vamserlike.Api.Configurations;
using Vamserlike.Api.Repositories;
using Vamserlike.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CognitoOptions>(
    builder.Configuration.GetSection("Cognito"));

builder.Services.Configure<DynamoDbOptions>(
    builder.Configuration.GetSection("DynamoDb"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vamserlike API",
        Version = "v1"
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

var cognitoOptions =
    builder.Configuration.GetSection("Cognito").Get<CognitoOptions>()
    ?? new CognitoOptions();

var awsRegion =
    builder.Configuration["AWS:Region"] ??
    cognitoOptions.Region ??
    "ap-northeast-2";

var regionEndpoint = RegionEndpoint.GetBySystemName(awsRegion);

builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
    new AmazonCognitoIdentityProviderClient(
        new AnonymousAWSCredentials(),
        regionEndpoint));

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    new AmazonDynamoDBClient(regionEndpoint));

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
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();
}

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPlayerRepository, DynamoPlayerRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("UnityWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();