using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.Mappings;

public static class AuctionMappings
{
    public static AuctionDto ToDto(this Subasta subasta)
    {
        return new AuctionDto
        {
            Id = subasta.Id,
            Titulo = subasta.Titulo,
            Descripcion = subasta.Descripcion,
            UrlImagen = subasta.UrlImagen,
            CategoriaNombre = subasta.Categoria?.Nombre ?? string.Empty,
            PrecioBase = subasta.PrecioBase,
            IncrementoMinimo = subasta.IncrementoMinimo,
            OfertaMasAlta = subasta.Pujas.Any() ? subasta.Pujas.Max(p => p.Monto) : 0,
            CantidadOfertas = subasta.Pujas.Count,
            FechaInicio = subasta.FechaInicio,
            FechaFin = subasta.FechaFin,
            Estado = subasta.Estado,
            HoraServidor = DateTime.UtcNow,
            Pujas = subasta.Pujas
                .Select(p => p.ToInfoDto())
                .ToList()
        };
    }

    public static PujaInfoDto ToInfoDto(this Puja puja) =>
        new(puja.Id, puja.CompradorId, puja.Monto, puja.FechaPuja);

    public static PujaResponseDto ToResponseDto(this Puja puja, int subastaId, DateTime nuevaFechaFin, bool tiempoExtendido) =>
        new(
            puja.Id,
            subastaId,
            puja.CompradorId,
            puja.Monto,
            puja.FechaPuja,
            nuevaFechaFin,
            tiempoExtendido);
}
