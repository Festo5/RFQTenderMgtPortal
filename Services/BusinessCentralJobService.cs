using ApplicationManage;
using Microsoft.Extensions.Configuration;
using RecruitmentPortal.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Threading.Tasks;

namespace RecruitmentPortal.Services
{
    public class BusinessCentralJobService
    {
        private readonly ApplicationManagement_PortClient _soapClient;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public BusinessCentralJobService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Initialize SOAP Client (unchanged from your working version)
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly)
            {
                Security = { Transport = { ClientCredentialType = HttpClientCredentialType.Windows } },
                MaxReceivedMessageSize = 65536000
            };
            var endpoint = new EndpointAddress(_config["BusinessCentral:SOAP:JobEndpoint"]);
            _soapClient = new ApplicationManagement_PortClient(binding, endpoint);
            _soapClient.ClientCredentials.Windows.ClientCredential = new NetworkCredential(
                _config["BusinessCentral:SOAP:Username"],
                _config["BusinessCentral:SOAP:Password"]
            );

            // Initialize HTTP Client with Windows Auth
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(
                    _config["BusinessCentral:OData:Username"],
                    _config["BusinessCentral:OData:Password"]
                ),
                UseDefaultCredentials = false
            };

            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(_config["BusinessCentral:OData:BaseUrl"]);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<bool> ApplyForJobAsync(int jobId, int candidateId, string coverLetter, string resumePath)
        {
            try
            {
                var request = new ApplyForJob(
                    new ApplyForJobBody(jobId, candidateId, coverLetter, resumePath)
                );
                var response = await _soapClient.ApplyForJobAsync(request);
                return response?.Body?.return_value ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApplyForJobAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<JobDetailsDto> GetJobDetailsAsync(int jobId)
        {
            try
            {
                var companyName = _config["BusinessCentral:OData:Company"];
                var endpoint = $"{_config["BusinessCentral:OData:BaseUrl"]}/Company('{companyName}')/JobPostingAPI({jobId})";

                var response = await _httpClient.GetFromJsonAsync<JobDetailsDto>(endpoint);
                return response ?? throw new ApplicationException($"Job {jobId} not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetJobDetailsAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<JobDetailsDto>> GetActiveJobsAsync()
        {
            try
            {
                var companyName = _config["BusinessCentral:OData:Company"];
                var endpoint = $"{_config["BusinessCentral:OData:BaseUrl"]}/Company('{companyName}')/JobPostingAPI";

                var response = await _httpClient.GetFromJsonAsync<ODataResponse<JobDetailsDto>>(endpoint);
                return response?.Value ?? new List<JobDetailsDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetActiveJobsAsync: {ex.Message}");
                return new List<JobDetailsDto>();
            }
        }
    }
}