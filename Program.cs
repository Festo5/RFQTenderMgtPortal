using Microsoft.AspNetCore.Authentication.Cookies;
using RecruitmentPortal.Services;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Register HTTP Client for OData with proper base URL
builder.Services.AddHttpClient("BusinessCentral", client =>
{
    var baseUrl = builder.Configuration["BusinessCentral:OData:BaseUrl"].TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    Credentials = new NetworkCredential(
        builder.Configuration["BusinessCentral:OData:Username"],
        builder.Configuration["BusinessCentral:OData:Password"]
    ),
    PreAuthenticate = true,
    UseDefaultCredentials = false
});

// Register SOAP services
builder.Services.AddScoped<BusinessCentralAuthService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    if (string.IsNullOrEmpty(config["BusinessCentral:SOAP:AuthEndpoint"]) ||
        string.IsNullOrEmpty(config["BusinessCentral:SOAP:RFQEndpoint"]))
    {
        throw new ApplicationException("Missing required SOAP endpoint configuration");
    }
    return new BusinessCentralAuthService(config);
});

// Register application services
builder.Services.AddScoped<BusinessCentralJobService>();
builder.Services.AddScoped<IBusinessCentralRfqService, BusinessCentralRfqService>();
builder.Services.AddScoped<IBusinessCentralTenderService, BusinessCentralTenderService>();
builder.Services.AddScoped<IBusinessCentralImprestItemService, BusinessCentralImprestItemService>();
builder.Services.AddSingleton<PasswordHasher>();

// Register Color Settings Service
builder.Services.AddScoped<IColorSettingsService, ColorSettingsService>();

// Add framework services
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configure resumes directory
var resumesPath = Path.Combine(app.Environment.WebRootPath, "resumes");
if (!Directory.Exists(resumesPath))
{
    Directory.CreateDirectory(resumesPath);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();