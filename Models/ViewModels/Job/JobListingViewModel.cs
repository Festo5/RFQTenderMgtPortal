// JobListingViewModel.cs
using RecruitmentPortal.Models.DTOs;

public class JobListingViewModel
{
    public List<JobDetailsDto> ActiveJobs { get; set; }
    public int SelectedJobId { get; set; } // For form submission
}