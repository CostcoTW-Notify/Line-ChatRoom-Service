using LineChatRoomService.Repositories;
using LineChatRoomService.Repositories.Interface;
using LineChatRoomService.Services;
using LineChatRoomService.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Security.Claims;


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
builder.Services.AddScoped<ILineNotifyService, LineNotifyService>();
builder.Services.AddScoped<IChatRoomService, ChatRoomService>();
builder.Services.AddSingleton<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddSingleton<IInventoryCheckRepository, InventoryCheckRepository>();
builder.Services.AddSingleton(c =>
{
    var connStr = Environment.GetEnvironmentVariable("mongo_conn_str")!;
    return new MongoClient(connStr).GetDatabase("LineChatRoom-Service");
});
builder.Services.AddCors(op =>
{
    op.AddPolicy(
        name: "AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin();
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


app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 400;
        Console.Error.WriteLine("Request caught ex: " + ex);
        await context.Response.WriteAsJsonAsync(new
        {
            error_message = ex.Message.ToString()
        });
    }
});
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Map("/api/test2", async (HttpContext context, ClaimsPrincipal principal) =>
{
    return "ok";
});

app.Run();


static void EnsureEnv()
{
    var aesKey = Environment.GetEnvironmentVariable("AES-KEY");
    var aesIv = Environment.GetEnvironmentVariable("AES-IV");
    var lineClientId = Environment.GetEnvironmentVariable("line_client_id");
    var lineClientSecret = Environment.GetEnvironmentVariable("line_client_secret");
    var mongo_connStr = Environment.GetEnvironmentVariable("mongo_conn_str");

    if (new[] { aesKey, aesIv, lineClientId, lineClientSecret, mongo_connStr }.Any(x => string.IsNullOrWhiteSpace(x)))
        throw new Exception("EnvironmentVariable setup fail...");
}