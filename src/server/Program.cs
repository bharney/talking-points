using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using talking_points.Models;
using talking_points.Repository;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using talking_points.Services;

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

builder.Services.AddAuthentication()
    .AddCookie(cfg => cfg.SlidingExpiration = true)
    .AddJwtBearer(cfg =>
    {
        cfg.RequireHttpsMetadata = false;
        cfg.SaveToken = true;

        cfg.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidIssuer = builtConfig["Token:Issuer"],
            ValidAudience = builtConfig["Token:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builtConfig["Token:Key256"]))
        };
    });

// Configure Identity
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IKeywordRepository, KeywordRepository>();
builder.Services.AddScoped<IArticleDetailsSearchClient, ArticleDetailsSearchClient>();
builder.Services.AddScoped<IKeywordsSearchClient, KeywordsSearchClient>();
builder.Services.AddScoped<ICachedUserRepository<ApplicationUser>, CachedUserRepositoryDecorator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<DbSeeder>();
builder.Services.AddRazorPages();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowLocalhost,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000").AllowCredentials().AllowAnyHeader();
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
app.UseAuthentication();
app.Run();
