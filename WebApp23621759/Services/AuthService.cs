using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using WebApp23621759.Models.Entities;

namespace WebApp23621759.Services
{
    public class AuthService
    {
        public async Task SignInAsync(HttpContext context, User user)
        {
            //Списък с данни за логнатия потребител - тип:стойност
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            };

            //Данните на потребителя ще се запазят чрез cookie authentication
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            //Обектът, който представя логнатия потребител като цяло.
            var principal = new ClaimsPrincipal(identity);

            //Логва потребителя в системата и запазва информацията за него чрез cookie
            //1. ASP.NET взима principal
            //2. Сериализира данните за него
            //3. Създава authentication cookie
            //4. Изпраща cookie към браузъра
            //5. Браузърът я пази
            //6. При следваща заявка браузърът я изпраща обратно
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}
