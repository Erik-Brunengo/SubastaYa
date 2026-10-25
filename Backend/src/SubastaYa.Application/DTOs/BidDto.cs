namespace SubastaYa.Application.DTOs
{
    public record RegistrarPujaDto(
    int SubastaId,
    int? UsuarioId,
    decimal Monto
    );

    public record PujaResponseDto(
        int Id,
        int SubastaId,
        int UsuarioId,
        decimal Monto,
        DateTime FechaHora,
        DateTime NuevaFechaFin,
        bool TiempoExtendido
    );
}
