using PlayerSignupBlazor.Models;
using PlayerSignupBlazor.Models.PlayerSignup;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlayerSignupBlazor.Services
{
    public interface IPlayerService
    {
        Player Player { get; }
        Task Initialize();
        Task Login(Login model);
        Task Logout();
        Task Signup(AddPlayer model);
        Task<IList<Player>> GetAll();
        Task<Player> GetById(string id);
        Task Update(string id, EditPlayer model);
        Task Delete(string id);
    }

    public class PlayerService : IPlayerService
    {
        private IHttpService _httpService;
        private NavigationManager _navigationManager;
        private ILocalStorageService _localStorageService;
        private string _userKey = "user";

        public Player Player { get; private set; }

        public PlayerService(
            IHttpService httpService,
            NavigationManager navigationManager,
            ILocalStorageService localStorageService
        ) {
            _httpService = httpService;
            _navigationManager = navigationManager;
            _localStorageService = localStorageService;
        }

        public async Task Initialize()
        {
            Player = await _localStorageService.GetItem<Player>(_userKey);
        }

        public async Task Login(Login model)
        {
            Player = await _httpService.Post<Player>("/players/authenticate", model);
            await _localStorageService.SetItem(_userKey, Player);
        }

        public async Task Logout()
        {
            Player = null;
            await _localStorageService.RemoveItem(_userKey);
            _navigationManager.NavigateTo("player/login");
        }

        public async Task Signup(AddPlayer model)
        {
            await _httpService.Post("/players/signup", model);
        }

        public async Task<IList<Player>> GetAll()
        {
            return await _httpService.Get<IList<Player>>("/players");
        }

        public async Task<Player> GetById(string id)
        {
            return await _httpService.Get<Player>($"/players/{id}");
        }

        public async Task Update(string id, EditPlayer model)
        {
            await _httpService.Put($"/players/{id}", model);

            // update stored user if the logged in user updated their own record
            if (id == Player.Id) 
            {
                // update local storage
                Player.FirstName = model.FirstName;
                Player.LastName = model.LastName;
                Player.Username = model.Username;
                await _localStorageService.SetItem(_userKey, Player);
            }
        }

        public async Task Delete(string id)
        {
            await _httpService.Delete($"/players/{id}");

            // auto logout if the logged in user deleted their own record
            if (id == Player.Id)
                await Logout();
        }
    }
}