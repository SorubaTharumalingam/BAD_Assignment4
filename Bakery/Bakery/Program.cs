
using Bakery;
using Bakery.Context;
using Bakery.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// add logger
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
    config.WriteTo.Console();
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MyDbContext>(options => 
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();

// adding mongoDB service for endpoint searching in the logs
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<IMongoClient, MongoClient>(sp =>
{
   var settings = sp.GetRequiredService<IConfiguration>().GetSection("MongoDB").Get<MongoSettings>();
   return new MongoClient(settings.ConnectionString);
});

// Adding Core Identity service to service container
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // minimum requirements for user passwords
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
}).AddEntityFrameworkStores<MyDbContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        options.DefaultChallengeScheme =
            options.DefaultForbidScheme =
                options.DefaultScheme =
                    options.DefaultSignInScheme =
                        options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
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
            System.Text.Encoding.UTF8.GetBytes(
                builder.Configuration["JWT:SigningKey"]))
    };
});



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    // Login is the only endpoint accessible for anonymous users.
    options =>
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


// creating roles:
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireClaim("IsAdmin","true"));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireClaim("IsManager","true"));
    options.AddPolicy("RequireBakerRole", policy => policy.RequireClaim("IsBaker","true"));
    options.AddPolicy("RequireDriverRole", policy => policy.RequireClaim("IsDriver","true"));
    options.AddPolicy("RequireAdminManagerOrBakerRole", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c => 
            (c.Type == "IsAdmin" && c.Value == "true") || 
            (c.Type == "IsManager" && c.Value == "true") || 
            (c.Type == "IsBaker" && c.Value == "true")
        )));
    options.AddPolicy("RequireAdminManagerOrDriverRole", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c => 
            (c.Type == "IsAdmin" && c.Value == "true") || 
            (c.Type == "IsManager" && c.Value == "true") || 
            (c.Type == "IsDriver" && c.Value == "true")
        )));
    options.AddPolicy("RequireAdminOrManager", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c => 
            (c.Type == "IsAdmin" && c.Value == "true") || 
            (c.Type == "IsManager" && c.Value == "true")
        )));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
