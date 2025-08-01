using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using RecruitmentPortal.Models.ViewModels;
using RecruitmentPortal.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RecruitmentPortal.Controllers
{
    [Authorize]
    public class JobController : Controller
    {
        private readonly BusinessCentralJobService _jobService;
        private readonly IWebHostEnvironment _environment;

        public JobController(BusinessCentralJobService jobService, IWebHostEnvironment environment)
        {
            _jobService = jobService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var activeJobs = await _jobService.GetActiveJobsAsync();
            var model = new JobListingViewModel
            {
                ActiveJobs = activeJobs
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Apply(int jobId)
        {
            var candidateIdClaim = User.FindFirst("CandidateId");
            if (candidateIdClaim == null || !int.TryParse(candidateIdClaim.Value, out int candidateId) || candidateId <= 0)
            {
                TempData["ErrorMessage"] = "Please complete your profile before applying for jobs.";
                return RedirectToAction("Login", "Account");
            }

            var jobDetails = await _jobService.GetJobDetailsAsync(jobId);
            if (jobDetails == null)
            {
                return NotFound();
            }

            var model = new JobApplicationModel
            {
                JobId = jobDetails.Id,
                CandidateId = candidateId,
                JobTitle = jobDetails.Title
            };

            ViewBag.JobTitle = jobDetails.Title;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(JobApplicationModel model)
        {
            if (!ModelState.IsValid)
            {
                var jobDetails = await _jobService.GetJobDetailsAsync(model.JobId);
                ViewBag.JobTitle = jobDetails?.Title;
                return View(model);
            }

            try
            {
                string resumePath = await SaveResumeFile(model.ResumeFile);
                var success = await _jobService.ApplyForJobAsync(
                    model.JobId,
                    model.CandidateId,
                    model.CoverLetter,
                    resumePath);

                if (!success)
                {
                    ModelState.AddModelError("", "Failed to submit application. Please try again.");
                    ViewBag.JobTitle = (await _jobService.GetJobDetailsAsync(model.JobId))?.Title;
                    return View(model);
                }

                return RedirectToAction("ApplicationSuccess");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error processing your application: {ex.Message}");
                ViewBag.JobTitle = (await _jobService.GetJobDetailsAsync(model.JobId))?.Title;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Apply(JobListingViewModel model)
        {
            if (model.SelectedJobId <= 0)
            {
                ModelState.AddModelError("", "Please select a job to apply for");
                model.ActiveJobs = await _jobService.GetActiveJobsAsync();
                return View("Index", model);
            }

            return RedirectToAction("Apply", new { jobId = model.SelectedJobId });
        }

        [HttpGet]
        public IActionResult ApplicationSuccess()
        {
            return View();
        }

        private async Task<string> SaveResumeFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded");

            var safeFileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                throw new ArgumentException("Invalid file name");

            if (safeFileName.Contains("..") || safeFileName.Contains("/") || safeFileName.Contains("\\"))
                throw new ArgumentException("Invalid file name");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "resumes");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/resumes/{uniqueFileName}";
        }
    }
}