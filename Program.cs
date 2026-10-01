using System.Reflection;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<News, NewsDto>();
});
IMapper mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Blog}/{action=Index}/{id?}");

app.Run();

public class News
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class NewsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public static class NewsMappingExtensions
{
    public static NewsDto ToDto(this News news)
    {
        return new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content
        };
    }
}

public static class ReflectionMapper
{
    public static TTarget Map<TSource, TTarget>(TSource source) where TTarget : new()
    {
        var target = new TTarget();
        var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var targetProperties = typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var sourceProp in sourceProperties)
        {
            var targetProp = targetProperties.FirstOrDefault(p => p.Name == sourceProp.Name && p.PropertyType == sourceProp.PropertyType);
            if (targetProp != null && targetProp.CanWrite)
            {
                var value = sourceProp.GetValue(source);
                targetProp.SetValue(target, value);
            }
        }

        return target;
    }
}

public class BlogController : Controller
{
    private readonly IMapper _autoMapper;

    private static readonly List<News> NewsList = new()
    {
        new News { Id = 1, Title = "Перша новина блогу", Content = "Ласкаво просимо до нашого оновленого блогу!", Date = DateTime.Now.AddDays(-2) },
        new News { Id = 2, Title = "Оновлення ASP.NET Core", Content = "Дізнайтеся про нові можливості роботи з Cookie та Session.", Date = DateTime.Now.AddDays(-1) },
        new News { Id = 3, Title = "Поради щодо оформлення", Content = "Ви можете змінити тему оформлення у налаштуваннях.", Date = DateTime.Now }
    };

    public BlogController(IMapper autoMapper)
    {
        _autoMapper = autoMapper;
    }

    [HttpGet]
    public IActionResult Index()
    {
        string theme = Request.Cookies["UserTheme"] ?? "light";

        var dtoAutoMapper = _autoMapper.Map<List<NewsDto>>(NewsList);
        var dtoExtension = NewsList.Select(n => n.ToDto()).ToList();
        var dtoReflection = NewsList.Select(n => ReflectionMapper.Map<News, NewsDto>(n)).ToList();

        var newsCards = new StringBuilder();
        foreach (var news in dtoAutoMapper)
        {
            newsCards.Append($@"
                <div class='card mb-3 shadow-sm {(theme == "dark" ? "bg-secondary text-white" : "")}'>
                    <div class='card-body'>
                        <h5 class='card-title'>{news.Title}</h5>
                        <p class='card-text'>{news.Content}</p>
                    </div>
                </div>");
        }

        var html = $@"
            <!DOCTYPE html>
            <html lang='uk'>
            <head>
                <meta charset='utf-8'>
                <title>Особистий блог</title>
                <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
            </head>
            <body class='{(theme == "dark" ? "bg-dark text-white" : "bg-light text-dark")} py-4'>
                <div class='container'>
                    <div class='d-flex justify-content-between align-items-center mb-4'>
                        <h1>Особистий блог</h1>
                        <a href='/Blog/Settings' class='btn {(theme == "dark" ? "btn-outline-light" : "btn-outline-dark")}'>Налаштування читання</a>
                    </div>
                    
                    <div class='mb-4'>
                        <h3>Новини блогу:</h3>
                        {newsCards}
                    </div>

                    <div class='card p-3 my-4 {(theme == "dark" ? "bg-secondary text-white" : "bg-white text-dark")}'>
                        <h6>Демонстрація мапінгу (Доп. завдання 2):</h6>
                        <small>
                            AutoMapper (кількість): {dtoAutoMapper.Count} | 
                            Extension Method (кількість): {dtoExtension.Count} | 
                            Reflection (кількість): {dtoReflection.Count}
                        </small>
                    </div>
                </div>
            </body>
            </html>";

        return Content(html, "text/html", Encoding.UTF8);
    }

    [HttpGet]
    public IActionResult Settings()
    {
        string currentTheme = Request.Cookies["UserTheme"] ?? "light";

        var html = $@"
            <!DOCTYPE html>
            <html lang='uk'>
            <head>
                <meta charset='utf-8'>
                <title>Налаштування читання</title>
                <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
            </head>
            <body class='{(currentTheme == "dark" ? "bg-dark text-white" : "bg-light text-dark")} py-5'>
                <div class='container' style='max-width: 500px;'>
                    <div class='card p-4 shadow {(currentTheme == "dark" ? "bg-secondary text-white" : "")}'>
                        <h2 class='mb-4 text-center'>Налаштування читання</h2>
                        <form action='/Blog/SaveSettings' method='post'>
                            <div class='mb-3'>
                                <label class='form-label'>Оберіть тему оформлення:</label>
                                <select name='theme' class='form-select'>
                                    <option value='light' {(currentTheme == "light" ? "selected" : "")}>Світла тема</option>
                                    <option value='dark' {(currentTheme == "dark" ? "selected" : "")}>Темна тема</option>
                                </select>
                            </div>
                            <button type='submit' class='btn btn-primary w-100 mb-2'>Зберегти налаштування</button>
                            <a href='/Blog/Index' class='btn btn-link w-100 text-center {(currentTheme == "dark" ? "text-white" : "")}'>Назад до блогу</a>
                        </form>
                    </div>
                </div>
            </body>
            </html>";

        return Content(html, "text/html", Encoding.UTF8);
    }

    [HttpPost]
    public IActionResult SaveSettings(string theme)
    {
        CookieOptions option = new CookieOptions
        {
            Expires = DateTime.Now.AddDays(30),
            IsEssential = true
        };

        Response.Cookies.Append("UserTheme", theme, option);

        return RedirectToAction("Index");
    }
}