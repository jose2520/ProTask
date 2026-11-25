using Microsoft.AspNetCore.Mvc;
using ProTask.Application.DTOs.Tasks;
using ProTask.Application.Interfaces;
using ProTask.Domain.Enums;

namespace ProTask.API.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskItemDto>>> GetAll(
        [FromQuery] TaskItemStatus? status,
        [FromQuery] TaskPriority? priority,
        CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetAllAsync(status, priority, cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskItemDto>>> GetOverdue(CancellationToken cancellationToken)
    {
        var overdueTasks = await _taskService.GetOverdueAsync(cancellationToken);
        return Ok(overdueTasks);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        return Ok(task);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> Create(
        [FromBody] CreateTaskItemDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> Update(
        int id,
        [FromBody] UpdateTaskItemDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _taskService.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> UpdateStatus(
        int id,
        [FromBody] UpdateTaskStatusDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await _taskService.UpdateStatusAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
