using CustomHttpServer.Framework.Attributes;

namespace CustomHttpServer.Controllers;

[Controller("auth")]
public class AuthController
{
    [Post("login")]
    public void Login(string email, string password)
    {
        Console.WriteLine($"[AuthController] Пришли данные авторизации:");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Password: {password}");
    }
}