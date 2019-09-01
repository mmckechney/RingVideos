using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Ring.Models;

namespace RingVideos
{
    /// <summary>
    /// Provides authenticated access to the Ring API. The <see cref="AuthToken"/> can be used to create future instances.
    /// </summary>
    public class RingClient
    {
        /// <summary>
        /// The absolute URI for the Ring OAuth Provider.
        /// </summary>
        private const string OAuthUri = "https://oauth.ring.com/oauth/token";

        /// <summary>
        /// The API version used for the Ring API.
        /// </summary>
        private const string ApiVersion = "9";

        /// <summary>
        /// The base absolute URI for the Ring API.
        /// </summary>
        private const string ApiUri = "https://api.ring.com";

        /// <summary>
        /// The relative URI used to create a new session.
        /// </summary>
        private const string NewSessionUri = "/clients_api/session";
        /// <summary>
        /// The relative URI used to access the user's Ring devices.
        /// </summary>
        private const string DevicesUri = "/clients_api/ring_devices";
        /// <summary>
        /// The relative URI used to access the user's active dings.
        /// </summary>
        private const string ActiveDingsUri = "/clients_api/dings/active";
        /// <summary>
        /// The relative URI used to access the user's ding history.
        /// </summary>
        private const string DingHistoryUri = "/clients_api/doorbots/history";
        /// <summary>
        /// The relative URI used to access the recording of a specific ding.
        /// </summary>
        private const string DingRecordingUri = "/clients_api/dings/{id}/recording";

        /// <summary>
        /// JSON data sent with the new session request to authenticate the client with the Ring API.
        /// </summary>
        private readonly string NewSessionJson = $"{{ \"device\": {{ \"hardware_id\": \"{Guid.NewGuid()}\", \"metadata\": {{ \"api_version\": \"{ApiVersion}\" }}, \"os\": \"android\" }} }}";

        /// <summary>
        /// The auth token used to authenticate requests against the Ring API.
        /// </summary>
        public string AuthToken { get; private set; }

        /// <summary>
        /// Data sent with subsequent requests to authenticate the client with the Ring API.
        /// </summary>
        private Dictionary<string, string> AuthedSessionData
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { "api_version", ApiVersion },
                    { "auth_token", AuthToken }
                };
            }
        }

        /// <summary>
        /// Create an instance without any initialization.
        /// </summary>
        private RingClient()
        {
        }

        /// <summary>
        /// Create an authenticated connection to the Ring API using an auth token. Use <see cref="CreateAsync(string)"/> to create an instance asynchronously.
        /// </summary>
        /// <param name="authToken">Ring API auth token</param>
        public RingClient(string authToken)
        {
            Initialize(authToken).Wait();
        }

        /// <summary>
        /// Create an authenticated connection to the Ring API using a Ring account username and password. Use <see cref="CreateAsync(string, string)"/> to create an instance asynchronously. This should only be used when an auth token has not been created or has expired.
        /// </summary>
        /// <param name="username">Ring account username</param>
        /// <param name="password">Ring account password</param>
        public RingClient(string username, string password)
        {
            Initialize(username, password).Wait();
        }

        /// <summary>
        /// Asynchronously create an authenticated connection to the Ring API using an auth token.
        /// </summary>
        /// <param name="authToken">Ring API auth token</param>
        /// <returns></returns>
        public static async Task<RingClient> CreateAsync(string authToken)
        {
            var ringClient = new RingClient();

            await ringClient.Initialize(authToken);

            return ringClient;
        }

        /// <summary>
        /// Asynchronously create an authenticated connection to the Ring API using a Ring account username and password. This should only be used when an auth token has not been created or has expired.
        /// </summary>
        /// <param name="username">Ring account username</param>
        /// <param name="password">Ring account password</param>
        /// <returns></returns>
        public static async Task<RingClient> CreateAsync(string username, string password)
        {
            var ringClient = new RingClient();

            await ringClient.Initialize(username, password);

            return ringClient;
        }

        /// <summary>
        /// Initialize the connection to the Ring API using an auth token.
        /// </summary>
        /// <param name="authToken">Ring API auth token</param>
        /// <returns></returns>
        private async Task Initialize(string authToken)
        {
            AuthToken = authToken;

            var response = await SendRequestAsync(HttpMethod.Get, DevicesUri, AuthedSessionData);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring API returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring API returned the following error: " + response.ReasonPhrase);
                }
            }
        }

        /// <summary>
        /// Initialize the connection to the Ring API using a Ring account username and password.
        /// </summary>
        /// <param name="username">Ring account username</param>
        /// <param name="password">Ring account password</param>
        /// <returns></returns>
        private async Task Initialize(string username, string password)
        {
            var response = await Authorize(username, password);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring API returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring API returned the following error: " + response.ReasonPhrase);
                }
            }

            var jsonObject = JObject.Parse(await response.Content.ReadAsStringAsync());
            var authToken = (string)jsonObject["profile"]["authentication_token"];

            if (authToken == null || authToken.Length <= 0)
            {
                throw new SecurityException("The Ring API did not return the auth token.");
            }

            AuthToken = authToken;

            response = await SendRequestAsync(HttpMethod.Get, DevicesUri, AuthedSessionData);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring API returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring API returned the following error: " + response.ReasonPhrase);
                }
            }
        }

        /// <summary>
        /// Sends a request to the Ring API asynchronously.
        /// </summary>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="relativeUri">The relative URI to send the request to.</param>
        /// <param name="data">The data to send as part of the request.</param>
        /// <param name="autoRedirect">Specifies if the client should automatically redirect if requested.</param>
        /// <returns>The response received from the Ring API.</returns>
        private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string relativeUri, IReadOnlyDictionary<string, string> data, bool autoRedirect = true)
        {
            var httpHandler = new HttpClientHandler();
            httpHandler.AllowAutoRedirect = autoRedirect;
            httpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var httpClient = new HttpClient(httpHandler);
            httpClient.BaseAddress = new Uri(ApiUri, UriKind.Absolute);

            if (method == HttpMethod.Get)
            {
                var queryString = "?" + await new FormUrlEncodedContent(data).ReadAsStringAsync();
                return await httpClient.GetAsync(new Uri(relativeUri + queryString, UriKind.Relative));
            }
            else if (method == HttpMethod.Post)
            {
                return await httpClient.PostAsync(new Uri(relativeUri, UriKind.Relative), new FormUrlEncodedContent(data));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Sends an authentication request to the Ring OAuth provider and API asynchronously.
        /// </summary>
        /// <param name="username">The username used to authenticate.</param>
        /// <param name="password">The password used to authenticate.</param>
        /// <returns>The response received from the Ring API.</returns>
        private async Task<HttpResponseMessage> Authorize(string username, string password)
        {
            var httpHandler = new HttpClientHandler();
            httpHandler.AllowAutoRedirect = true;
            httpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var httpClient = new HttpClient(httpHandler);

            string json = $"{{ \"client_id\": \"ring_official_android\", \"grant_type\": \"password\", \"password\": \"{password}\", \"scope\": \"client\", \"username\": \"{username}\" }}";

            var response = await httpClient.PostAsync(OAuthUri, new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring OAuth provider returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring OAuth provider returned the following error: " + response.ReasonPhrase);
                }
            }

            var jsonObject = JObject.Parse(await response.Content.ReadAsStringAsync());
            var accessToken = (string)jsonObject["access_token"];

            httpClient = new HttpClient(httpHandler);

            httpClient.BaseAddress = new Uri(ApiUri, UriKind.Absolute);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await httpClient.PostAsync(new Uri(NewSessionUri, UriKind.Relative), new StringContent(NewSessionJson, Encoding.UTF8, "application/json"));
        }

        /// <summary>
        /// Gets a list containing the devices that the user has access to.
        /// </summary>
        /// <returns>The list of devices that the user has access to.</returns>
        public async Task<List<Device>> GetDevicesAsync()
        {
            var response = await SendRequestAsync(HttpMethod.Get, DevicesUri, AuthedSessionData);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring API returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring API returned the following error: " + response.ReasonPhrase);
                }
            }

            var devices = new List<Device>();

            var jsonObject = JObject.Parse(await response.Content.ReadAsStringAsync());

            foreach (var kvp in jsonObject)
            {
                DeviceType type;

                if (kvp.Key == "doorbots")
                {
                    type = DeviceType.Doorbell;
                }
                else if (kvp.Key == "authorized_doorbots")
                {
                    type = DeviceType.AuthorizedDoorbell;
                }
                else if (kvp.Key == "chimes")
                {
                    type = DeviceType.Chime;
                }
                else if (kvp.Key == "stickup_cams")
                {
                    type = DeviceType.Cam;
                }
                else
                {
                    type = DeviceType.Unknown;
                }

                foreach (var token in kvp.Value)
                {
                    devices.Add(new Device()
                    {
                        Id = (ulong)token["id"],
                        Description = (string)token["description"],
                        FirmwareVersion = (string)token["firmware_version"],
                        Address = (string)token["address"],
                        Latitude = (double)token["latitude"],
                        Longitude = (double)token["longitude"],
                        TimeZone = (string)token["time_zone"],
                        BatteryLife = (int?)token["battery_life"] ?? -1,
                        Type = type
                    });
                }
            }

            return devices;
        }

        /// <summary>
        /// Gets a list containing the active dings that the user has access to.
        /// </summary>
        /// <returns>The list of active dings that the user has access to.</returns>
        public async Task<List<ActiveDing>> GetActiveDingsAsync()
        {
            var response = await SendRequestAsync(HttpMethod.Get, ActiveDingsUri, AuthedSessionData);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring API returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring API returned the following error: " + response.ReasonPhrase);
                }
            }

            var devices = await GetDevicesAsync();

            var activeDings = new List<ActiveDing>();

            var jsonArray = JArray.Parse(await response.Content.ReadAsStringAsync());

            foreach (var token in jsonArray.Children())
            {
                DingType type;

                var kind = (string)token["kind"];

                if (kind == "motion")
                {
                    type = DingType.Motion;
                }
                else if (kind == "ding")
                {
                    type = DingType.Ring;
                }
                else
                {
                    type = DingType.Unknown;
                }

                activeDings.Add(new ActiveDing()
                {
                    Id = (ulong)token["id"],
                    Device = devices.Where(d => d.Id == (ulong)token["doorbot_id"]).FirstOrDefault(),
                    Type = type
                });
            }

            return activeDings;
        }

        /// <summary>
        /// Gets a list containing the recent dings that the user has access to.
        /// </summary>
        /// <param name="limit">The maximum number of dings to list.</param>
        /// <returns>The list of recent dings that the user has access to.</returns>
        public async Task<List<Ding>> GetDingsAsync(int limit = 30)
        {
            var data = AuthedSessionData;
            data.Add("limit", limit.ToString());

            var response = await SendRequestAsync(HttpMethod.Get, DingHistoryUri, data);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring API returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring API returned the following error: " + response.ReasonPhrase);
                }
            }

            var devices = await GetDevicesAsync();

            var dings = new List<Ding>();

            var jsonArray = JArray.Parse(await response.Content.ReadAsStringAsync());

            foreach (var token in jsonArray.Children())
            {
                DingType type;

                var kind = (string)token["kind"];

                if (kind == "motion")
                {
                    type = DingType.Motion;
                }
                else if (kind == "ding")
                {
                    type = DingType.Ring;
                }
                else
                {
                    type = DingType.Unknown;
                }

                dings.Add(new Ding()
                {
                    Id = (ulong)token["id"],
                    CreatedAt = DateTime.Parse((string)token["created_at"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
                    Answered = (bool)token["answered"],
                    RecordingIsReady = ((string)token["recording"]["status"] == "ready"),
                    Device = devices.Where(d => d.Id == (ulong)token["doorbot"]["id"]).FirstOrDefault(),
                    Type = type
                });
            }

            return dings;
        }

        /// <summary>
        /// Gets the URI of the recording for the specified ding.
        /// </summary>
        /// <param name="ding">The ding to get the recording URI for.</param>
        /// <returns>The URI for the recording of the provided ding.</returns>
        public async Task<Uri> GetRecordingUriAsync(Ding ding)
        {
            if (!ding.RecordingIsReady)
            {
                throw new ArgumentException("The provided ding does not have a recording available.");
            }

            var uri = DingRecordingUri.Replace("{id}", ding.Id.ToString());

            var response = await SendRequestAsync(HttpMethod.Get, uri, AuthedSessionData, false);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Found)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new SecurityException("The Ring API returned the following error: " + response.ReasonPhrase);
                }
                else
                {
                    throw new Exception("The Ring API returned the following error: " + response.ReasonPhrase);
                }
            }

            return response.Headers.Location;
        }
    }
}