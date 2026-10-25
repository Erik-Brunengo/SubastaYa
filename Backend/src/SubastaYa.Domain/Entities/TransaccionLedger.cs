using SubastaYa.Domain.Enums;

namespace SubastaYa.Domain.Entities
{
    public class TransaccionLedger
    {
        public int Id { get; init; }
        public int BilleteraId { get; init; }
        public TipoTransaccion Tipo { get; init; }
        public decimal Monto { get; init; }
        public DateTime Fecha { get; init; } = DateTime.UtcNow;
        public int? SubastaId { get; init; }

        // NavegaciÃ³n
        public Billetera Billetera { get; init; } = null!;
        public Subasta? Subasta { get; init; }
    }
}
