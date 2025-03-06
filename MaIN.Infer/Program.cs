using MaIN.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register your NuGet package services
builder.Services.AddMaIN(builder.Configuration);

var app = builder.Build();

// Initialize MaIN-related services
app.Services.UseMaIN();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host"); // Even if you don't have a _Host.cshtml, 
// this fallback is typical in .NET 8 templates.

app.Run();