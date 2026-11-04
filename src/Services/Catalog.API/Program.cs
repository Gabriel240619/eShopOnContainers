using Catalog.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Add service for managing a sqlserver database that will be managed using ApplicationDBContext
// the connection to the database was defined in appsettings
builder.Services.AddDbContext<CatalogContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
     options => {
         options.SwaggerDoc("v1",
            new OpenApiInfo
            {  // Uso el array de par�metros por nombre.
                Title = "API Rest del Ejemplo eShopOnContainers",
                Contact = new OpenApiContact
                {
                    Email = "mariaelena.@uclm.com", //cambiar estos datos por los nuestros
                    Name = "Mar�a Elena Pretel Fern�ndez", //cambiar estos datos por los nuestros
                    Url = new Uri("https://www.esiiab.uclm.es")
                },
                License = new OpenApiLicense
                {
                    Name = "Apache License, 2.0",
                    Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
                },
                // Cuidado que Version es obligatorio.
                Version = "v1"
            });

         // Para a�adir la informaci�n de documentaci�n del XML generado a Swagger.
         var xmlfile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
         var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlfile);

     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    try
    {

        var db = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        //it creates the DB in case it does not exist
        //this is used only while developing the system

        db.Database.Migrate();

    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.MapControllers();

app.Run();