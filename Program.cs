using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Extensions;
using APICatalogo.Filters;
using APICatalogo.Repositories;
using APICatalogo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Tratando a referencia ciclica
builder.Services.AddControllers().AddJsonOptions(options =>
options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles)
// Adicionando o pacote Json Patch
.AddNewtonsoftJson();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Incluindo o serviço de autenticação e autorização
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization();

string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
                                options.UseMySql(mySqlConnection, 
                                ServerVersion.AutoDetect(mySqlConnection)));

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
    app.ConfigureExceptionHandler();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
