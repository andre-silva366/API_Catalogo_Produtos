//using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Extensions;
using APICatalogo.Filters;
using APICatalogo.Repositories;
using APICatalogo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using APICatalogo.Models;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using APICatalogo.RateLimitOptions;
using Asp.Versioning;
using APICatalogo.Context;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Tratando a referencia ciclica -------------------------------------------------------- REFERENCIA CICLICA -----------------------
builder.Services.AddControllers().AddJsonOptions(options =>
options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles)
// Adicionando o pacote Json Patch
.AddNewtonsoftJson();

// Incluindo os perfis do Identity ------------------------------------------------------ PERFIS DO IDENTITY -----------------------
//builder.Services.AddIdentity<ApplicationUser, IdentityRole>().
//    AddEntityFrameworkStores<AppDbContext>()
//    .AddDefaultTokenProviders();



// habilita e configura a autenticação jwt bearer na aplicação --------------------------- JWT BEARER -------------------------------
var secretKey = builder.Configuration["JWT:SecretKey"]
    ?? throw new ArgumentException("invalid secret key!!");
// --------------------------------------------------------------------------------------- ADICIONA AUTENTICAÇÃO -----------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddScoped<ITokenService, TokenService>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    //c.SwaggerDoc("v1", new OpenApiInfo { Title = "apicatalogo", Version = "v1" });
    //c.SwaggerDoc("v2", new OpenApiInfo { Title = "apicatalogo", Version = "v2" });
    // ------------------------------------------------------------------------------------------- INCLUINDO DOCUMENTAÇÃO ---------------------------------------------------
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "APICatalogo",
        Description = "Catálogo de Produtos e Categorias",
        TermsOfService = new Uri("https://macoratti.net/terms"),
        Contact = new OpenApiContact
        {
            Name = "macoratti",
            Email = "macoratti@yahoo.com",
            Url = new Uri("https://macoratti.net"),
        },
        License = new OpenApiLicense
        {
            Name = "Usar sobre LICX",
            Url = new Uri("https://macoratti.net/license"),
        }
    });

    // Incluindo comentários XML --------------------------------------------------------------------------- XML ------------------

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer JWT",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme, Id = "Bearer"
                }

            },
            new string[]{}

        }
    });
});



// Habilitando o CORS - Cross Origin Resource Sharing ---------------------------------------------- CORS POLITICA NOMEADA ---------
// Politica nomeada

builder.Services.AddCors(options =>
{
    options.AddPolicy("OrigensComAcessoPermitido",
        policy =>
        {
            policy.WithOrigins("https://localhost:7092/index.html")
            .WithMethods("GET", "POST")
                .AllowAnyHeader();
                
        });
});

// Habilitando o CORS - Cross Origin Resource Sharing ---------------------------------------------- CORS POLITICA PADRÃO -----------
// Politica padrão

//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(
//        policy =>
//        {
//            policy.WithOrigins("https://apirequest.io")
//                .WithMethods("GET", "POST")
//                .AllowAnyHeader()
//                .AllowCredentials();
//        });
//});

// Incluindo o serviço de autenticação e autorização
//builder.Services.AddAuthentication("Bearer").AddJwtBearer();
//  --------------------------------------------------------------------------------------- ADICIONA AUTORIZAÇÃO -----------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("Admin").RequireClaim("id", "Andre"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("ExclusiveOnly", policy => policy.RequireAssertion(context => 
        context.User.HasClaim(claim => claim.Type == "id" && claim.Value == "Andre") || context.User.IsInRole("SuperAdmin")));

});

// Ratelimiting usando o json ----------------
var myOptions = new MyRateLimitOptions();
builder.Configuration.GetSection(MyRateLimitOptions.MyRateLimit).Bind(myOptions);

//incluindo Rate Limiting ------------------------------------------------------------------ RATE LIMITING ---------------------
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter(policyName: "fixedwindow", options =>
    {
        options.PermitLimit = myOptions.PermitLimit;
        options.Window = TimeSpan.FromSeconds(myOptions.Window);
        options.QueueLimit = myOptions.QueueLimit;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// incluindo Rate Limiting ------------------------------------------------------------------ RATE LIMITING ---------------------
//builder.Services.AddRateLimiter(rateLimiterOptions =>
//{
//    rateLimiterOptions.AddFixedWindowLimiter(policyName: "fixedwindow", options =>
//    {
//        options.PermitLimit = 1;
//        options.Window = TimeSpan.FromSeconds(5);
//        options.QueueLimit = 2;
//        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
//    });
//    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//});



//builder.Services.AddRateLimiter(options =>
//{
//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpcontext =>
//                            RateLimitPartition.GetFixedWindowLimiter(
//                                partitionKey: httpcontext.User.Identity?.Name ??
//                                              httpcontext.Request.Headers.Host.ToString(),
//                                factory: partition => new FixedWindowRateLimiterOptions
//                                {
//                                    AutoReplenishment = true,
//                                    PermitLimit = 2,
//                                    QueueLimit = 0,
//                                    Window = TimeSpan.FromSeconds(10)
//                                }));
//});

string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
                                options.UseMySql(mySqlConnection,
                                ServerVersion.AutoDetect(mySqlConnection)));

//// Versionamento da Api --------------------------------------------------------------------- VERSIONAMENTO PADRÃO QUERY STRING-----------
//builder.Services.AddApiVersioning(o =>
//{
//    o.DefaultApiVersion = new ApiVersion(1, 0);
//    o.AssumeDefaultVersionWhenUnspecified = true;
//    o.ReportApiVersions = true;

//}).AddApiExplorer(options =>
//{
//    options.GroupNameFormat = "'v'VVV";
//    options.SubstituteApiVersionInUrl = true;
//});

// Versionamento da Api --------------------------------------------------------------------- VERSIONAMENTO URI -----------
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(
                        new QueryStringApiVersionReader(),
                        new UrlSegmentApiVersionReader());

}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Registrando o serviço de log
builder.Services.AddScoped<ApiLoggingFilter>();

// Registrando o repository
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();

// Registrando o serviço do repositório genérico
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Registrando o serviço do UnitOfWork
builder.Services.AddScoped<IUnityOfWork,UnitOfWork>();

// Registrando o serviço do Auto mapper
builder.Services.AddAutoMapper(typeof(ProdutoDTOMappingProfile));

// Também daria certo para o automapper -----------------------------------------------------------------------------
//builder.Services.AddAutoMapper(typeof(Program));

// Teste
var valor1 = builder.Configuration["chave1"];
var valor2 = builder.Configuration["secao1:chave2"];

// Transiente cria uma nova instância sempre que for chamada
builder.Services.AddTransient<IMeuServico,MeuServico>();

// Desabilitando o mecanismo de inferencia para a injeção de dependencia nos controladores
builder.Services.Configure <ApiBehaviorOptions>(options =>
{
    options.DisableImplicitFromServicesParameters = true;
});

// Configurando o provedor de log customizado
//builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
//{
//    LogLevel = LogLevel.Information
//}));

// Adicionar o filtro criado como um filtro global
builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ApiExceptionFilter));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    //O código abaixo é desnecessário, basta o de cima 'app.UseSwaggerUI', somente para mostrar
    //app.UseSwaggerUI(c =>
    //{
    //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "APICatalogo");
    //});

    app.ConfigureExceptionHandler();
}

app.UseHttpsRedirection();

//// Habilitando o CORS - Cross Origin Resource Sharing com politica nomeada
//app.UseCors(OrigensComAcessoPermitido);

app.UseRateLimiter();


app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
