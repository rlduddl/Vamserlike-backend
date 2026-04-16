using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPlayerRepository, DynamoPlayerRepository>();
builder.Services.AddScoped<IPlayerService, PlayerService>();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("UnityWeb");
app.UseAuthorization();
app.MapControllers();

app.Run();