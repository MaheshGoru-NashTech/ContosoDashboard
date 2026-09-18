using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var isSqliteConnection = !string.IsNullOrWhiteSpace(defaultConnection) &&
    (defaultConnection.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
     defaultConnection.Contains("Filename=", StringComparison.OrdinalIgnoreCase)) &&
    !defaultConnection.Contains("Server=", StringComparison.OrdinalIgnoreCase);

if (isSqliteConnection)
{
    var sqliteDbPath = Path.Combine(builder.Environment.ContentRootPath, "ContosoDashboard.db");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={sqliteDbPath}"));
}
else if (!string.IsNullOrWhiteSpace(defaultConnection))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(defaultConnection));
}
else
{
    var sqliteDbPath = Path.Combine(builder.Environment.ContentRootPath, "ContosoDashboard.db");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite($"Data Source={sqliteDbPath}"));
}

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
var uploadRoot = app.Configuration["FileStorage:UploadRoot"] ?? Path.Combine("AppData", "uploads");
Directory.CreateDirectory(Path.IsPathRooted(uploadRoot) ? uploadRoot : Path.Combine(app.Environment.ContentRootPath, uploadRoot));

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated(); // For development - use migrations in production
        EnsureDocumentTables(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

static void EnsureDocumentTables(ApplicationDbContext context)
{
    if (!context.Database.IsSqlite())
    {
        return;
    }

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS Documents (
            DocumentId INTEGER NOT NULL CONSTRAINT PK_Documents PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            Description TEXT NULL,
            Category TEXT NOT NULL,
            Tags TEXT NULL,
            FileName TEXT NOT NULL,
            StoredFileName TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            FileType TEXT NOT NULL,
            FileSizeBytes INTEGER NOT NULL,
            UploadedByUserId INTEGER NOT NULL,
            ProjectId INTEGER NULL,
            UploadedAtUtc TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL,
            IsDeleted INTEGER NOT NULL,
            CONSTRAINT FK_Documents_Users_UploadedByUserId FOREIGN KEY (UploadedByUserId) REFERENCES Users (UserId) ON DELETE RESTRICT,
            CONSTRAINT FK_Documents_Projects_ProjectId FOREIGN KEY (ProjectId) REFERENCES Projects (ProjectId) ON DELETE SET NULL
        );
        CREATE TABLE IF NOT EXISTS DocumentShares (
            DocumentShareId INTEGER NOT NULL CONSTRAINT PK_DocumentShares PRIMARY KEY AUTOINCREMENT,
            DocumentId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            SharedByUserId INTEGER NOT NULL,
            SharedAtUtc TEXT NOT NULL,
            NotificationSent INTEGER NOT NULL,
            CONSTRAINT FK_DocumentShares_Documents_DocumentId FOREIGN KEY (DocumentId) REFERENCES Documents (DocumentId) ON DELETE CASCADE,
            CONSTRAINT FK_DocumentShares_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (UserId) ON DELETE CASCADE,
            CONSTRAINT FK_DocumentShares_Users_SharedByUserId FOREIGN KEY (SharedByUserId) REFERENCES Users (UserId) ON DELETE RESTRICT
        );
        CREATE TABLE IF NOT EXISTS DocumentActivityLogs (
            ActivityId INTEGER NOT NULL CONSTRAINT PK_DocumentActivityLogs PRIMARY KEY AUTOINCREMENT,
            DocumentId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            ActivityType TEXT NOT NULL,
            ActivityAtUtc TEXT NOT NULL,
            Details TEXT NULL,
            CONSTRAINT FK_DocumentActivityLogs_Documents_DocumentId FOREIGN KEY (DocumentId) REFERENCES Documents (DocumentId) ON DELETE CASCADE,
            CONSTRAINT FK_DocumentActivityLogs_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (UserId) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS IX_Documents_UploadedByUserId ON Documents (UploadedByUserId);
        CREATE INDEX IF NOT EXISTS IX_Documents_ProjectId ON Documents (ProjectId);
        CREATE INDEX IF NOT EXISTS IX_Documents_Category ON Documents (Category);
        CREATE INDEX IF NOT EXISTS IX_DocumentShares_DocumentId ON DocumentShares (DocumentId);
        CREATE INDEX IF NOT EXISTS IX_DocumentShares_UserId ON DocumentShares (UserId);
        """);

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE Documents ADD COLUMN Tags TEXT NULL;");
    }
    catch (Exception ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
    {
        // Existing databases already contain the column.
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/documents/download/{documentId:int}", async (int documentId, HttpContext httpContext, IDocumentService documentService) =>
{
    var claim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (claim == null || !int.TryParse(claim.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    try
    {
        var document = await documentService.GetDocumentByIdAsync(documentId, userId);
        if (document == null)
        {
            return Results.NotFound();
        }

        var stream = await documentService.DownloadDocumentAsync(documentId, userId);
        return Results.File(stream, document.FileType, document.FileName);
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
