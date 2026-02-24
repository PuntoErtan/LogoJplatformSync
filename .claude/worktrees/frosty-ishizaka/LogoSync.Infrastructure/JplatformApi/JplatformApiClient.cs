using LogoSync.Core.DTOs;
using LogoSync.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogoSync.Infrastructure.JplatformApi
{
    public class JplatformApiClient : IJplatformApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JplatformSettings _settings;
        private readonly ILogger<JplatformApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private string _authToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        public JplatformApiClient(JplatformSettings settings, ILogger<JplatformApiClient> logger)
        {
            _settings = settings;
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/')),
                Timeout = TimeSpan.FromSeconds(60)
            };

            // ✅ KRITIK: UnsafeRelaxedJsonEscaping kullanarak + karakterinin 
            // \u002B olarak encode edilmesini önlüyoruz
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // ← BU SATIR ÖNEMLİ!
            };
        }

        /// <summary>
        /// Logo J-Platform Login - Token al
        /// </summary>
        private async Task<bool> LoginAsync()
        {
            const string endpoint = "/logo/restservices/rest/login";

            try
            {
                // Önce logout yap (eski session temizliği)
                await LogoutAsync();

                // Logo Basic Auth formatı: username:password:periodNo:firmNo:lang
                // Baştaki sıfırları kaldır: "005" → "5", "01" → "1"
                var periodNo = int.Parse(_settings.PeriodNo).ToString();
                var firmNo = int.Parse(_settings.FirmNo).ToString();
                var authString = $"{_settings.Username}:{_settings.Password}:{periodNo}:{firmNo}:TRTR";
                var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
                request.Content = new StringContent("", Encoding.UTF8, "application/json");

                _logger.LogDebug("Login attempt to {Endpoint}", endpoint);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Login response ({StatusCode}): {Content}", response.StatusCode, content);

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        if (root.TryGetProperty("authToken", out var tokenProp))
                        {
                            _authToken = tokenProp.GetString();
                            _tokenExpiry = DateTime.Now.AddMinutes(25);
                            _logger.LogInformation("Login successful. Token acquired.");
                            return true;
                        }
                    }

                    var errorMsg = root.TryGetProperty("errorMessage", out var errProp)
                        ? errProp.GetString()
                        : "Unknown login error";
                    _logger.LogError("Login failed: {Error}", errorMsg);
                }
                else
                {
                    _logger.LogError("Login HTTP error: {StatusCode} - {Content}", response.StatusCode, content);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login exception");
                return false;
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                await _httpClient.PostAsync("/logo/restservices/rest/logout", null);
                _logger.LogDebug("Logout completed");
            }
            catch
            {
                // Logout hatası kritik değil
            }
        }

        private async Task<string> GetAuthTokenAsync()
        {
            await _tokenLock.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(_authToken) || DateTime.Now >= _tokenExpiry)
                {
                    _logger.LogDebug("Token expired or missing, refreshing...");
                    var success = await LoginAsync();
                    if (!success)
                    {
                        throw new InvalidOperationException("Failed to acquire auth token");
                    }
                }

                // Final token formatı: base64(1:authToken:username)
                var finalTokenString = $"1:{_authToken}:{_settings.Username}";
                var finalToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(finalTokenString));

                return finalToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        public async Task<JplatformApiResponse> CreateCashReceiptAsync(JplatformSafeDepositSlip slip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/safedepositslips";

            try
            {
                var authToken = await GetAuthTokenAsync();

                var json = JsonSerializer.Serialize(slip, _jsonOptions);
                _logger.LogDebug("POST {Endpoint} - Request: {Json}", endpoint, json);

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("auth-token", authToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Response ({StatusCode}): {Content}", response.StatusCode, responseContent);

                // 401 ise token'ı sıfırla ve tekrar dene
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized, clearing token and retrying...");
                    _authToken = null;
                    _tokenExpiry = DateTime.MinValue;
                    return await CreateCashReceiptRetryAsync(slip);
                }

                return ParseApiResponse(response, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Error");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Connection error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = "Request timeout"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<JplatformApiResponse> CreateCashReceiptRetryAsync(JplatformSafeDepositSlip slip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/safedepositslips";

            var authToken = await GetAuthTokenAsync();
            var json = JsonSerializer.Serialize(slip, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("auth-token", authToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            return ParseApiResponse(response, responseContent);
        }

        /// <summary>
        /// Çek Giriş Bordrosu oluştur
        /// POST /chequepnoteslips?slipType=1
        /// </summary>
        public async Task<JplatformApiResponse> CreateChequeReceiptAsync(JplatformChequePNoteSlip slip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/chequepnoteslips?slipType=1";

            try
            {
                var authToken = await GetAuthTokenAsync();

                var json = JsonSerializer.Serialize(slip, _jsonOptions);
                _logger.LogDebug("POST {Endpoint} - Request: {Json}", endpoint, json);

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("auth-token", authToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Response ({StatusCode}): {Content}", response.StatusCode, responseContent);

                // 401 ise token'ı sıfırla ve tekrar dene
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized, clearing token and retrying...");
                    _authToken = null;
                    _tokenExpiry = DateTime.MinValue;
                    return await CreateChequeReceiptRetryAsync(slip);
                }

                return ParseApiResponse(response, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Error (Cheque)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Connection error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = "Request timeout"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error (Cheque)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<JplatformApiResponse> CreateChequeReceiptRetryAsync(JplatformChequePNoteSlip slip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/chequepnoteslips?slipType=1";

            var authToken = await GetAuthTokenAsync();
            var json = JsonSerializer.Serialize(slip, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("auth-token", authToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            return ParseApiResponse(response, responseContent);
        }

        /// <summary>
        /// Sipariş oluştur
        /// POST /salesOrder
        /// </summary>
        public async Task<JplatformApiResponse> SendOrderAsync(JplatformOrderSlip orderSlip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/salesOrder";

            try
            {
                var authToken = await GetAuthTokenAsync();

                var json = JsonSerializer.Serialize(orderSlip, _jsonOptions);
                _logger.LogDebug("POST {Endpoint} - Request: {Json}", endpoint, json);

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("auth-token", authToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Response ({StatusCode}): {Content}", response.StatusCode, responseContent);

                // 401 ise token'ı sıfırla ve tekrar dene
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized (Order), clearing token and retrying...");
                    _authToken = null;
                    _tokenExpiry = DateTime.MinValue;
                    return await SendOrderRetryAsync(orderSlip);
                }

                return ParseApiResponse(response, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Error (Order)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Connection error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = "Request timeout"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error (Order)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<JplatformApiResponse> SendOrderRetryAsync(JplatformOrderSlip orderSlip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/salesOrder";

            var authToken = await GetAuthTokenAsync();
            var json = JsonSerializer.Serialize(orderSlip, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("auth-token", authToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            return ParseApiResponse(response, responseContent);
        }

        /// <summary>
        /// Cari Hesap Fişi (Sanal POS) oluştur
        /// POST /arpslips?slipType=08
        /// </summary>
        public async Task<JplatformApiResponse> CreateArpSlipAsync(JplatformArpSlip slip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/arpslips?slipType=08";

            try
            {
                var authToken = await GetAuthTokenAsync();

                var json = JsonSerializer.Serialize(slip, _jsonOptions);
                _logger.LogDebug("POST {Endpoint} - Request: {Json}", endpoint, json);

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("auth-token", authToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Response ({StatusCode}): {Content}", response.StatusCode, responseContent);

                // 401 ise token'ı sıfırla ve tekrar dene
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized (ArpSlip), clearing token and retrying...");
                    _authToken = null;
                    _tokenExpiry = DateTime.MinValue;
                    return await CreateArpSlipRetryAsync(slip);
                }

                return ParseApiResponse(response, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Error (ArpSlip)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Connection error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = "Request timeout"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error (ArpSlip)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<JplatformApiResponse> CreateArpSlipRetryAsync(JplatformArpSlip slip)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/arpslips?slipType=08";

            var authToken = await GetAuthTokenAsync();
            var json = JsonSerializer.Serialize(slip, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("auth-token", authToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            return ParseApiResponse(response, responseContent);
        }

        private JplatformApiResponse ParseApiResponse(HttpResponseMessage response, string content)
        {
            if (response.IsSuccessStatusCode)
            {
                var transactionNo = ParseTransactionNo(content);
                var code = ParseCode(content);
                var logicalRef = ParseLogicalRef(content);
                return new JplatformApiResponse
                {
                    Success = true,
                    TransactionNo = transactionNo,
                    Code = code,
                    LogicalRef = logicalRef,
                    Data = content
                };
            }
            else
            {
                var errorMessage = ParseErrorMessage(content);
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorCode = (int)response.StatusCode,
                    ErrorMessage = errorMessage ?? $"HTTP {response.StatusCode}: {content}"
                };
            }
        }

        private string ParseTransactionNo(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("transactionNo", out var transNoElement))
                    return transNoElement.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string ParseCode(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var codeElement))
                    return codeElement.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        private int? ParseLogicalRef(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("logicalRef", out var refElement))
                    return refElement.GetInt32();

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string ParseErrorMessage(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // Logo hata formatları
                if (root.TryGetProperty("message", out var msgArray) && msgArray.ValueKind == JsonValueKind.Array)
                {
                    var messages = new List<string>();
                    foreach (var msg in msgArray.EnumerateArray())
                    {
                        messages.Add(msg.GetString());
                    }
                    return string.Join(" | ", messages);
                }

                if (root.TryGetProperty("error", out var errorElement))
                {
                    if (errorElement.TryGetProperty("message", out var msgElement))
                        return msgElement.GetString();
                }

                if (root.TryGetProperty("Message", out var messageElement))
                    return messageElement.GetString();

                if (root.TryGetProperty("ERRORMESSAGE", out var errMsgElement))
                    return errMsgElement.GetString();

                if (root.TryGetProperty("errorMessage", out var errorMsgElement))
                    return errorMsgElement.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _tokenLock?.Dispose();
            _httpClient?.Dispose();
        }

        // =====================================================================
        // JplatformApiClient.cs içine eklenecek region
        // POST /invoices/sales?invoiceType=8
        // =====================================================================

        #region Sales Invoice Methods (Faturalama)

        /// <summary>
        /// İrsaliyeden satış faturası oluştur
        /// POST /invoices/sales?invoiceType=8
        /// </summary>
        public async Task<JplatformApiResponse> CreateSalesInvoiceAsync(JplatformSalesInvoice invoice)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/invoices/sales?invoiceType=8";

            try
            {
                var authToken = await GetAuthTokenAsync();

                var json = JsonSerializer.Serialize(invoice, _jsonOptions);
                _logger.LogDebug("POST {Endpoint} - Request: {Json}", endpoint, json);

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("auth-token", authToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Response ({StatusCode}): {Content}", response.StatusCode, responseContent);

                // 401 → token sıfırla ve tekrar dene
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized (SalesInvoice), clearing token and retrying...");
                    _authToken = null;
                    _tokenExpiry = DateTime.MinValue;
                    return await CreateSalesInvoiceRetryAsync(invoice);
                }

                return ParseApiResponse(response, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Error (SalesInvoice)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Connection error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = "Request timeout"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error (SalesInvoice)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<JplatformApiResponse> CreateSalesInvoiceRetryAsync(JplatformSalesInvoice invoice)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/invoices/sales?invoiceType=8";

            var authToken = await GetAuthTokenAsync();
            var json = JsonSerializer.Serialize(invoice, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("auth-token", authToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            return ParseApiResponse(response, responseContent);
        }

        /// <summary>
        /// Fatura oluşturulduktan sonra kampanya uygula
        /// PUT /invoices/sales/applyCampaign?invoiceType=8&amp;canSaveAppliedCampaign=true
        /// </summary>
        public async Task<JplatformApiResponse> ApplyCampaignAsync(ApplyCampaignRequest request)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/invoices/sales/applyCampaign?invoiceType=8&canSaveAppliedCampaign=true";

            try
            {
                var authToken = await GetAuthTokenAsync();

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                _logger.LogDebug("PUT {Endpoint} - Request: {Json}", endpoint, json);

                var httpRequest = new HttpRequestMessage(HttpMethod.Put, endpoint);
                httpRequest.Headers.Add("auth-token", authToken);
                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("ApplyCampaign Response ({StatusCode}): {Content}", response.StatusCode, responseContent);

                // 401 → token sıfırla ve tekrar dene
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Unauthorized (ApplyCampaign), clearing token and retrying...");
                    _authToken = null;
                    _tokenExpiry = DateTime.MinValue;
                    return await ApplyCampaignRetryAsync(request);
                }

                return ParseApiResponse(response, responseContent);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Error (ApplyCampaign)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Connection error: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = "Request timeout"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error (ApplyCampaign)");
                return new JplatformApiResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<JplatformApiResponse> ApplyCampaignRetryAsync(ApplyCampaignRequest request)
        {
            const string endpoint = "/logo/restservices/rest/v2.0/invoices/sales/applyCampaign?invoiceType=8&canSaveAppliedCampaign=true";

            var authToken = await GetAuthTokenAsync();
            var json = JsonSerializer.Serialize(request, _jsonOptions);

            var httpRequest = new HttpRequestMessage(HttpMethod.Put, endpoint);
            httpRequest.Headers.Add("auth-token", authToken);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            return ParseApiResponse(response, responseContent);
        }

        #endregion

    }
}
