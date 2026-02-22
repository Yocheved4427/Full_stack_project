using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    
    public record UserDTO
    (
        int Id,
        
        string FirstName,

        string LastName,

        string Email,

        string Password,
        
        bool IsAdmin

        
    )
    {
    public UserDTO() : this(0, string.Empty, string.Empty, string.Empty, string.Empty, false)
    {
    }

}
};
