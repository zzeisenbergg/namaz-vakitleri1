using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using MyNamespace.Extensions; // Namespace'inizi buraya yazınız

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// CORS yapılandırması
builder.Services.AddCors(options => 
    options.AddDefaultPolicy(policy => 
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Awqat Salah Service Settings
var awqatSalahSettingsSection = builder.Configuration.GetSection(nameof(AwqatSalahSettings));
builder.Services.Configure<AwqatSalahSettings>(awqatSalahSettingsSection);
builder.Services.AddSingleton<IAwqatSalahSettings>(sp => sp.GetRequiredService<IOptions<AwqatSalahSettings>>().Value);

builder.Services.AddHttpClient("AwqatSalahApi", client => 
{
    var awqatSalahSettings = awqatSalahSettingsSection.Get<AwqatSalahSettings>();
    if (awqatSalahSettings != null)
    {
        client.BaseAddress = new Uri(awqatSalahSettings.ApiUri);
    }
});

// Kullanıcı doğrulaması için ClientActionFilter'i etkinleştirin
builder.Services
    //.AddControllers() // Bu satırı kapatıyoruz.
    .AddControllers(opt => opt.Filters.Add<ClientActionFilter>()); // Kullanıcı doğrulaması filtre eklenerek aktif edilir.

// CacheSettings
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(nameof(CacheSettings)));
builder.Services.AddSingleton<ICacheSettings>(sp => sp.GetRequiredService<IOptions<CacheSettings>>().Value);
builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// API Bağlantı ve Servis Bağımlılıkları
builder.Services.AddScoped<IAwqatSalahConnectService, AwqatSalahApiService>();
builder.Services.AddTransient<IPlaceService, PlaceService>();
builder.Services.AddTransient<IDailyContentService, DailyContentService>();
builder.Services.AddTransient<IAwqatSalahService, AwqatSalahService>();

// API Versiyonlama
builder.Services.AddAndConfigureApiVersioning();

// Swagger ve OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureSwagger();

var app = builder.Build();

// Geliştirme ve Üretim Ortamı Middleware'leri
if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwagger(apiVersionDescriptionProvider);
}
else
{
    app.UseMiddleware<ExceptionMiddleware>();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
