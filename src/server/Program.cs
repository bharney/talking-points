using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using talking_points.Models;
using talking_points.Repository;

var allowLocalhost = "allowLocalhost";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The secret is stored in the user secrets store
new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
var builtConfig = builder.Configuration;

var keyVaultEndpoint = new Uri(builtConfig["VaultEndpoint"]);
var secretClient = new SecretClient(keyVaultEndpoint, new DefaultAzureCredential());

builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
               .AddEntityFrameworkStores<ApplicationDbContext>()
               .AddDefaultTokenProviders();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IKeywordRepository, KeywordRepository>();
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowLocalhost,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000");
                      });
});
var app = builder.Build();

app.UseCors(allowLocalhost);
app.UseHttpsRedirection();
app.UseAuthorization();
//include controllers
app.MapControllers();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.Run();
