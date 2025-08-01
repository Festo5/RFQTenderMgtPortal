using Microsoft.Extensions.Configuration;
using PortalAuthMgmt;
using RFQVendorManage;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecruitmentPortal.Services;

public class BusinessCentralAuthService
{
    private readonly PortalAuthManagement_PortClient _authClient;
    private readonly RFQVendorPortalAPI_PortClient _rfqClient;

    public BusinessCentralAuthService(IConfiguration config)
    {
        // Verify configuration values
        var authEndpoint = config["BusinessCentral:SOAP:AuthEndpoint"] ??
            throw new ArgumentNullException("BusinessCentral:SOAP:AuthEndpoint is missing in configuration");
        var rfqEndpoint = config["BusinessCentral:SOAP:RFQEndpoint"] ??
            throw new ArgumentNullException("BusinessCentral:SOAP:RFQEndpoint is missing in configuration");
        var username = config["BusinessCentral:SOAP:Username"] ??
            throw new ArgumentNullException("BusinessCentral:SOAP:Username is missing in configuration");
        var password = config["BusinessCentral:SOAP:Password"] ??
            throw new ArgumentNullException("BusinessCentral:SOAP:Password is missing in configuration");

        // Initialize auth client with Basic authentication
        var authBinding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly)
        {
            Security = {
                Transport = {
                    ClientCredentialType = HttpClientCredentialType.Basic,
                    ProxyCredentialType = HttpProxyCredentialType.None
                }
            },
            MaxReceivedMessageSize = 65536000,
            AllowCookies = true
        };

        _authClient = new PortalAuthManagement_PortClient(authBinding, new EndpointAddress(authEndpoint));
        _authClient.ClientCredentials.UserName.UserName = username;
        _authClient.ClientCredentials.UserName.Password = password;

        // Initialize RFQ client with Basic authentication
        var rfqBinding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly)
        {
            Security = {
                Transport = {
                    ClientCredentialType = HttpClientCredentialType.Basic,
                    ProxyCredentialType = HttpProxyCredentialType.None
                }
            },
            MaxReceivedMessageSize = 65536000,
            AllowCookies = true
        };

        _rfqClient = new RFQVendorPortalAPI_PortClient(rfqBinding, new EndpointAddress(rfqEndpoint));
        _rfqClient.ClientCredentials.UserName.UserName = username;
        _rfqClient.ClientCredentials.UserName.Password = password;
    }

    public async Task<bool> CreateUserAsync(string email, string plainPassword, string fullName)
    {
        try
        {
            string passwordHash = ComputeHash(plainPassword);
            var body = new CreateUserBody(email, passwordHash, fullName);
            var request = new CreateUser(body);
            var response = await _authClient.CreateUserAsync(request);
            return response?.Body?.return_value ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateUserAsync Error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ValidateUserAsync(string email, string plainPassword)
    {
        try
        {
            string passwordHash = ComputeHash(plainPassword);
            var body = new ValidateUserBody(email, passwordHash);
            var request = new ValidateUser(body);
            var response = await _authClient.ValidateUserAsync(request);
            return response?.Body?.return_value ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ValidateUserAsync Error: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetVendorNoByEmailAsync(string email)
    {
        try
        {
            var requestBody = new GetVendorNoByEmailBody(email);
            var request = new GetVendorNoByEmail(requestBody);
            var response = await _rfqClient.GetVendorNoByEmailAsync(request);

            if (string.IsNullOrWhiteSpace(response?.Body?.return_value))
            {
                throw new Exception("No vendor assigned to this user");
            }

            return response.Body.return_value;
        }
        catch (FaultException ex)
        {
            Console.WriteLine($"SOAP Fault in GetVendorNoByEmailAsync: {ex.Message}");
            throw new Exception("Failed to get vendor number from Business Central", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetVendorNoByEmailAsync error: {ex.Message}");
            throw;
        }
    }

    public async Task<int> GetCandidateIdAsync(string email)
    {
        try
        {
            // TODO: Implement actual Business Central API call to get candidate ID
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCandidateIdAsync error: {ex.Message}");
            return 0;
        }
    }

    private string ComputeHash(string input)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }

    public async Task<bool> UpdateUserProfileAsync(string email, string companyName, string phoneNumber, byte[] profilePicture)
    {
        try
        {
            string pictureBase64 = profilePicture != null ? Convert.ToBase64String(profilePicture) : string.Empty;
            var body = new UpdateProfileBody(email, companyName, phoneNumber, pictureBase64);
            var request = new UpdateProfile(body);
            var response = await _authClient.UpdateProfileAsync(request);
            return response?.Body?.return_value ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateUserProfileAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<UserProfile> GetUserProfileAsync(string email)
    {
        try
        {
            string companyName = string.Empty;
            string phoneNumber = string.Empty;
            string profilePictureBase64 = string.Empty;

            var body = new GetProfileBody(email, companyName, phoneNumber, profilePictureBase64);
            var request = new GetProfile(body);
            var response = await _authClient.GetProfileAsync(request);

            return new UserProfile
            {
                CompanyName = response?.Body?.companyName,
                PhoneNumber = response?.Body?.phoneNumber,
                ProfilePicture = !string.IsNullOrEmpty(response?.Body?.profilePictureBase64)
                    ? Convert.FromBase64String(response.Body.profilePictureBase64)
                    : null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetUserProfileAsync error: {ex.Message}");
            return null;
        }
    }

}

public class UserProfile
{
    public string CompanyName { get; set; }
    public string PhoneNumber { get; set; }
    public byte[] ProfilePicture { get; set; }
}
