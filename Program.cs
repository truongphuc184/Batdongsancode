using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Batdongsancode.Data;
using Batdongsancode.Helpers;
using Batdongsancode.Services.Interfaces;
using Batdongsancode.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration
var configuration = builder.Configuration;

// DB Context (MySQL)
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT Authentication
var jwtSettings = configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization policies based on Role claim
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireBuyer", policy => policy.RequireClaim("role", "Buyer"));
    options.AddPolicy("RequireSeller", policy => policy.RequireClaim("role", "Seller"));
    options.AddPolicy("RequireBroker", policy => policy.RequireClaim("role", "Broker"));
    options.AddPolicy("RequireAgency", policy => policy.RequireClaim("role", "Agency"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireClaim("role", "Admin"));
});

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IListingService, ListingService>();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS - allow frontend to call API (phải đặt trước Auth)
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Tắt HttpsRedirection ở local dev để không làm rơi rớt header Authorization khi gọi fetch giữa http/https
// app.UseHttpsRedirection();

// Serve static files from wwwroot (frontend HTML)
app.UseDefaultFiles(); // serves index.html as default page
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

