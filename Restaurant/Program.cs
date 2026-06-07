using AutoMapper;
using Restaurant.Services;
using Restaurant.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Unit of Work (voor Repository Pattern)
builder.Services.AddScoped<MailService>();
builder.Services.AddScoped<TemplateMailService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Background scheduler voor welkomstmails
builder.Services.AddHostedService<WelkomstMailScheduler>();

// AutoMapper (voor ViewModel <-> Model mapping)
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddDbContext<RestaurantContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("LocalDBConnection")));

// Identity
builder.Services.AddIdentity<CustomUser, IdentityRole>()
    .AddEntityFrameworkStores<RestaurantContext>()
    .AddDefaultTokenProviders();

// SignalR
builder.Services.AddSignalR();

// Service aanmaken voor identityseeding
builder.Services.AddTransient<IdentitySeeding>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Gebruiker/Login";
    options.AccessDeniedPath = "/Home/Index";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

// Map hub
app.MapHub<OrderHub>("/orderHub");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeding>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    UserManager<CustomUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<CustomUser>>();
    await seeder.IdentitySeedingAsync(userManager, roleManager);
}

app.Run();
