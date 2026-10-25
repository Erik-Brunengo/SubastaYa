using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Application.DTOs;

public sealed class CreateDepositRequestDto
{
    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(typeof(decimal), "1", "10000000", ErrorMessage = "El monto debe ser entre $1 y $10.000.000.")]
    public decimal? Monto { get; init; }
}
