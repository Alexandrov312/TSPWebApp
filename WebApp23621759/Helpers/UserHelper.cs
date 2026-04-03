using System.Security.Claims;

namespace WebApp23621759.Helpers
{
    public class UserHelper
    {
        public static int GetUserId(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userId);
        }
    }
}
