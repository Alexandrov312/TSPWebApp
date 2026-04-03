using Microsoft.AspNetCore.Identity;
using WebApp23621759.Models.Entities;

namespace WebApp23621759.Services
{
	public class PasswordService
	{
		private static readonly PasswordHasher<User> _hasher = new();

		public static string Hash(string password)
		{
			return _hasher.HashPassword(null!, password);
		}

		//Проверява дали въведената парола съответства на дадения хеш
		public static bool Verify(string hashedPassword, string inputPassword)
		{
			var result = _hasher.VerifyHashedPassword(null!, hashedPassword, inputPassword);
			return result == PasswordVerificationResult.Success;
		}
	}
}
