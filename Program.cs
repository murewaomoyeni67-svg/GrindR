using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var websitePath = Path.Combine(builder.Environment.ContentRootPath, "website");
builder.Services.AddSingleton(new ExerciseCatalog(1008));
builder.Services.AddSingleton(new AccountStore());
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = new PhysicalFileProvider(websitePath) });
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(websitePath) });
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "grindr-api" }));
app.MapPost("/api/ai", async (AiRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Message)) return Results.BadRequest(new { error = "Message is required." });
    using var client = new HttpClient();
    var system = "You are a concise, encouraging fitness assistant. Give safe general fitness guidance and recommend a qualified professional for medical concerns.";
    var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (!string.IsNullOrWhiteSpace(openAiKey))
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", openAiKey);
        var body = JsonSerializer.Serialize(new { model = "gpt-4o-mini", messages = new[] { new { role = "system", content = system }, new { role = "user", content = request.Message } }, max_tokens = 500 });
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(body, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) { using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return Results.Ok(new { reply = json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString(), provider = "ChatGPT" }); }
    }
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))) return Results.Ok(new { reply = "Claude is configured, but its provider request adapter is next to be enabled.", provider = "Claude" });
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY"))) return Results.Ok(new { reply = "Gemini is configured, but its provider request adapter is next to be enabled.", provider = "Gemini" });
    return Results.Json(new { error = "No AI provider key is configured in Railway." }, statusCode: 503);
});
app.MapGet("/api/integrations", () => Results.Ok(new
{
    google = HasSetting("GOOGLE_CLIENT_ID") && HasSetting("GOOGLE_CLIENT_SECRET"),
    chatgpt = HasSetting("OPENAI_API_KEY"),
    claude = HasSetting("ANTHROPIC_API_KEY"),
    gemini = HasSetting("GEMINI_API_KEY"),
    storage = HasSetting("SUPABASE_URL") && HasSetting("SUPABASE_SERVICE_ROLE_KEY"),
    stripe = HasSetting("STRIPE_SECRET_KEY") && HasSetting("STRIPE_PRICE_ID")
}));
app.MapGet("/api/exercises", (ExerciseCatalog catalog, string? search, string? muscle, int page = 1, int pageSize = 24) => { page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100); var all = catalog.Search(search, muscle); return Results.Ok(new { total = all.Count, page, pageSize, results = all.Skip((page - 1) * pageSize).Take(pageSize) }); });
app.MapGet("/api/exercises/{id:int}", (ExerciseCatalog catalog, int id) => catalog.TryGet(id, out var exercise) ? Results.Ok(exercise) : Results.NotFound());
app.MapGet("/api/exercises/{id:int}/demo.svg", (ExerciseCatalog catalog, int id) => { if (!catalog.TryGet(id, out var exercise)) return Results.NotFound(); var label = System.Security.SecurityElement.Escape(exercise.Name); var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 640 360'><rect width='640' height='360' fill='#101010'/><text x='32' y='42' fill='#ef4444' font-family='sans-serif' font-weight='700' font-size='25'>{label}</text><text x='32' y='70' fill='#999' font-family='sans-serif' font-size='14'>{exercise.Muscle} · {exercise.Equipment}</text><g fill='#d0a08e' stroke='#292020' stroke-width='3'><ellipse cx='500' cy='155' rx='28' ry='34'/><path d='M475 178 C420 160 370 175 315 205 C260 230 190 210 130 190 C90 178 55 200 25 230 L38 248 C90 220 145 252 210 270 C290 292 350 245 410 215 C450 195 480 210 495 190Z'/><path d='M130 190 C80 160 42 155 10 175 L5 195 C48 203 82 225 120 246 L150 220Z'/><path d='M155 235 C115 265 100 292 98 320 L120 320 C140 290 165 275 190 258Z'/><path d='M225 255 C195 285 190 310 200 335 L222 332 C230 302 250 280 275 260Z'/></g><path d='M270 210 C310 180 350 180 390 195' fill='none' stroke='#ef4444' stroke-width='9' stroke-linecap='round'><animate attributeName='d' values='M270 210 C310 180 350 180 390 195;M270 190 C310 160 350 160 390 175;M270 210 C310 180 350 180 390 195' dur='1.4s' repeatCount='indefinite'/></path></svg>"; return Results.Text(svg, "image/svg+xml", Encoding.UTF8); });
app.MapGet("/api/members", (AccountStore accounts) => Results.Ok(accounts.GetMembers()));
app.MapPost("/api/members/join", (JoinRequest request, AccountStore accounts) => Results.Ok(accounts.Join(request.Email, request.DisplayName)));
app.MapFallback(() => Results.Redirect("/index.html"));
app.Run();

static bool HasSetting(string name) => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name));

public sealed record JoinRequest(string Email, string DisplayName);
public sealed record AiRequest(string Message);
public sealed record Exercise(int Id, string Name, string Muscle, string Equipment, string Level, string DemoUrl);
public sealed class ExerciseCatalog
{
    private readonly List<Exercise> exercises;
    private static readonly string[] Muscles = ["Chest", "Back", "Legs", "Shoulders", "Arms", "Core", "Glutes", "Calves"];
    private static readonly string[] Equipment = ["Barbell", "Dumbbell", "Cable", "Machine", "Bodyweight", "Kettlebell"];
    private static readonly string[] Movements = ["Bench Press", "Row", "Curl", "Raise", "Squat", "Lunge", "Fly", "Extension", "Pull", "Crunch", "Carry", "Deadlift", "Shoulder Press", "Lat Pulldown"];
    public ExerciseCatalog(int count) => exercises = Enumerable.Range(1, count).Select(id => { var muscle = Muscles[(id - 1) % Muscles.Length]; var movement = Movements[(id * 3) % Movements.Length]; var equipment = Equipment[(id * 5) % Equipment.Length]; return new Exercise(id, $"{equipment} {muscle} {movement} {id}", muscle, equipment, id % 3 == 0 ? "Advanced" : id % 2 == 0 ? "Intermediate" : "Beginner", $"/api/exercises/{id}/demo.svg"); }).ToList();
    public List<Exercise> Search(string? search, string? muscle) => exercises.Where(e => (string.IsNullOrWhiteSpace(search) || Normalize(e.Name).Contains(Normalize(search))) && (string.IsNullOrWhiteSpace(muscle) || e.Muscle.Equals(muscle, StringComparison.OrdinalIgnoreCase))).ToList();
    public bool TryGet(int id, out Exercise exercise) { exercise = exercises.FirstOrDefault(e => e.Id == id)!; return exercise is not null; }
    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
public sealed class AccountStore
{
    private readonly ConcurrentDictionary<string, string> members = new(StringComparer.OrdinalIgnoreCase);
    public object[] GetMembers() => members.Select(m => new { email = m.Key, displayName = m.Value }).ToArray();
    public object Join(string email, string displayName) { members[email.Trim()] = displayName.Trim(); return new { joined = true, email = email.Trim().ToLowerInvariant(), displayName = displayName.Trim() }; }
}
