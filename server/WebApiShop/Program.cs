using System.Text;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Repositories;
using Services;
using WebApiShop.MiddleWare;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserServices, UserServices>();  
builder.Services.AddScoped<IPasswordServices, PasswordServices>();
builder.Services.AddScoped<IProductsServices, ProductsServices>();
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<ICategoriesServices, CategoriesServices>();
builder.Services.AddScoped<ICategoriesRepository, CategoriesRepository>();
builder.Services.AddScoped<IOrdersRepository, OrdersRepository> ();
builder.Services.AddScoped<IOrdersServices, OrdersServices>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<OrderStatusUpdaterService>();

builder.Host.UseNLog();
builder.Services.AddDbContext<ApiShopContext>(option=>option.UseSqlServer ("Data Source=Noa;Initial Catalog=ApiShop;Integrated Security=True;Trust Server Certificate=True"));
// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });


builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularOrigins",
    policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                      return false;

                  return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
              })
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);

// ����� ������ ������
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true, // ���� ���� �� ������
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

app.UseCors("AllowAngularOrigins");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
    });
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseErrorHandling();

app.UseRating();

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
