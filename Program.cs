using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using RecruitmentTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICvTextExtractor, CvTextExtractor>();
builder.Services.AddScoped<ICandidateFilterService, CandidateFilterService>();
builder.Services.AddScoped<IAiCvScreeningService, AiCvScreeningService>();

var app = builder.Build();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Development database bootstrap. If the old database is missing a table
    // introduced by this version, recreate it once with the complete schema.
    if (await db.Database.CanConnectAsync())
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                CASE WHEN OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NULL THEN 1 ELSE 0 END,
                CASE WHEN OBJECT_ID(N'[dbo].[Applications]', N'U') IS NULL THEN 1 ELSE 0 END,
                CASE WHEN COL_LENGTH(N'[dbo].[Candidates]', N'ApplicationUserId') IS NULL THEN 1 ELSE 0 END";

        await using var reader = await command.ExecuteReaderAsync();
        var needsRecreate = false;
        if (await reader.ReadAsync())
            needsRecreate = reader.GetInt32(0) == 1 || reader.GetInt32(1) == 1 || reader.GetInt32(2) == 1;

        await reader.CloseAsync();
        await connection.CloseAsync();

        if (needsRecreate)
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    await db.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
