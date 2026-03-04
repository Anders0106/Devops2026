using Chirp.Core.Classes;
using Chirp.Razor;
using Chirp.Repositories.Interfaces;
using Chirp.Repositories.Repositories;
using Chirp.Services;
using Chirp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<ICheepService, CheepService>();
builder.Services.AddScoped<ICheepRepository, CheepRepository>();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChirpDBContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Chirp.Razor")));

builder.Services.AddDefaultIdentity<Author>(options =>
{
	options.SignIn.RequireConfirmedAccount = false;
	options.Password.RequireDigit = false;
	options.Password.RequireLowercase = false;
	options.Password.RequireUppercase = false;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequiredLength = 1;
	options.Password.RequiredUniqueChars = 1;
	options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
}).AddEntityFrameworkStores<ChirpDBContext>();

builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 1000);


builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// string? githubClientId = builder.Configuration["GITHUBCLIENTID"];
// string? githubClientSecret = builder.Configuration["GITHUBCLIENTSECRET"];
// if (string.IsNullOrEmpty(githubClientId) || string.IsNullOrEmpty(githubClientSecret))
// {
// 	throw new Exception("GitHub Client ID and Client Secret must be set in the configuration.");
// }

builder.Services.AddAuthentication()
	.AddCookie();
	// .AddGitHub(o =>
	// {
	// 	o.ClientId = githubClientId; // Need to default to something ??
	// 	o.ClientSecret = githubClientSecret;
	// 	o.Scope.Add("user:email");
	// 	// o.CallbackPath = "/signin-github";
	// });


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChirpDBContext>();
    context.Database.Migrate();
}

Console.WriteLine(builder.Configuration.GetConnectionString("DefaultConnection"));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{

	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
else
{
	app.UseMigrationsEndPoint();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();

public partial class Program { }