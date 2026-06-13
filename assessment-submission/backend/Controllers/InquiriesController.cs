using CourseInquiryApi.Models;
using CourseInquiryApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseInquiryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InquiriesController : ControllerBase
{
    private readonly IInquiryService _service;

    public InquiriesController(IInquiryService service)
    {
        _service = service;
    }

    //Create a new inquiry (public — prospective students submit without a key)
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateInquiryRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    //List inquiries can by status
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] InquiryStatus? status, CancellationToken ct)
    {
        var items = await _service.GetAllAsync(status, ct);
        return Ok(items);
    }

    //Get a single inquiry by id
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var inquiry = await _service.GetByIdAsync(id, ct);
        return inquiry is null ? NotFound() : Ok(inquiry);
    }

    //Update an inquiry's status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateStatusAsync(id, request.Status!.Value, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    //Soft-delete an inquiry
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _service.ArchiveAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
