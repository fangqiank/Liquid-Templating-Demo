using Fluid;
using Marten;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddRazorPages();

builder.Services.AddMarten(con =>
{
    con.Connection("host=192.168.5.1;port=5432;database=email;user id=postgres;password=g73gle73;");
});

builder.Services.AddSingleton<FluidParser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//app.UseAuthorization();

//app.MapRazorPages();

app.MapGet("/", () => "Hello World");

app.MapPost("/create-template", async (EmailTemplate email, IDocumentSession session ) =>
{
    session.Store(email);
    await session.SaveChangesAsync();
    return "ok";
});

app.MapPost("/create-generic", async (Generic gen, IDocumentSession session) =>
{
    session.Store(gen);
    await session.SaveChangesAsync();
    return "ok";
});

app.MapPost("/create-config", async (EmailConfig config, IDocumentSession session) =>
{
    session.Store(config);
    await session.SaveChangesAsync();
    return "ok";
});

app.MapGet("/test/{genId:guid}", async (Guid genId, FluidParser parser, IQuerySession session) =>
{
    var gen = await session.LoadAsync<Generic>(genId);

    EmailTemplate emailTemplate = null;

    var _ = await session.Query<EmailConfig>()
        .Include<EmailTemplate>(x => x.TemplateId, t => emailTemplate = t )
        .FirstOrDefaultAsync(x => x.Type == gen.Bag["type"]);
    
    //var model = new { Firstname = "Bill", Lastname = "Gates" };
    //var emailTemplate = await session.LoadAsync<EmailTemplate>(id);

    if (parser.TryParse(emailTemplate?.Template, out var template, out var error))
    {
        var context = new TemplateContext(gen?.Bag);

        return template.Render(context);
    }
    
    return "Out of order";

});

app.Run();



public record Generic(Guid Id, Dictionary<string, string> Bag);
public record EmailTemplate(Guid Id, string Template);
public record EmailConfig(Guid Id, string Type, Guid TemplateId);