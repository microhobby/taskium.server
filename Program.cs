using Microsoft.EntityFrameworkCore;
using taskium.server;
using microhobby.Utils;

// create the .taskium folder
ConstructEnvironment.Constructium();

// follow with the minimal api
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<TaskDb>(
    opt => opt.UseSqlite("Data Source=./taskium.db;"), 
    ServiceLifetime.Transient
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
// app.UseAuthorization();
// app.MapControllers();
app.UseCors(builder => 
    builder.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

// ------------------------------------------------------------------------ API

app.MapGet("/", () => {
    var version = app.Configuration.GetSection("Version").Get<String>();
    return $"Taskium server v{version}";
});

app.MapPost("/task", async (taskium.server.Task newTask, TaskDb db) => {
    // add it
    db.Tasks.Add(newTask);
    await db.SaveChangesAsync();

    return Results.Created($"/task/{newTask.Id}", newTask);
});

app.MapGet("/task/{id}", async (int id, TaskDb db) => {
    return await db.Tasks.FindAsync(id)
        is taskium.server.Task task
            ? Results.Ok(task)
            : Results.NotFound();
})
.Produces<taskium.server.Task>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/tasks/running", (TaskDb db) => {
    var tasks = db.Tasks
                    .Where(t => t.IsStarted == true && t.IsComplete == false);

    return Results.Ok(tasks);
})
.Produces<taskium.server.Task[]>(StatusCodes.Status200OK);

app.MapGet("/tasks", (TaskDb db) => {
    var tasks = db.Tasks.OrderByDescending(x => x.Id);
    return Results.Ok(tasks);
})
.Produces<taskium.server.Task[]>(StatusCodes.Status200OK);

// ------------------------------------------------------------------------ API

var _finished = false;

// check if we have tasks to run until finished
TaskDb.SERVICE = app.Services;

new Thread(() => {
    var pollingCheckInterval = app.Configuration
        .GetSection("TasksPollingCheckInterval").Get<int>();

    while (!_finished) {
        app.Logger.LogInformation("Checking for tasks ...");
        var db = TaskDb.GetDBFromNewScope();

        if (db != null) {
            // find the tasks that are not started yet
            var tasks = db.Tasks
                .Where(t => t.IsStarted == false);

            // start it
            foreach(var task in tasks) {
                // we need to create a new scope for each call
                new TaskExecuter(task);

                task.IsStarted = true;

                // yeah new one to update the start of the task
                db.Update(task);
                db.SaveChanges();

                Thread.Sleep(pollingCheckInterval);
            }
        }

        Thread.Sleep(pollingCheckInterval);
    }
}).Start();

// run the server until finished
app.Run();
_finished = true;
