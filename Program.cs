using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StudentApi.Data;
using System.Text;
//Creates the app builder
var builder = WebApplication.CreateBuilder(args);

// -------------------- CORS --------------------
//Allows Angular (localhost:4200) to call this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        //policy.WithOrigins(http://localhost:4200)
         policy.WithOrigins("https://famous-syrniki-ec8fd9.netlify.app/")

              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// -------------------- CONTROLLERS & SWAGGER --------------------
//check all folder where controller class is inheriting or like that it catches that
builder.Services.AddControllers(); //find and register all controllers
builder.Services.AddEndpointsApiExplorer(); //expose meta data routes,parameters,return types]
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    //show authorize button in swagger UI
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
}); //generate API doc with auth button

// -------------------- DATABASE --------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// -------------------- JWT AUTH --------------------
var key = Encoding.UTF8.GetBytes("THIS_IS_MY_SUPER_SECRET_KEY_123456");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// -------------------- MIDDLEWARE --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");

// 🔴 ORDER MATTERS
app.UseAuthentication();   // FIRST
app.UseAuthorization();    // SECOND

app.MapControllers();
app.Run();
