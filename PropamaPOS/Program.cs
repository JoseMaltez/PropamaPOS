using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var defaultCulture = new CultureInfo("es-GT");

CultureInfo customCulture = (CultureInfo)defaultCulture.Clone();
customCulture.NumberFormat.NumberDecimalSeparator = ".";
customCulture.NumberFormat.CurrencyDecimalSeparator = ".";

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(customCulture),
    SupportedCultures = new List<CultureInfo> { customCulture },
    SupportedUICultures = new List<CultureInfo> { customCulture }
};

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";   // Formulario si no se esta logueado
        options.AccessDeniedPath = "/Account/AccessDenied"; // Formulario si no tiene permisos
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // tiempo de sesión
    });


builder.Services.AddTransient<EmailServiceClient>();

builder.Services.AddHttpClient("EmailService", client =>
{
    client.BaseAddress = new Uri("http://correopropama.somee.com");
});


builder.Services.AddControllersWithViews(options =>
{
    // Aplica autorización global
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddAuthorization(); //Manejar roles y permisos

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

//app.MapStaticAssets();

//app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
