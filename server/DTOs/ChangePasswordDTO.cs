namespace DTOs
{
    public record ChangePasswordDTO
    (
        int UserId,
        string CurrentPassword,
        string NewPassword
    );
}
