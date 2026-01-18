using Microsoft.EntityFrameworkCore;
using Payments.Infrastructure;
using Payments.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext with in-memory database for basic template
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlite("Data Source=memory:payments.db")); // Use SQLite for simplicity

// Register services
// builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
// builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
