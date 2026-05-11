using ActiveCitizen.Web.Models;
using ActiveCitizen.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActiveCitizen.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiClient _apiClient;

        public AccountController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            try
            {
                var loginRequest = new {Email = model.Email, Password = model.Password};
                var loginResponse = await _apiClient.PostAsync<LoginResponse>("api/auth/Login", loginRequest);

                string token = loginResponse.Token;
                int userId = loginResponse.User.Id;
                string userEmail = loginResponse.User.Email;
                string fullName = loginResponse.User.FullName;
                string phoneNumber = loginResponse.User.PhoneNumber;
                int? districtId = loginResponse.User.Id;
                string role = loginResponse.User.Role;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, fullName ?? userEmail),
                    new Claim(ClaimTypes.Email, userEmail),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("AccessToken", token)
                };

                if(!string.IsNullOrEmpty(phoneNumber))
                {
                    claims.Add(new Claim(ClaimTypes.MobilePhone, phoneNumber));
                }

                if(districtId.HasValue) 
                    claims.Add(new Claim("DistrictId", districtId.Value.ToString()));

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (role.Equals("Inspector", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Inspector");
                }
                else
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Не удалось войти. Проверьте почту и пароль.");
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Произошла ошибка при входе. Попробуйте позже.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
