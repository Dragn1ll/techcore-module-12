using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "issuer",
            ValidateAudience = true,
            ValidAudience = "audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "super_secret_key_which_long_and_secure_which_can_get_from_internet"u8.ToArray()),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Role", "Admin");
    });

builder.Services.AddHttpClient("BookService", client =>
{
    client.BaseAddress = new Uri("http://book-service:80/");
});

builder.Services.AddHttpClient("ReviewService", client =>
{
    client.BaseAddress = new Uri("http://review-service:80/");
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault().AddService("book-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddZipkinExporter(o =>
        {
            o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
        })
    )
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/details/{id}", async (int id, IHttpClientFactory httpClientFactory) =>
{
    var booksClient = httpClientFactory.CreateClient("BookService");
    var reviewsClient = httpClientFactory.CreateClient("ReviewService");

    var bookResponse = await booksClient.GetAsync($"/api/books/{id}");
    var reviewResponse = await reviewsClient.GetAsync($"/api/reviews/book/{id}");

    bookResponse.EnsureSuccessStatusCode();
    reviewResponse.EnsureSuccessStatusCode();

    var bookJson = await bookResponse.Content.ReadAsStringAsync();
    var reviewsJson = await reviewResponse.Content.ReadAsStringAsync();

    return Results.Ok(new
    {
        book = System.Text.Json.JsonSerializer.Deserialize<object>(bookJson),
        reviews = System.Text.Json.JsonSerializer.Deserialize<object>(reviewsJson)
    });
});

app.MapReverseProxy()
    .RequireAuthorization();

app.Run();