using Chirp.Core.Classes;
using Chirp.Razor;
using Chirp.Repositories.Interfaces;
using Chirp.Repositories.Repositories;
using Chirp.Services;
using Chirp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<ICheepService, CheepService>();
builder.Services.AddScoped<ICheepRepository, CheepRepository>();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChirpDBContext>(options => options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Chirp.Razor")));

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
	using var context = scope.ServiceProvider.GetService<ChirpDBContext>();
	if (context == null)
	{
		throw new Exception("Could not get ChirpDBContext from service provider.");
	}

	// If tables already exist but are not tracked in migration history (e.g. from a
	// previous EnsureCreated call or a stale Docker volume), mark pending migrations
	// as applied so Migrate() does not try to recreate existing tables.
	var pending = context.Database.GetPendingMigrations().ToList();
	if (pending.Count > 0)
	{
		var dbConn = context.Database.GetDbConnection();
		dbConn.Open();
		using var checkCmd = dbConn.CreateCommand();
		checkCmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'AspNetRoles'";
		var tablesExist = Convert.ToInt64(checkCmd.ExecuteScalar()!) > 0;
		dbConn.Close();

		if (tablesExist)
		{
			foreach (var migration in pending)
			{
				context.Database.ExecuteSqlRaw(
					"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") SELECT {0}, '8.0.8' WHERE NOT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = {1})",
					migration, migration);
			}
		}
	}

	context.Database.Migrate();

	var sqlitePath = builder.Configuration["Chirp:SqliteMigrationPath"] ?? "Assets/chirp.db";
	SqliteToPostgresMigrator.MigrateIfNeededAsync(context, sqlitePath).GetAwaiter().GetResult();

	var authors = DbInitializer.SeedDatabase(context);
	DbInitializer.SetAuthorPasswords(authors, scope.ServiceProvider);
}

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