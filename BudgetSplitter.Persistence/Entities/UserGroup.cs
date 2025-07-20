using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence;

public class UserGroup
{
    [Key, Column(Order = 0)]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Key, Column(Order = 1)]
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
}