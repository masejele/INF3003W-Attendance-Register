using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UCTAttendanceRegister.Models;
using UCTAttendanceRegister.Services;

namespace UCTAttendanceRegister.Pages.Lecturer.AttendanceImport;

[Authorize(Roles = "Lecturer")]
public class IndexModel : PageModel
{
    private readonly AttendanceCsvImportService _importService;

    public IndexModel(
        AttendanceCsvImportService importService)
    {
        _importService = importService;
    }

    [BindProperty]
    [Required]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [BindProperty]
    [Required]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [BindProperty]
    [Display(Name = "CSV File")]
    public IFormFile? CsvFile { get; set; }

    public AttendanceCsvImportResult? ImportResult { get; private set; }

    public async Task OnGetAsync()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (EndTime <= StartTime)
        {
            ModelState.AddModelError(nameof(EndTime), "End time must be after the start time.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        ImportResult = await _importService.ImportAsync(
            CsvFile,
            StartTime,
            EndTime,
            cancellationToken);

        if (!ImportResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, ImportResult.ErrorMessage ?? "The import could not be completed.");
        }

        return Page();
    }
}