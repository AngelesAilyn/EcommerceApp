using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
//using System.ComponentModel;

namespace EcommerceApp.Reports;

public sealed record OrdersPdfRow(
    string Pedido,
    string Fecha,
    string Cliente,
    decimal Total,
    string MetodoPago,
    string EstadoPago,
    string Entrega,
    string EstadoPedido);

public sealed class OrdersPdfDocument(IReadOnlyList<OrdersPdfRow> rows, DateTime generatedAt) : IDocument
{
    private const string Cocoa = "#5B3B2D";
    private const string CocoaDark = "#3F291F";
    private const string Cream = "#F6EFE5";
    private const string Paper = "#FFFDF9";
    private const string Line = "#E6D8C8";
    private const string Muted = "#806F61";
    private const string Latte = "#EADBC9";
    private const string Green = "#65785B";

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.PageColor(Cream);
            page.DefaultTextStyle(x => x.FontSize(8).FontColor(CocoaDark));

            page.Header().Column(column =>
            {
                column.Item().Background(Paper).Border(1).BorderColor(Line).Padding(16).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text("DULCE ANTOJO").FontSize(20).Bold().FontColor(CocoaDark);
                        left.Item().PaddingTop(3).Text("REPORTE DE PEDIDOS Y VENTAS").FontSize(11).Bold().FontColor(Cocoa);
                        left.Item().PaddingTop(3).Text("Resumen administrativo de las compras registradas en el sistema.").FontSize(8).FontColor(Muted);
                    });
                    row.ConstantItem(160).Background(Latte).Padding(10).Column(right =>
                    {
                        right.Item().Text("GENERADO").FontSize(7).Bold().FontColor(Cocoa);
                        right.Item().PaddingTop(3).Text(generatedAt.ToString("dd/MM/yyyy HH:mm")).FontSize(10).Bold().FontColor(CocoaDark);
                        right.Item().PaddingTop(3).Text("Todos los pedidos").FontSize(7).FontColor(Muted);
                    });
                });

                column.Item().PaddingTop(12).Row(row =>
                {
                    SummaryCard(row.RelativeItem(), "PEDIDOS", rows.Count.ToString(), Cocoa);
                    row.ConstantItem(10);
                    SummaryCard(row.RelativeItem(), "VENTAS", $"Bs {rows.Sum(x => x.Total):N2}", Cocoa);
                    row.ConstantItem(10);
                    SummaryCard(row.RelativeItem(), "PAGADOS", rows.Count(x => x.EstadoPago.Equals("Pagado", StringComparison.OrdinalIgnoreCase)).ToString(), Green);
                    row.ConstantItem(10);
                    SummaryCard(row.RelativeItem(), "PENDIENTES", rows.Count(x => x.EstadoPago.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)).ToString(), "#8B6750");
                });
            });

            page.Content().PaddingTop(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(72);
                    columns.RelativeColumn(1.35f);
                    columns.ConstantColumn(70);
                    columns.RelativeColumn(1.05f);
                    columns.RelativeColumn(1.0f);
                    columns.RelativeColumn(1.0f);
                    columns.RelativeColumn(1.0f);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "PEDIDO");
                    HeaderCell(header.Cell(), "FECHA");
                    HeaderCell(header.Cell(), "CLIENTE");
                    HeaderCell(header.Cell(), "TOTAL");
                    HeaderCell(header.Cell(), "PAGO");
                    HeaderCell(header.Cell(), "ESTADO PAGO");
                    HeaderCell(header.Cell(), "ENTREGA");
                    HeaderCell(header.Cell(), "ESTADO");
                });

                foreach (var row in rows)
                {
                    BodyCell(table.Cell(), row.Pedido, true);
                    BodyCell(table.Cell(), row.Fecha);
                    BodyCell(table.Cell(), row.Cliente);
                    BodyCell(table.Cell(), $"Bs {row.Total:N2}", true);
                    BodyCell(table.Cell(), row.MetodoPago);
                    BodyCell(table.Cell(), row.EstadoPago, true, row.EstadoPago.Equals("Pagado", StringComparison.OrdinalIgnoreCase) ? Green : "#8B6750");
                    BodyCell(table.Cell(), row.Entrega);
                    BodyCell(table.Cell(), row.EstadoPedido);
                }
            });

            page.Footer().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text("Dulce Antojo · Repostería artesanal").FontSize(7).FontColor(Muted);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Página ").FontSize(7).FontColor(Muted);
                    text.CurrentPageNumber().FontSize(7).FontColor(Muted);
                    text.Span(" de ").FontSize(7).FontColor(Muted);
                    text.TotalPages().FontSize(7).FontColor(Muted);
                });
            });
        });
    }

    private static void SummaryCard(IContainer container, string label, string value, string accent)
    {
        container.Background(Paper).Border(1).BorderColor(Line).Padding(10).Column(column =>
        {
            column.Item().Text(label).FontSize(7).Bold().FontColor(Muted);
            column.Item().PaddingTop(4).Text(value).FontSize(15).Bold().FontColor(accent);
        });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container.Background(Cocoa).Padding(7).Text(text).FontSize(7).Bold().FontColor(Colors.White);
    }

    private static void BodyCell(IContainer container, string text, bool bold = false, string? color = null)
    {
        var cell = container.Background(Paper).BorderBottom(1).BorderColor(Line).Padding(6);
        var textBlock = cell.Text(text).FontSize(7).FontColor(color ?? CocoaDark);
        if (bold)
            textBlock.Bold();
    }
}
