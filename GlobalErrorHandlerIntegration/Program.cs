using GlobalErrorHandlerIntegration.Helpers;
using GlobalErrorHandlerIntegration.IServices;
using GlobalErrorHandlerIntegration.Middlewares;
using GlobalErrorHandlerIntegration.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the Error Logger service for dependency injection
builder.Services.AddHttpClient<ITelexErrorLogger, TelexErrorLogger>();
builder.Services.AddSingleton<ITelexErrorLogger, TelexErrorLogger>();

builder.Services.Configure<TelexSettings>(builder.Configuration.GetSection("TelexSettings"));

// Allow Cors from Telex
builder.Services.AddCors(options =>
{
    options.AddPolicy("TelexPolicy", policy =>
    {
        policy.WithOrigins(
            "https://telex.im", 
            "http://staging.telextest.im",
            "http://telextest.im",
            "https://staging.telex.im") 
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("TelexPolicy");

// Insert Global Exception Middleware into the pipeline to catch errors early
app.UseMiddleware<TelexGlobalExceptionMiddleware>();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
