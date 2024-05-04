using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITaskService>(new inMemoryTaskService());
var app = builder.Build();
// built in middleware
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
// custom middleware
app.Use(async (context, next) =>{
    Console.WriteLine($"[{context.Request.Method}{context.Request.Path}{DateTime.UtcNow} Started.]");
    await next(context);
    Console.WriteLine($"[{context.Request.Method}{context.Request.Path}{DateTime.UtcNow} Finished.]");
});
var todos = new List<Todo>();

app.MapGet("/todos", ()=>todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) => {
    var targetTodo = todos.SingleOrDefault(t => id == t.Id);
    return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task) => {
    todos.Add(task);
    return TypedResults.Created("/todos/{id}", task);
})
.AddEndpointFilter(async (context, next) => {
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();
    if(taskArgument.DueDate<DateTime.UtcNow)
    {
        errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past"]);
    }
    if(taskArgument.isCompleted)
    {
        errors.Add(nameof(Todo.isCompleted), ["Cannot add completed todo."]);
    }
    if(errors.Count>0)
    {
        return Results.ValidationProblem(errors);
    }
    return await next(context);
});

app.MapDelete("/todos/{id}", (int id) => {
    todos.RemoveAll(t=> id == t.Id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool isCompleted);

interface ITaskService
{
    Todo? GetTodoById(int id);
    List<Todo> GetTodos();
    void DeleteTodoById(int id);
    Todo AddTodo(Todo task);
}
class inMemoryTaskService : ITaskService
{
    private readonly List<Todo> _todos= [];
    public Todo AddTodo(Todo task)
    {
        _todos.Add(task);
        return task;
    }
    public void DeleteTodoById(int id)
    {
        _todos.RemoveAll(t=> id == t.Id);
    }
    public Todo? GetTodoById(int id)
    {
        return _todos.SingleOrDefault(t=> t.Id == id);
    }
    public List<Todo> GetTodos()
    {
        return _todos;
    }
}