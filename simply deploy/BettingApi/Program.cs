using Microsoft.EntityFrameworkCore;
using BettingApi.Data;
using BettingApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using BettingApi.Services;
using BettingApi.Repositories;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {//ændrer cors kun til githubpages for admin og user
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();


builder.Services.AddDbContext<BetAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDepositRepository, DepositRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICoinFlipRandommizer, CoinFlipRandommizer>();
builder.Services.AddScoped<ICoinFlipService, CoinFlipService>();
builder.Services.AddScoped<IDepositService, DepositService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ICrashRng, CrashRng>();
builder.Services.AddScoped<ISlotMachineService, SlotMachineService>();
builder.Services.AddScoped<ICrashGameService, CrashGameService>();
builder.Services.AddScoped<IGenerateGrid, GenerateGrid>();
builder.Services.AddScoped<ICalculatePayout, CalculatePayout>();


builder.Services.AddIdentity<ApiUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.AllowedForNewUsers = false;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;

}).AddEntityFrameworkStores<BetAppDbContext>();



builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
    options.DefaultChallengeScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme =
    JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
    System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])),
    ClockSkew = TimeSpan.Zero //burde fjerne default 5 min default tillægs tid som er til få små delays osv
    };
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<BetAppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApiUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await TestBetDbSeeder.Seed(context, userManager, roleManager);
}



// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
