using Microsoft.AspNetCore.Mvc;

namespace Services
{
    public interface IPasswordServices
    {
        public int PasswordScore(string password);
    }
}