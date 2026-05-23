using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var connectionString = "Host=ep-restless-butterfly-ald0v8wi-pooler.c-3.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_1v5incwuqtbo;SslMode=Require;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    
    if (!db.Topics.Any())
    {
        db.Topics.AddRange(
            new Topic { Name = "Техподдержка" },
            new Topic { Name = "Продажи" },
            new Topic { Name = "Другое" },
            new Topic { Name = "Еще один пункт" }
        );
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.MapGet("/api/topics", async (AppDbContext db) =>
{
    return await db.Topics.ToListAsync();
});

app.MapPost("/api/feedback", async (FeedbackDto dto, AppDbContext db) =>
{
    if (!dto.Email.Contains("@")) return Results.BadRequest("Invalid Email");

    var contact = await db.Contacts
        .FirstOrDefaultAsync(c => c.Email == dto.Email && c.Phone == dto.Phone);
    
    if (contact == null)
    {
        contact = new Contact
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
    }

    var message = new Message
    {
        ContactId = contact.Id,
        TopicId = dto.TopicId,
        Text = dto.MessageText
    };
    db.Messages.Add(message);
    await db.SaveChangesAsync();

    var savedMessage = await db.Messages
        .Include(m => m.Contact)
        .Include(m => m.Topic)
        .FirstOrDefaultAsync(m => m.Id == message.Id);

    return Results.Ok(new
    {
        Name = savedMessage.Contact.Name,
        Email = savedMessage.Contact.Email,
        Phone = savedMessage.Contact.Phone,
        Topic = savedMessage.Topic.Name,
        Message = savedMessage.Text
    });
});

app.Run();

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Topic> Topics { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Message> Messages { get; set; }
}

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

public class Message
{
    public int Id { get; set; }
    public string Text { get; set; }
    
    public int TopicId { get; set; }
    public Topic Topic { get; set; }
    
    public int ContactId { get; set; }
    public Contact Contact { get; set; }
}

public class FeedbackDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int TopicId { get; set; }
    public string MessageText { get; set; }
}