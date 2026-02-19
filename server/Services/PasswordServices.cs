using Zxcvbn;
namespace Services
{
    public class PasswordServices : IPasswordServices
    {
        public int PasswordScore(string password)
        {
            var result = Zxcvbn.Core.EvaluatePassword(password);
            return result.Score;
        }
    }
}
