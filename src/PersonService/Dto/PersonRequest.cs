using System.ComponentModel.DataAnnotations;

namespace PersonService.Dto;

/// <summary>
/// Request payload for create (POST) and partial update (PATCH).
/// A <c>null</c> property means "leave untouched" on PATCH, so nothing here is
/// annotated as [Required]; presence of <see cref="Name"/> is enforced by the
/// create endpoint only.
/// </summary>
public class PersonRequest
{
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }

    [Range(1, 200)]
    public int? Age { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(255)]
    public string? Work { get; set; }
}
