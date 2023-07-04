using TrialUsersMediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrialUsersDbContext>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseSwagger().UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run("https://localhost:7000");
