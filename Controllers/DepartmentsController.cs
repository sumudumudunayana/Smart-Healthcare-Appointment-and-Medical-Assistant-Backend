using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHealthcare.API.DTOs.Departments;
using SmartHealthcare.API.Services;

namespace SmartHealthcare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _departmentService;

    public DepartmentsController(
        DepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    // ============================================================
    // GET ALL DEPARTMENTS
    // GET: /api/Departments
    // ============================================================

    [HttpGet]
    public async Task<ActionResult<List<DepartmentResponse>>> GetAll()
    {
        List<DepartmentResponse> departments =
            await _departmentService.GetAllAsync();

        return Ok(departments);
    }

    // ============================================================
    // CREATE DEPARTMENT
    // POST: /api/Departments
    // ============================================================

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<DepartmentResponse>> Create(
        [FromBody] CreateDepartmentRequest request)
    {
        try
        {
            DepartmentResponse department =
                await _departmentService.CreateAsync(request);

            return Ok(department);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // UPDATE DEPARTMENT
    // PUT: /api/Departments/{departmentId}
    // ============================================================

    [HttpPut("{departmentId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<DepartmentResponse>> Update(
        Guid departmentId,
        [FromBody] UpdateDepartmentRequest request)
    {
        try
        {
            DepartmentResponse department =
                await _departmentService.UpdateAsync(
                    departmentId,
                    request
                );

            return Ok(department);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // DELETE DEPARTMENT
    // DELETE: /api/Departments/{departmentId}
    // ============================================================

    [HttpDelete("{departmentId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid departmentId)
    {
        try
        {
            await _departmentService.DeleteAsync(departmentId);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }
}