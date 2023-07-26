using TrialUsersMediator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrialUsersDbContext>();
builder.WebHost.ConfigureTrialUserMediator();
builder.Services.AddScoped<Authentication>();

builder.Services.AddControllers().AddNewtonsoftJson(_ => { _.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; });
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseSwagger().UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync_();


static class TrialUsersMediatorExtensions
{
    internal static async Task RunAsync_(this WebApplication app)
    { await app.Services.GetRequiredService<TrialUser.Identity>().ObtainAsync(); app.Run(); }
}
