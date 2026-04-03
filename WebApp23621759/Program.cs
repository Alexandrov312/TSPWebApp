using Microsoft.AspNetCore.Authentication.Cookies;
using WebApp23621759.Database;
using WebApp23621759.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();

//Логинът ще се пази чрез cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options => { options.LoginPath = "/Login"; });
//Ако някой се опита да отвори защитена страница без да е логнат, прати го към /Login

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
	DatabaseInitializer.Initialize(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
