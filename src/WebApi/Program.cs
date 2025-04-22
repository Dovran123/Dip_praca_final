using Application;
using Infrastructure;
using WebApi.Infrastructure;
using Infrastructure.Persistence;
using SoapCore;
using Application.Common.Interfaces.Repositories;
using static SoapCore.DocumentationWriter.SoapDefinition;

var builder = WebApplication.CreateBuilder(args);

// Pridanie služieb
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSoapCore();

// Konfigurácia globálneho handlera chýb, atï.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

// Nastavenie SOAP endpointu
app.UseRouting();
app.UseSoapEndpoint<IPreskripciaService>("/PreskripciaService.asmx", new SoapEncoderOptions());

// Ostatné middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseExceptionHandler();
app.MapControllers();

app.Run();
