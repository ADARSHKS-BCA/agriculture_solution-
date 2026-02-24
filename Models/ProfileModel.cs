using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace Agriculture.Models
{
    [Table("profiles")]
    public class ProfileModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Column("username")]
        public string? Username { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
