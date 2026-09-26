using AspnetAspect.DependencyInjection;
using AspectSample.Aspects;
using AspectSample.Logic;
using AspectSample.Repositories;
using AspectSample.Services;
using AspnetAspect.Aspects;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<MethodLoggingAspect>();
builder.Services.AddScoped<TransactionAspect>();
builder.Services.AddScoped<IAspect>(sp => sp.GetRequiredService<MethodLoggingAspect>());
builder.Services.AddScoped<IAspect>(sp => sp.GetRequiredService<TransactionAspect>());

builder.Services.AddScopedAspectProxy<IAccountService, AccountService>();
builder.Services.AddScopedAspectProxy<IAccountLogic, AccountLogic>();
builder.Services.AddScopedAspectProxy<IAuditLogic, AuditLogic>();
builder.Services.AddScopedAspectProxy<IAccountRepository, AccountRepository>();
builder.Services.AddScopedAspectProxy<IAuditRepository, AuditRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()) {
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
  name: "default",
  pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

app.Run();
