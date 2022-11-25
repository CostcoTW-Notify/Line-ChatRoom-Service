using Google.Cloud.PubSub.V1;
using LineChatRoomService.Models.Mongo;
using LineChatRoomService.Repositories;
using LineChatRoomService.Repositories.Interface;
using LineChatRoomService.Services;
using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Diagnostics;
using System.Text.Json;

EnsureEnv();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        RequireAudience = false,
                        ValidateAudience = false,
                        IssuerSigningKey = new SymmetricSecurityKey(File.ReadAllBytes("HS256.key"))
                    };
                    options.IncludeErrorDetails = true;
                })
                ;
builder.Services.AddHttpClient("default", options =>
{
    // For GCP Cloud run
    options.DefaultRequestHeaders.Add("User-Agent", "python-httpx/0.23.0 CostcoTW-Notify/0.1");
    options.DefaultRequestHeaders.Add("Accept", "application/json");
    options.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection().PersistKeysToMongoDb(c => c.GetRequiredService<IMongoDatabase>());
builder.Services.AddScoped<ILineNotifyService>(c =>
{
    var lineClientId = Environment.GetEnvironmentVariable("line_client_id");
    var lineClientSecret = Environment.GetEnvironmentVariable("line_client_secret");
    var service = new LineNotifyService(c.GetRequiredService<ILogger<LineNotifyService>>(),
                                        lineClientId!,
                                        lineClientSecret!,
                                        c.GetRequiredService<IHttpClientFactory>(),
                                        c.GetRequiredService<IHttpContextAccessor>(),
                                        c.GetRequiredService<IDataProtectionProvider>()
                                        );
    return service;
});

builder.Services.AddScoped<IChatRoomService, ChatRoomService>();
builder.Services.AddSingleton<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddSingleton<IInventoryCheckRepository, InventoryCheckRepository>();
builder.Services.AddSingleton<IServerLogRepository, ServerLogRepository>();

builder.Services.AddSingleton(c =>
{
    var connStr = Environment.GetEnvironmentVariable("mongo_conn_str")!;
    return new MongoClient(connStr).GetDatabase("LineChatRoom-Service");
});


builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

builder.Services.AddScoped<IIntergrationEventService>(c =>
{
    var topic = TopicName.Parse(Environment.GetEnvironmentVariable("gcp_intergration_topic_path"));
    var publisher = PublisherClient.Create(topic);
    var service = new IntergrationEventService(publisher);
    return service;
});

builder.Services.AddCors(op =>
{
    op.AddPolicy(
        name: "Allow-GitHub.io-SPA-App",
        policy =>
        {
            policy.WithOrigins(
                "https://costcotw-notify.github.io",
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://192.168.2.6:5173"
                );
            policy.AllowAnyMethod();
            policy.AllowAnyHeader();
        });
});

// Require All endpoint
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});


var app = builder.Build();

// Error handle and logger
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    string? error = null;

    var startTime = DateTime.Now;
    var sw = Stopwatch.StartNew();
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        error = ex.ToString();
        context.Response.StatusCode = 500;
        Console.Error.WriteLine("Request caught ex: " + ex);
    }

    // Log Req and Resp
    try
    {
        string? requestBody = null;
        // Reset buffer offset
        context.Request.Body.Position = 0;
        if (context.Request.Headers.ContentType == "application/json")
            requestBody = JsonSerializer.Serialize(await context.Request.ReadFromJsonAsync<object>(), new JsonSerializerOptions
            {
                WriteIndented = true,
            });


        var req = new Request
        {
            Url = context.Request.Path,
            Method = context.Request.Method,
            Body = requestBody,
        };

        var log = new ServerLog
        {
            StartTime = startTime,
            ProcessTime = (uint)sw.ElapsedMilliseconds,
            Error = error,
            Request = req,
            ResponseStatus = context.Response.StatusCode
        };

        var repo = context.RequestServices.GetRequiredService<IServerLogRepository>();
        await repo.InsertLog(log);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
});


// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Allow-GitHub.io-SPA-App");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();


static void EnsureEnv()
{
    var envs = Environment.GetEnvironmentVariables();

    if (!envs.Contains("gcp_intergration_topic_path"))
        throw new Exception("env: gcp_intergration_topic_path not setup");
    if (!envs.Contains("line_client_id"))
        throw new Exception("env: line_client_id not setup");
    if (!envs.Contains("line_client_secret"))
        throw new Exception("env: line_client_secret not setup");
    if (!envs.Contains("mongo_conn_str"))
        throw new Exception("env: mongo_conn_str not setup");
    if (!envs.Contains("GOOGLE_APPLICATION_CREDENTIALS"))
        throw new Exception("env: GOOGLE_APPLICATION_CREDENTIALS not setup");
}