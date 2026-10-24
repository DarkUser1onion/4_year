using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

public class CreateTaskRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

public class TasksController : Controller
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    public IActionResult Index(string? status)
    {
        var snapshot = _taskService.GetAll();

        var mode = status is "done" or "active" ? status : "all";

        var visible = mode switch
        {
            "done" => snapshot.Where(t => t.IsDone).ToList(),
            "active" => snapshot.Where(t => !t.IsDone).ToList(),
            _ => snapshot
        };

        ViewBag.TotalTasks = snapshot.Count;
        ViewBag.CompletedTasks = snapshot.Count(t => t.IsDone);
        ViewBag.CurrentStatus = mode;

        return View(visible);
    }

    public IActionResult Details(int id)
    {
        var found = _taskService.GetById(id);
        if (found is null) return NotFound();
        return View(found);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("title", "Заголовок обязателен");
            return View();
        }

        _taskService.Add(title, description);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Done(int id, string? status)
    {
        _taskService.MarkDone(id);
        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpPost]
    public IActionResult Delete(int id, string? status)
    {
        _taskService.Delete(id);
        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpGet("tasks/api/list")]
    public IActionResult ApiList()
    {
        var items = _taskService.GetAll();
        return Json(items);
    }

    [HttpGet("tasks/api/{id:int}")]
    public IActionResult ApiGet(int id)
    {
        var item = _taskService.GetById(id);
        if (item is null) return NotFound();
        return Json(item);
    }

    [HttpPost("tasks/api/create")]
    public IActionResult ApiCreate([FromBody] CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Заголовок обязателен" });

        var created = _taskService.Add(request.Title, request.Description);
        return CreatedAtAction(nameof(ApiGet), new { id = created.Id }, created);
    }
}