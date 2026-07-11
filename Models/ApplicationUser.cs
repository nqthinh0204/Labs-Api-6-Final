using Microsoft.AspNetCore.Identity;

namespace AspLab06Final.Mvc.Models;

// ApplicationUser mở rộng IdentityUser (Lab06 - Identity + Role-based Authorization)
// IdentityUser đã có sẵn: Id, UserName, Email, PasswordHash, ... nên không cần khai báo lại.
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
