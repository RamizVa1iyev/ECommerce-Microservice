using CatalogService.Api.Extensions;
using CatalogService.Api.Extensions.Registration;
using CatalogService.Api.Infrastructure;
using CatalogService.Api.Infrastructure.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Add folder for pictures
builder.Services.Configure<CatalogSettings>(builder.Configuration.GetSection("CatalogSettings"));

builder.Services.ConfigureConsul(builder.Configuration);

builder.Services.ConfigureDbContext(builder.Configuration);

//Add migration
builder.Services.MigrateDbContext<CatalogContext>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.StartAsync().Wait();

app.RegisterWithConsul(app.Services.GetService<IHostApplicationLifetime>());

app.WaitForShutdown();
//app.Run();
