using Microsoft.AspNetCore.Identity;

namespace LoteriaProject.Model
{
    public class User
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required UserRole Role { get; set; }

    }
    public enum UserRole
    {
        Admin,
        User
    }
}
