namespace EcommerceApp.Models
{
    public class AdminDashboardViewModel
    {

        public int TotalPedidos { get; set; }

        public decimal VentasTotales { get; set; }

        public int PedidosPagados { get; set; }

        public int PedidosPendientes { get; set; }


        public List<VentaMensualViewModel> VentasPorMes { get; set; }
            = new List<VentaMensualViewModel>();


        public List<ProductoSolicitadoViewModel> ProductosMasSolicitados { get; set; }
            = new List<ProductoSolicitadoViewModel>();


        public List<PedidoReporteViewModel> Pedidos { get; set; }
            = new List<PedidoReporteViewModel>();


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