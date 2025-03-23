using System.Text.Json;
using HaxorByteClub.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen();
builder.Services.AddRazorPages();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Enable CORS middleware
app.UseCors();

app.MapRazorPages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// GuestBook endpoints
var guestBookFilePath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "guestbook.json");

// Ensure the JSON file exists
if (!File.Exists(guestBookFilePath))
{
    Directory.CreateDirectory(Path.GetDirectoryName(guestBookFilePath)!);
    File.WriteAllText(guestBookFilePath, "[]");
}

app.MapGet("/api/v0/guestbook", () =>
    {
        var json = File.ReadAllText(guestBookFilePath);
        var entries = JsonSerializer.Deserialize<List<Message>>(json) ?? new List<Message>();
        return Results.Ok(entries);
    })
    .WithName("GetGuestBookEntries");

app.MapPost("/api/v0/guestbook", async (Message newMessage) =>
{
    var json = await File.ReadAllTextAsync(guestBookFilePath);
    var entries = JsonSerializer.Deserialize<List<Message>>(json) ?? new List<Message>();

    entries.Add(newMessage);

    json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(guestBookFilePath, json);

    return Results.Created($"/api/v0/guestbook/{entries.Count - 1}", newMessage);
})
.WithName("AddGuestBookEntry");

app.MapGet("/api/v0/hackernews", async () =>
{
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync("https://news.ycombinator.com/rss");
    return Results.Content(response, "application/rss+xml");
});


app.MapGet("/api/v0/music", async () =>
{
    //get data from /data/music.json and return string
    var musicFilePath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "data", "music.json");
    if (!File.Exists(musicFilePath))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(musicFilePath)!);
        File.WriteAllText(musicFilePath, "[]");
    }
    var json = await File.ReadAllTextAsync(musicFilePath);
    return Results.Content(json, "application/json");
});

app.Run();

