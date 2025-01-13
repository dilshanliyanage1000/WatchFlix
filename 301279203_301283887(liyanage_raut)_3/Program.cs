using _301279203_301283887_liyanage_raut__3;
using _301279203_301283887_liyanage_raut__3.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Configuration;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Configuration.AddSystemsManager("/MovieDb",
    new Amazon.Extensions.NETCore.Setup.AWSOptions { Region = Amazon.RegionEndpoint.USEast1 });

// Add RDS connection
var dbConnectionString = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Connection2RDS"));
dbConnectionString.UserID = builder.Configuration["dbUser"];
dbConnectionString.Password = builder.Configuration["dbPassword"];

builder.Services.AddDbContext<MovieappContext>(options => options.UseSqlServer(dbConnectionString.ConnectionString));

// Add AWS services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions("AWS"));
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

