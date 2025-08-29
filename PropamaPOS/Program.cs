using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
    client.BaseAddress = new Uri("http://localhost:5204");
});

builder.Services.AddHttpClient<ProveedorServiceClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5050/");
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapControllers();

app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//app.MapStaticAssets();

//app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
