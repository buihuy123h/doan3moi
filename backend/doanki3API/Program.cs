using System.Text;
using doanki3API.Data;
using doanki3API.Services;
using doanki3API.Services.Auth;
using doanki3API.Services.Chat;
using doanki3API.Services.Email;
using doanki3API.Services.Notifications;
using doanki3API.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Nexus API server entry point.
var builder = WebApplication.CreateBuilder(args);

// EventLog trên Windows có thể làm request lỗi nếu tiến trình không có quyền ghi.
// Console và Debug là đủ cho API chạy local/container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Giá trị không hợp lệ"
                            : error.ErrorMessage)
                        .ToArray());

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                message = "Dữ liệu gửi lên không hợp lệ",
                errors
            });
        };
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("MyAppDbContext")
    ?? throw new InvalidOperationException(
        "Không tìm thấy connection string 'MyAppDbContext'.");

builder.Services.AddDbContext<MyAppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<TechnicalDashboardService>();
builder.Services.AddScoped<AccountIdGenerator>();
builder.Services.AddScoped<TechnicalOrderService>();
builder.Services.AddScoped<ConnectionService>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<BillingService>();

// Module chat dùng MongoDB (không phải SQL Server) — xem Services/Chat/ChatService.cs.
// Singleton vì MongoClient tự quản lý connection pool, tạo lại mỗi request là lãng phí.
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.AddSingleton<ChatService>();

// Gửi email (Gmail SMTP) + mã OTP đăng nhập cho khách hàng.
// OTP lưu trong MongoDB với TTL index nên mã hết hạn tự biến mất, không cần job dọn.
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<LoginOtpService>();
builder.Services.AddSingleton<JwtTokenService>();

// Thông báo real-time: SignalR giữ kết nối WebSocket với trình duyệt, NotificationService
// lưu lịch sử vào MongoDB rồi đẩy ngay xuống đúng người nhận (xem Hubs/NotificationHub.cs).
builder.Services.AddSignalR();
builder.Services.AddSingleton<NotificationService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.Path);

app.Run();
