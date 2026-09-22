using EcommerceApp.Data;
using EcommerceApp.Models;
using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FastReport.Utils;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController(ApplicationDbContext context) : Controller
    {
        // =============================================================
        // VISTA PRINCIPAL DE REPORTES
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            // =========================================================
            // RESUMEN GENERAL
            // =========================================================

            int totalPedidos = orders.Count;

            decimal ventasTotales = orders.Sum(o => o.Total);

            int pedidosPagados = orders.Count(o =>
                string.Equals(
                    o.PaymentStatus,
                    "Pagado",
                    StringComparison.OrdinalIgnoreCase));

            int pedidosPendientes = orders.Count(o =>
                string.Equals(
                    o.PaymentStatus,
                    "Pendiente",
                    StringComparison.OrdinalIgnoreCase));

            // =========================================================
            // VENTAS POR MES
            // =========================================================

            var ventasPorMes = new List<VentaMensualViewModel>();

            DateTime hoy = DateTime.Now;

            for (int i = 4; i >= 0; i--)
            {
                DateTime fecha = hoy.AddMonths(-i);

                int año = fecha.Year;
                int mes = fecha.Month;

                var pedidosMes = orders
                    .Where(o =>
                        o.OrderDate.ToLocalTime().Year == año &&
                        o.OrderDate.ToLocalTime().Month == mes)
                    .ToList();

                ventasPorMes.Add(new VentaMensualViewModel
                {
                    Mes = fecha.ToString("MMM"),
                    Total = pedidosMes.Sum(o => o.Total),
                    Pedidos = pedidosMes.Count
                });
            }

            // =========================================================
            // PRODUCTOS MÁS SOLICITADOS
            // =========================================================

            var productosMasSolicitados = orders
                .SelectMany(o => o.OrderDetails)
                .Where(d => d.Product != null)
                .GroupBy(d => d.Product!.Name)
                .Select(g => new ProductoSolicitadoViewModel
                {
                    Nombre = g.Key,
                    Cantidad = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(4)
                .ToList();

            // =========================================================
            // DETALLE DE PEDIDOS
            // =========================================================

            var pedidos = orders
                .Select(o => new PedidoReporteViewModel
                {
                    Id = o.Id,

                    Fecha = o.OrderDate,

                    Cliente = o.User?.FullName
                              ?? o.User?.Email
                              ?? "Sin cliente",

                    Total = o.Total,

                    MetodoPago = o.PaymentMethod ?? "-",

                    EstadoPago = o.PaymentStatus ?? "-",

                    Entrega = o.DeliveryMethod ?? "-",

                    EstadoPedido = o.Status ?? "-"
                })
                .ToList();

            // =========================================================
            // CREAR EL MODELO QUE UTILIZA INDEX.CSHTML
            // =========================================================

            var modelo = new AdminDashboardViewModel
            {
                TotalPedidos = totalPedidos,

                VentasTotales = ventasTotales,

                PedidosPagados = pedidosPagados,

                PedidosPendientes = pedidosPendientes,

                VentasPorMes = ventasPorMes,

                ProductosMasSolicitados = productosMasSolicitados,

                Pedidos = pedidos,

                FechaGeneracion = DateTime.Now
            };

            // =========================================================
            // ENVIAR EL MODELO A Views/Reports/Index.cshtml
            // =========================================================

            return View(modelo);
        }


        // =============================================================
        // GENERAR REPORTE PDF
        // =============================================================

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            // =========================================================
            // DATOS DEL REPORTE
            // =========================================================

            int totalPedidos = orders.Count;

            decimal totalVentas = orders.Sum(o => o.Total);

            int pedidosPagados = orders.Count(o =>
                string.Equals(
                    o.PaymentStatus,
                    "Pagado",
                    StringComparison.OrdinalIgnoreCase));

            int pedidosPendientes = totalPedidos - pedidosPagados;

            // =========================================================
            // DATASET PARA FASTREPORT
            // =========================================================

            var table = new DataTable("Pedidos");

            table.Columns.Add("Pedido", typeof(string));
            table.Columns.Add("Fecha", typeof(string));
            table.Columns.Add("Cliente", typeof(string));
            table.Columns.Add("Total", typeof(decimal));
            table.Columns.Add("MetodoPago", typeof(string));
            table.Columns.Add("EstadoPago", typeof(string));
            table.Columns.Add("Entrega", typeof(string));
            table.Columns.Add("EstadoPedido", typeof(string));

            foreach (var order in orders)
            {
                table.Rows.Add(
                    $"#{order.Id}",
                    order.OrderDate
                        .ToLocalTime()
                        .ToString("dd/MM/yyyy HH:mm"),
                    order.User?.FullName
                        ?? order.User?.Email
                        ?? "Sin cliente",
                    order.Total,
                    order.PaymentMethod ?? "-",
                    order.PaymentStatus ?? "-",
                    order.DeliveryMethod ?? "-",
                    order.Status ?? "-"
                );
            }

            var dataSet = new DataSet("PedidosData");
            dataSet.Tables.Add(table);

            // =========================================================
            // CREAR REPORTE
            // =========================================================

            using var report = new Report();

            var page = new ReportPage
            {
                Name = "PaginaReporte",
                PaperWidth = 210,
                PaperHeight = 297,
                Landscape = true,
                LeftMargin = 10,
                RightMargin = 10,
                TopMargin = 10,
                BottomMargin = 10
            };

            report.Pages.Add(page);

            float ancho = 277;

            // =========================================================
            // COLORES
            // =========================================================

            Color cafeOscuro = Color.FromArgb(78, 45, 32);
            Color cafe = Color.FromArgb(112, 70, 51);
            Color crema = Color.FromArgb(250, 242, 232);
            Color cremaOscura = Color.FromArgb(242, 225, 210);

            Color rosa = Color.FromArgb(235, 188, 175);
            Color rosaClaro = Color.FromArgb(250, 225, 216);

            Color naranja = Color.FromArgb(224, 137, 67);
            Color verde = Color.FromArgb(117, 151, 82);
            Color verdeClaro = Color.FromArgb(224, 235, 207);

            Color texto = Color.FromArgb(55, 38, 30);
            Color blanco = Color.White;
            Color gris = Color.FromArgb(105, 95, 88);
            Color linea = Color.FromArgb(220, 201, 188);

            // =========================================================
            // ENCABEZADO
            // =========================================================

            page.ReportTitle = new ReportTitleBand
            {
                Name = "Encabezado",
                Height = 36 * Units.Millimeters
            };

            AddShape(
                page.ReportTitle,
                "FondoEncabezado",
                0,
                0,
                ancho,
                36,
                crema,
                Color.Transparent);

            AddShape(
                page.ReportTitle,
                "LineaEncabezado",
                0,
                34,
                ancho,
                2,
                cafe,
                cafe);

            AddText(
                page.ReportTitle,
                "Marca",
                5,
                5,
                65,
                10,
                "DULCE ANTOJO",
                cafeOscuro,
                19,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "SubMarca",
                6,
                15,
                65,
                6,
                "ROLLS • COOKIES • TORTAS",
                cafe,
                7,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "TituloReporte",
                76,
                6,
                130,
                10,
                "REPORTE DE PEDIDOS Y VENTAS",
                cafeOscuro,
                16,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "Descripcion",
                76,
                16,
                130,
                7,
                "Resumen y detalle de los pedidos registrados en el sistema.",
                gris,
                8,
                FontStyle.Regular,
                HorzAlign.Left);

            AddShape(
                page.ReportTitle,
                "CajaFecha",
                220,
                5,
                52,
                24,
                rosaClaro,
                rosaClaro);

            AddText(
                page.ReportTitle,
                "FechaTitulo",
                224,
                8,
                44,
                5,
                "FECHA DE GENERACIÓN",
                cafe,
                6.5f,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "FechaValor",
                224,
                14,
                44,
                7,
                DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                texto,
                9,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "PeriodoTitulo",
                224,
                21,
                44,
                4,
                "PERÍODO: TODOS LOS PEDIDOS",
                gris,
                6.5f,
                FontStyle.Regular,
                HorzAlign.Left);

            // =========================================================
            // TARJETAS
            // =========================================================

            var tarjetas = new[]
            {
                new
                {
                    X = 0f,
                    Fondo = rosaClaro,
                    Acento = cafe,
                    Titulo = "TOTAL DE PEDIDOS",
                    Valor = totalPedidos.ToString()
                },
                new
                {
                    X = 69.25f,
                    Fondo = Color.FromArgb(250, 232, 207),
                    Acento = naranja,
                    Titulo = "VENTAS TOTALES",
                    Valor = $"Bs {totalVentas:N2}"
                },
                new
                {
                    X = 138.5f,
                    Fondo = verdeClaro,
                    Acento = verde,
                    Titulo = "PEDIDOS PAGADOS",
                    Valor = pedidosPagados.ToString()
                },
                new
                {
                    X = 207.75f,
                    Fondo = Color.FromArgb(250, 232, 207),
                    Acento = naranja,
                    Titulo = "PEDIDOS PENDIENTES",
                    Valor = pedidosPendientes.ToString()
                }
            };

            foreach (var tarjeta in tarjetas)
            {
                AddShape(
                    page.ReportTitle,
                    "Card" + tarjeta.X,
                    tarjeta.X,
                    39,
                    65.5f,
                    21,
                    tarjeta.Fondo,
                    tarjeta.Fondo);

                AddShape(
                    page.ReportTitle,
                    "Acento" + tarjeta.X,
                    tarjeta.X,
                    39,
                    3,
                    21,
                    tarjeta.Acento,
                    tarjeta.Acento);

                AddText(
                    page.ReportTitle,
                    "TituloCard" + tarjeta.X,
                    tarjeta.X + 7,
                    43,
                    53,
                    5,
                    tarjeta.Titulo,
                    gris,
                    6.5f,
                    FontStyle.Bold,
                    HorzAlign.Left);

                AddText(
                    page.ReportTitle,
                    "ValorCard" + tarjeta.X,
                    tarjeta.X + 7,
                    49,
                    53,
                    9,
                    tarjeta.Valor,
                    texto,
                    15,
                    FontStyle.Bold,
                    HorzAlign.Left);
            }

            // =========================================================
            // RESUMEN VISUAL
            // =========================================================

            AddText(
                page.ReportTitle,
                "TituloResumen",
                0,
                65,
                90,
                7,
                "RESUMEN DEL PERÍODO",
                cafeOscuro,
                11,
                FontStyle.Bold,
                HorzAlign.Left);

            AddShape(
                page.ReportTitle,
                "CajaEstado",
                0,
                74,
                135,
                34,
                crema,
                crema);

            AddText(
                page.ReportTitle,
                "EstadoTitulo",
                7,
                78,
                55,
                6,
                "ESTADO DE PAGOS",
                cafeOscuro,
                9,
                FontStyle.Bold,
                HorzAlign.Left);

            AddShape(
                page.ReportTitle,
                "BarraPagadosFondo",
                7,
                88,
                115,
                5,
                Color.FromArgb(235, 225, 216),
                Color.FromArgb(235, 225, 216));

            float porcentajePagados =
                totalPedidos == 0
                    ? 0
                    : 115f * pedidosPagados / totalPedidos;

            if (porcentajePagados > 0)
            {
                AddShape(
                    page.ReportTitle,
                    "BarraPagados",
                    7,
                    88,
                    porcentajePagados,
                    5,
                    verde,
                    verde);
            }

            AddText(
                page.ReportTitle,
                "PagadosTexto",
                7,
                94,
                55,
                5,
                $"Pagados: {pedidosPagados}",
                texto,
                7,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "PendientesTexto",
                67,
                94,
                55,
                5,
                $"Pendientes: {pedidosPendientes}",
                texto,
                7,
                FontStyle.Bold,
                HorzAlign.Left);

            AddShape(
                page.ReportTitle,
                "CajaVentas",
                142,
                74,
                135,
                34,
                Color.FromArgb(250, 235, 223),
                Color.FromArgb(250, 235, 223));

            AddText(
                page.ReportTitle,
                "VentasTitulo",
                149,
                78,
                55,
                6,
                "VENTAS ACUMULADAS",
                cafeOscuro,
                9,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "VentasValor",
                149,
                87,
                80,
                10,
                $"Bs {totalVentas:N2}",
                cafe,
                17,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportTitle,
                "VentasDescripcion",
                149,
                99,
                115,
                5,
                "Total correspondiente a los pedidos registrados.",
                gris,
                7,
                FontStyle.Regular,
                HorzAlign.Left);

            // =========================================================
            // TÍTULO DETALLE
            // =========================================================

            AddShape(
                page.ReportTitle,
                "CabeceraDetalle",
                0,
                113,
                ancho,
                9,
                cafeOscuro,
                cafeOscuro);

            AddText(
                page.ReportTitle,
                "TituloDetalle",
                6,
                114.5f,
                80,
                6,
                "DETALLE DE PEDIDOS",
                blanco,
                9,
                FontStyle.Bold,
                HorzAlign.Left);

            // =========================================================
            // CABECERA TABLA
            // =========================================================

            page.ColumnHeader = new ColumnHeaderBand
            {
                Name = "CabeceraTabla",
                Height = 9 * Units.Millimeters
            };

            AddShape(
                page.ColumnHeader,
                "FondoCabecera",
                0,
                0,
                ancho,
                9,
                Color.FromArgb(105, 65, 47),
                Color.FromArgb(105, 65, 47));

            string[] headers =
            {
                "N.º PEDIDO",
                "FECHA",
                "CLIENTE",
                "TOTAL",
                "MÉTODO DE PAGO",
                "ESTADO DE PAGO",
                "ENTREGA",
                "ESTADO DEL PEDIDO"
            };

            float[] posiciones =
            {
                0,
                24,
                59,
                108,
                133,
                174,
                212,
                242
            };

            float[] anchos =
            {
                24,
                35,
                49,
                25,
                41,
                38,
                30,
                35
            };

            for (int i = 0; i < headers.Length; i++)
            {
                AddText(
                    page.ColumnHeader,
                    "Header" + i,
                    posiciones[i],
                    1.5f,
                    anchos[i],
                    6,
                    headers[i],
                    blanco,
                    6.5f,
                    FontStyle.Bold,
                    HorzAlign.Center);
            }

            // =========================================================
            // DETALLE
            // =========================================================

            var dataBand = new DataBand
            {
                Name = "DetallePedidos",
                Height = 11 * Units.Millimeters,
                DataSource = null
            };

            page.Bands.Add(dataBand);

            AddShape(
                dataBand,
                "FondoFila",
                0,
                0,
                ancho,
                11,
                Color.FromArgb(255, 250, 245),
                Color.FromArgb(255, 250, 245));

            AddText(
                dataBand,
                "Pedido",
                0,
                1,
                24,
                8,
                "[Pedidos.Pedido]",
                texto,
                7.5f,
                FontStyle.Bold,
                HorzAlign.Center);

            AddText(
                dataBand,
                "Fecha",
                24,
                1,
                35,
                8,
                "[Pedidos.Fecha]",
                texto,
                7,
                FontStyle.Regular,
                HorzAlign.Center);

            AddText(
                dataBand,
                "Cliente",
                59,
                1,
                49,
                8,
                "[Pedidos.Cliente]",
                texto,
                7,
                FontStyle.Regular,
                HorzAlign.Left);

            var totalObject = AddText(
                dataBand,
                "Total",
                108,
                1,
                25,
                8,
                "[Pedidos.Total]",
                texto,
                7.5f,
                FontStyle.Bold,
                HorzAlign.Right);

            totalObject.Format =
                new FastReport.Format.CurrencyFormat();

            AddText(
                dataBand,
                "MetodoPago",
                133,
                1,
                41,
                8,
                "[Pedidos.MetodoPago]",
                texto,
                7,
                FontStyle.Regular,
                HorzAlign.Center);

            AddText(
                dataBand,
                "EstadoPago",
                174,
                1,
                38,
                8,
                "[Pedidos.EstadoPago]",
                texto,
                7,
                FontStyle.Bold,
                HorzAlign.Center);

            AddText(
                dataBand,
                "Entrega",
                212,
                1,
                30,
                8,
                "[Pedidos.Entrega]",
                texto,
                7,
                FontStyle.Regular,
                HorzAlign.Center);

            AddText(
                dataBand,
                "EstadoPedido",
                242,
                1,
                35,
                8,
                "[Pedidos.EstadoPedido]",
                texto,
                7,
                FontStyle.Bold,
                HorzAlign.Center);

            // =========================================================
            // PIE DEL REPORTE
            // =========================================================

            page.ReportSummary = new ReportSummaryBand
            {
                Name = "ResumenFinal",
                Height = 20 * Units.Millimeters
            };

            AddShape(
                page.ReportSummary,
                "FondoResumenFinal",
                0,
                2,
                ancho,
                17,
                cremaOscura,
                cremaOscura);

            AddText(
                page.ReportSummary,
                "ResumenFinalTitulo",
                7,
                5,
                60,
                5,
                "RESUMEN DEL REPORTE",
                cafeOscuro,
                8,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportSummary,
                "ResumenPedidos",
                7,
                11,
                65,
                5,
                $"Total de pedidos: {totalPedidos}",
                texto,
                7.5f,
                FontStyle.Regular,
                HorzAlign.Left);

            AddText(
                page.ReportSummary,
                "ResumenVentas",
                90,
                7,
                85,
                10,
                $"Ventas acumuladas: Bs {totalVentas:N2}",
                cafe,
                11,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportSummary,
                "ResumenPagados",
                185,
                7,
                45,
                10,
                $"Pagados: {pedidosPagados}",
                verde,
                9,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.ReportSummary,
                "ResumenPendientes",
                235,
                7,
                40,
                10,
                $"Pendientes: {pedidosPendientes}",
                naranja,
                9,
                FontStyle.Bold,
                HorzAlign.Left);

            // =========================================================
            // PIE DE PÁGINA
            // =========================================================

            page.PageFooter = new PageFooterBand
            {
                Name = "PiePagina",
                Height = 10 * Units.Millimeters
            };

            AddShape(
                page.PageFooter,
                "FondoPie",
                0,
                0,
                ancho,
                10,
                cafeOscuro,
                cafeOscuro);

            AddText(
                page.PageFooter,
                "MensajePie",
                5,
                2,
                120,
                6,
                "Gracias por ser parte de Dulce Antojo",
                blanco,
                8,
                FontStyle.Bold,
                HorzAlign.Left);

            AddText(
                page.PageFooter,
                "NumeroPagina",
                230,
                2,
                42,
                6,
                "Página [PageN] de [TotalPages#]",
                blanco,
                7,
                FontStyle.Regular,
                HorzAlign.Right);

            // =========================================================
            // REGISTRAR DATOS
            // =========================================================

            report.RegisterData(
                dataSet,
                "PedidosData",
                true);

            dataBand.DataSource =
                report.GetDataSource("Pedidos");

            // =========================================================
            // PREPARAR PDF
            // =========================================================

            report.Prepare();

            using var stream = new MemoryStream();

            var pdfExport = new PDFSimpleExport();

            report.Export(pdfExport, stream);

            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/pdf",
                "ReportePedidos.pdf");
        }

        // =============================================================
        // MÉTODOS AUXILIARES
        // =============================================================

        private static TextObject AddText(
            BandBase band,
            string name,
            float x,
            float y,
            float width,
            float height,
            string text,
            Color textColor,
            float fontSize,
            FontStyle fontStyle,
            HorzAlign align)
        {
            var obj = new TextObject
            {
                Name = name,

                Bounds = new RectangleF(
                    x * Units.Millimeters,
                    y * Units.Millimeters,
                    width * Units.Millimeters,
                    height * Units.Millimeters),

                Text = text,

                Font = new Font(
                    "Arial",
                    fontSize,
                    fontStyle),

                TextColor = textColor,
                HorzAlign = align,
                VertAlign = VertAlign.Center,
                Padding = new Padding(1, 1, 1, 1)
            };

            band.Objects.Add(obj);

            return obj;
        }

        private static ShapeObject AddShape(
            BandBase band,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color fillColor,
            Color borderColor)
        {
            var shape = new ShapeObject
            {
                Name = name,

                Bounds = new RectangleF(
                    x * Units.Millimeters,
                    y * Units.Millimeters,
                    width * Units.Millimeters,
                    height * Units.Millimeters),

                FillColor = fillColor
            };

            if (borderColor != Color.Transparent)
            {
                shape.Border.Lines = BorderLines.All;
                shape.Border.Color = borderColor;
                shape.Border.Width = 0.5f;
            }
            else
            {
                shape.Border.Lines = BorderLines.None;
            }

            band.Objects.Add(shape);

            return shape;
        }
    }
}