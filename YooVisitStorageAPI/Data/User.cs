using System.ComponentModel.DataAnnotations;

namespace YooVisitStorageAPI.Data;

public class UserApplication
{
    [Key]
    public Guid IdUtilisateur { get; set; }

    public string Email { get; set; }

    public string HashedPassword { get; set; }

    public DateTime DateInscription { get; set; }
}
