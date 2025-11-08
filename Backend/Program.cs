using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient();
            
            // Register Shelter Services
            builder.Services.AddHttpClient<AirRaidShelterService>();
            builder.Services.AddHttpClient<NaturalDisasterShelterService>();
            builder.Services.AddScoped<UnifiedShelterService>();
            
            // Register Google Maps Service
            builder.Services.AddHttpClient<GoogleMapsService>();
            
            // Configure SQLite Database for Shelter Cache
            builder.Services.AddDbContext<ShelterDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("ShelterCache") 
                    ?? "Data Source=shelters.db"));
            
            // Register Repository and Cached Service
            builder.Services.AddScoped<ShelterRepository>();
            builder.Services.AddScoped<CachedUnifiedShelterService>();
            
            // Add Memory Cache
            builder.Services.AddMemoryCache();

            var app = builder.Build();
            
            // Initialize Database
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
                db.Database.EnsureCreated();
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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
