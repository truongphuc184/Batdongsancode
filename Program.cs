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

// Tự động nhận diện SQL Server Instance khả dụng trên máy (SQL Server chuẩn '.', 'localhost', 'SQLEXPRESS' hoặc 'LocalDB')
string connectionString = ResolveSqlConnectionString(configuration);

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

// Tự động khởi tạo Database và nạp dữ liệu mẫu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Tự động tạo Database và các bảng (Users, Listings, Leads, Reports...) nếu chưa tồn tại
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(">>> [DATABASE OK] Đã kết nối cơ sở dữ liệu và nạp dữ liệu mẫu thành công!");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("==================================================================");
        Console.WriteLine(">>> [LỖI KẾT NỐI DATABASE]");
        Console.WriteLine($">>> Chi tiết lỗi: {ex.Message}");
        Console.WriteLine(">>> HƯỚNG DẪN KHẮC PHỤC TRÊN MÁY BẠN:");
        Console.WriteLine("    1. Mở file 'appsettings.json' kiểm tra dòng 'DefaultConnection'.");
        Console.WriteLine("    2. Kiểm tra tên SQL Server trong SSMS (thường là '.' hoặc 'localhost' hoặc 'localhost\\SQLEXPRESS').");
        Console.WriteLine("    3. Đảm bảo dịch vụ SQL Server (MSSQLSERVER hoặc SQLEXPRESS) trong Services.msc đang Chạy (Running).");
        Console.WriteLine("==================================================================");
        Console.ResetColor();
    }
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

// ====================================================================
// HÀM TỰ ĐỘNG PHÁT HIỆN SQL SERVER ĐANG CHẠY TRÊN MÁY TÍNH
// ====================================================================
static string ResolveSqlConnectionString(IConfiguration config)
{
    var configured = config.GetConnectionString("DefaultConnection") ?? "";
    var candidateServers = new List<string>();

    // 1. Lấy server chỉ định trong appsettings.json nếu có
    if (!string.IsNullOrWhiteSpace(configured))
    {
        var match = System.Text.RegularExpressions.Regex.Match(configured, @"Server=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success) candidateServers.Add(match.Groups[1].Value.Trim());
    }

    // 2. Danh sách các instance SQL Server chuẩn & phổ biến
    candidateServers.AddRange(new[] { ".", "localhost", "localhost\\SQLEXPRESS", "(localdb)\\mssqllocaldb" });

    foreach (var server in candidateServers.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        // Thử kết nối tới instance đang chạy với timeout 1.5 giây
        var testStr = $"Server={server};Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=2;";
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(testStr);
            conn.Open();
            var finalConn = $"Server={server};Database=batdongsan;Trusted_Connection=True;TrustServerCertificate=True;";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($">>> [SQL SERVER TỰ ĐỘNG NHẬN DIỆN] Đang kết nối tới SQL Server: '{server}'");
            Console.ResetColor();
            return finalConn;
        }
        catch
        {
            // Bỏ qua nếu server này không chạy trên máy, tiếp tục tìm server khác
        }
    }

    // Nếu không ping được instance nào thì dùng cấu hình mặc định
    return string.IsNullOrWhiteSpace(configured) 
        ? "Server=.;Database=batdongsan;Trusted_Connection=True;TrustServerCertificate=True;" 
        : configured;
}

