using PlayerSignupBlazor.Models;
using PlayerSignupBlazor.Models.PlayerSignup;
using PlayerSignupBlazor.Services;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerSignupBlazor.Helpers
{
    public class FakeBackendHandler : HttpClientHandler
    {
        private ILocalStorageService _localStorageService;

        public FakeBackendHandler(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // array in local storage for registered players
            var playersKey = "players-signup-blazor";
            var players = await _localStorageService.GetItem<List<PlayerRecord>>(playersKey) ?? new List<PlayerRecord>();
            var method = request.Method;
            var path = request.RequestUri.AbsolutePath;            

            return await handleRoute();

            async Task<HttpResponseMessage> handleRoute()
            {
                if (path == "/players/authenticate" && method == HttpMethod.Post)
                    return await authenticate();
                if (path == "/players/signup" && method == HttpMethod.Post)
                    return await signup();
                if (path == "/players" && method == HttpMethod.Get)
                    return await getPlayers();
                if (Regex.Match(path, @"\/players\/\d+$").Success && method == HttpMethod.Get)
                    return await getPlayerById();
                if (Regex.Match(path, @"\/players\/\d+$").Success && method == HttpMethod.Put)
                    return await updatePlayer();
                if (Regex.Match(path, @"\/players\/\d+$").Success && method == HttpMethod.Delete)
                    return await deletePlayer();
                
                // pass through any requests not handled above
                return await base.SendAsync(request, cancellationToken);
            }

            // route functions
            
            async Task<HttpResponseMessage> authenticate()
            {
                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<Login>(bodyJson);
                var user = players.FirstOrDefault(x => x.Username == body.Username && x.Password == body.Password);

                if (user == null)
                    return await error("Username or password is incorrect");

                return await ok(new {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = "fake-jwt-token"
                });
            }

            async Task<HttpResponseMessage> signup()
            {
                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<AddPlayer>(bodyJson);

                if (players.Any(x => x.Username == body.Username))
                    return await error($"Username '{body.Username}' is already taken");

                var user = new PlayerRecord {
                    Id = players.Count > 0 ? players.Max(x => x.Id) + 1 : 1,
                    Username = body.Username,
                    Password = body.Password,
                    FirstName = body.FirstName,
                    LastName = body.LastName
                };

                players.Add(user);

                await _localStorageService.SetItem(playersKey, players);
                
                return await ok();
            }

            async Task<HttpResponseMessage> getPlayers()
            {
                if (!isLoggedIn()) return await unauthorized();
                return await ok(players.Select(x => basicDetails(x)));
            }

            async Task<HttpResponseMessage> getPlayerById()
            {
                if (!isLoggedIn()) return await unauthorized();

                var user = players.FirstOrDefault(x => x.Id == idFromPath());
                return await ok(basicDetails(user));
            }

            async Task<HttpResponseMessage> updatePlayer() 
            {
                if (!isLoggedIn()) return await unauthorized();

                var bodyJson = await request.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<EditPlayer>(bodyJson);
                var user = players.FirstOrDefault(x => x.Id == idFromPath());

                // if username changed check it isn't already taken
                if (user.Username != body.Username && players.Any(x => x.Username == body.Username))
                    return await error($"Username '{body.Username}' is already taken");

                // only update password if entered
                if (!string.IsNullOrWhiteSpace(body.Password))
                    user.Password = body.Password;

                // update and save user
                user.Username = body.Username;
                user.FirstName = body.FirstName;
                user.LastName = body.LastName;
                await _localStorageService.SetItem(playersKey, players);

                return await ok();
            }

            async Task<HttpResponseMessage> deletePlayer()
            {
                if (!isLoggedIn()) return await unauthorized();

                players.RemoveAll(x => x.Id == idFromPath());
                await _localStorageService.SetItem(playersKey, players);

                return await ok();
            }

            // helper functions

            async Task<HttpResponseMessage> ok(object body = null)
            {
                return await jsonResponse(HttpStatusCode.OK, body ?? new {});
            }

            async Task<HttpResponseMessage> error(string message)
            {
                return await jsonResponse(HttpStatusCode.BadRequest, new { message });
            }

            async Task<HttpResponseMessage> unauthorized()
            {
                return await jsonResponse(HttpStatusCode.Unauthorized, new { message = "Unauthorized" });
            }

            async Task<HttpResponseMessage> jsonResponse(HttpStatusCode statusCode, object content)
            {
                var response = new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
                };
                
                // delay to simulate real api call
                await Task.Delay(500);

                return response;
            }

            bool isLoggedIn()
            {
                return request.Headers.Authorization?.Parameter == "fake-jwt-token";
            } 

            int idFromPath()
            {
                return int.Parse(path.Split('/').Last());
            }

            dynamic basicDetails(PlayerRecord user)
            {
                return new {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };
            }
        }
    }

    // class for user records stored by fake backend

    public class PlayerRecord {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}