using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

string userDb = "Data/users.json";
string vaultDb = "Data/vault.json";
if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");

// --- API KATMANI ---

app.MapPost("/api/auth/register", async (User newUser) => {
    var users = File.Exists(userDb) ? JsonSerializer.Deserialize<List<User>>(await File.ReadAllTextAsync(userDb)) : new List<User>();
    // Null uyarısını (CS8604) önlemek için ?? kullanıyoruz
    var userList = users ?? new List<User>();
    
    if (userList.Any(u => u.Username.ToLower() == newUser.Username.ToLower())) return Results.BadRequest("ID Kullanımda.");
    
    userList.Add(newUser);
    await File.WriteAllTextAsync(userDb, JsonSerializer.Serialize(userList));
    return Results.Ok();
});

app.MapPost("/api/auth/login", async (User loginData) => {
    if (!File.Exists(userDb)) return Results.Unauthorized();
    var users = JsonSerializer.Deserialize<List<User>>(await File.ReadAllTextAsync(userDb)) ?? new List<User>();
    var user = users.FirstOrDefault(u => u.Username.ToLower() == loginData.Username.ToLower() && u.Password == loginData.Password);
    return user is not null ? Results.Ok(user) : Results.Unauthorized();
});

app.MapPost("/api/vault/sync", async (VaultRequest req) => {
    var users = JsonSerializer.Deserialize<List<User>>(await File.ReadAllTextAsync(userDb)) ?? new List<User>();
    var user = users.FirstOrDefault(u => u.Username == req.Owner && u.Password == req.VaultKey);
    if (user == null) return Results.Problem("Sisteme Giriş Engellendi: Hatalı Kasa Anahtarı.");

    if (!File.Exists(vaultDb)) return Results.Ok(new List<VaultAsset>());
    var all = JsonSerializer.Deserialize<List<VaultAsset>>(await File.ReadAllTextAsync(vaultDb)) ?? new List<VaultAsset>();
    var userAssets = all.Where(a => a.Owner == req.Owner).ToList();
    
    userAssets.ForEach(a => a.Value = SecurityHelper.Decrypt(a.Value, req.VaultKey));
    return Results.Ok(userAssets);
});

app.MapPost("/api/vault/add", async (VaultAsset asset) => {
    var vault = File.Exists(vaultDb) ? JsonSerializer.Deserialize<List<VaultAsset>>(await File.ReadAllTextAsync(vaultDb)) : new List<VaultAsset>();
    var vaultList = vault ?? new List<VaultAsset>();
    
    asset.Value = SecurityHelper.Encrypt(asset.Value, asset.PassKey); 
    vaultList.Add(asset);
    await File.WriteAllTextAsync(vaultDb, JsonSerializer.Serialize(vaultList));
    return Results.Ok();
});

app.MapDelete("/api/vault/delete/{id}", async (Guid id) => {
    if (!File.Exists(vaultDb)) return Results.NotFound();
    var vault = JsonSerializer.Deserialize<List<VaultAsset>>(await File.ReadAllTextAsync(vaultDb)) ?? new List<VaultAsset>();
    vault.RemoveAll(a => a.Id == id);
    await File.WriteAllTextAsync(vaultDb, JsonSerializer.Serialize(vault));
    return Results.Ok();
});

app.Run("http://localhost:5050");

// --- MODELLER (EN ALTA ALINDI: CS0246 ÇÖZÜMÜ) ---
public record User(string Username, string Password, string Rank);
public record VaultRequest(string Owner, string VaultKey);
public class VaultAsset {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Value { get; set; } = "";
    public string Owner { get; set; } = "";
    public string PassKey { get; set; } = ""; 
}