namespace EcommerceApp.Models
{
    public class AdminDashboardViewModel
    {
        // =========================
        // RESUMEN GENERAL
        // =========================

        public int TotalPedidos { get; set; }

        public decimal VentasTotales { get; set; }

        public int PedidosPagados { get; set; }

        public int PedidosPendientes { get; set; }


        // =========================
        // VENTAS POR MES
        // =========================

        public List<VentaMensualViewModel> VentasPorMes { get; set; }
            = new List<VentaMensualViewModel>();


        // =========================
        // PRODUCTOS MÁS SOLICITADOS
        // =========================

        public List<ProductoSolicitadoViewModel> ProductosMasSolicitados { get; set; }
            = new List<ProductoSolicitadoViewModel>();


        // =========================
        // DETALLE DE PEDIDOS
        // =========================

        public List<PedidoReporteViewModel> Pedidos { get; set; }
            = new List<PedidoReporteViewModel>();


        // =========================
        // FECHA DEL REPORTE
        // =========================

        public DateTime FechaGeneracion { get; set; }
            = DateTime.Now;
    }


    public class VentaMensualViewModel
    {
        public string Mes { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public int Pedidos { get; set; }
    }


    public class ProductoSolicitadoViewModel
    {
        public string Nombre { get; set; } = string.Empty;

        public int Cantidad { get; set; }
    }


    public class PedidoReporteViewModel
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        public string Cliente { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public string EstadoPago { get; set; } = string.Empty;

        public string Entrega { get; set; } = string.Empty;

        public string EstadoPedido { get; set; } = string.Empty;
    }
}