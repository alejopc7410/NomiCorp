import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";

const FONT_FILES = [
    { file: "NotoSans-Regular.ttf", style: "normal" },
    { file: "NotoSans-Regular.ttf", style: "bold" },
    { file: "NotoSans-Italic.ttf", style: "italic" }
];

// Cache para no descargar las fuentes en cada PDF
const fontCache = {};

async function fetchFontAsBase64(file) {
    if (fontCache[file]) return fontCache[file];

    const response = await fetch(`/fonts/${file}`);

    if (!response.ok) {
        throw new Error(`No se pudo cargar la fuente ${file}.`);
    }

    const bytes = new Uint8Array(await response.arrayBuffer());

    let binary = "";
    const chunkSize = 0x8000;

    for (let i = 0; i < bytes.length; i += chunkSize) {
        binary += String.fromCharCode(...bytes.subarray(i, i + chunkSize));
    }

    fontCache[file] = btoa(binary);
    return fontCache[file];
}

async function loadUnicodeFont(doc) {
    for (const { file, style } of FONT_FILES) {
        const base64Font = await fetchFontAsBase64(file);
        doc.addFileToVFS(file, base64Font);
        doc.addFont(file, "NotoSans", style);
    }

    doc.setFont("NotoSans", "normal");
}

function formatCurrency(value) {
    return new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP",
        maximumFractionDigits: 0
    }).format(Number(value) || 0);
}

function formatDate(date) {
    if (!date) return "-";
    return new Date(`${date}T00:00:00`).toLocaleDateString("es-CO");
}

export async function generatePayslipPdf(period, employee) {
    const doc = new jsPDF();
    await loadUnicodeFont(doc);

    const monthNames = [
        "Enero", "Febrero", "Marzo", "Abril",
        "Mayo", "Junio", "Julio", "Agosto",
        "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    const periodName = `${monthNames[period.month - 1]} ${period.year}`;
    const pageWidth = doc.internal.pageSize.getWidth();

    // Estilos compartidos para todas las tablas
    const baseStyles = {
        font: "NotoSans",
        fontStyle: "normal"
    };

    const baseHeadStyles = {
        font: "NotoSans",
        fontStyle: "bold",
        fillColor: [35, 55, 90]
    };

    // Encabezado
    doc.setFontSize(18);
    doc.setFont("NotoSans", "bold");
    doc.text("NomiCorp", 14, 20);

    doc.setFontSize(14);
    doc.text("DESPRENDIBLE DE NÓMINA", pageWidth - 14, 20, { align: "right" });

    doc.setFontSize(10);
    doc.setFont("NotoSans", "normal");
    doc.text(`Periodo: ${periodName}`, 14, 30);
    doc.text(`Fecha de inicio: ${formatDate(period.startDate)}`, 14, 36);
    doc.text(`Fecha de finalización: ${formatDate(period.endDate)}`, 14, 42);

    // Datos del empleado
    doc.setFontSize(12);
    doc.setFont("NotoSans", "bold");
    doc.text("Información del empleado", 14, 55);

    autoTable(doc, {
        startY: 60,
        theme: "grid",
        head: [["Dato", "Información"]],
        body: [
            ["Nombre", employee.employeeName],
            ["Identificación", employee.identification],
            ["Cargo", employee.position || "-"],
            ["Salario mensual", formatCurrency(employee.monthlySalary)]
        ],
        styles: { ...baseStyles, fontSize: 9, cellPadding: 3 },
        headStyles: baseHeadStyles
    });

    // Conceptos liquidados
    const concepts = employee.concepts || [];
    const conceptsStartY = doc.lastAutoTable.finalY + 12;

    doc.setFontSize(12);
    doc.setFont("NotoSans", "bold");
    doc.text("Conceptos de liquidación", 14, conceptsStartY);

    autoTable(doc, {
        startY: conceptsStartY + 5,
        theme: "striped",
        head: [[
            "Código",
            "Descripción",
            "Categoría",
            "Cantidad",
            "Valor unitario",
            "Total"
        ]],
        body: concepts.map(concept => [
            concept.code || "-",
            concept.description || "-",
            concept.category || "-",
            concept.quantity ?? "-",
            concept.unitValue == null ? "-" : formatCurrency(concept.unitValue),
            formatCurrency(concept.amount)
        ]),
        styles: { ...baseStyles, fontSize: 8, cellPadding: 2 },
        headStyles: baseHeadStyles,
        columnStyles: {
            1: { cellWidth: 48 },
            5: { halign: "right" }
        }
    });

    // Resumen de liquidación
    const summaryStartY = doc.lastAutoTable.finalY + 12;

    autoTable(doc, {
        startY: summaryStartY,
        theme: "grid",
        head: [["Resumen", "Valor"]],
        body: [
            ["Total devengado", formatCurrency(employee.totalEarnings)],
            ["Total deducciones", formatCurrency(employee.totalDeductions)],
            ["NETO A PAGAR", formatCurrency(employee.netPay)]
        ],
        styles: { ...baseStyles, fontSize: 10, cellPadding: 4 },
        headStyles: baseHeadStyles,
        didParseCell(data) {
            if (data.section === "body" && data.row.index === 2) {
                data.cell.styles.fontStyle = "bold";
            }
        }
    });

    // Pie de página
    const pageHeight = doc.internal.pageSize.getHeight();

    doc.setFontSize(8);
    doc.setFont("NotoSans", "italic");
    doc.text(
        "Documento generado por el sistema NomiCorp.",
        14,
        pageHeight - 12
    );

    const monthNumber = String(period.month).padStart(2, "0");
    const fileName =
        `Desprendible_${employee.identification}_${period.year}_${monthNumber}.pdf`;

    doc.save(fileName);
}

/*import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";

async function loadUnicodeFont(doc) {
    const response = await fetch("/fonts/NotoSans-Regular.ttf");

    if (!response.ok) {
        throw new Error("No se pudo cargar la fuente Noto Sans.");
    }

    const fontBuffer = await response.arrayBuffer();

    const bytes = new Uint8Array(fontBuffer);

    let binary = "";

    const chunkSize = 0x8000;

    for (let i = 0; i < bytes.length; i += chunkSize) {
        const chunk = bytes.subarray(i, i + chunkSize);
        binary += String.fromCharCode(...chunk);
    }

    const base64Font = btoa(binary);

    doc.addFileToVFS("NotoSans-Regular.ttf", base64Font);

    doc.addFont(
        "NotoSans-Regular.ttf",
        "NotoSans",
        "normal"
    );

    doc.setFont("NotoSans", "normal");
}

function formatCurrency(value) {
    return new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP",
        maximumFractionDigits: 0
    }).format(Number(value) || 0);
}

function formatDate(date) {
    if (!date) return "-";

    return new Date(`${date}T00:00:00`).toLocaleDateString("es-CO");
}

export async function generatePayslipPdf(period, employee) {
    const doc = new jsPDF();
    await loadUnicodeFont(doc);

    const monthNames = [
        "Enero", "Febrero", "Marzo", "Abril",
        "Mayo", "Junio", "Julio", "Agosto",
        "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    const periodName =
        `${monthNames[period.month - 1]} ${period.year}`;

    const pageWidth = doc.internal.pageSize.getWidth();

    // Encabezado
    doc.setFontSize(18);
    doc.setFont("NotoSans", "bold");
    doc.text("NomiCorp", 14, 20);

    doc.setFontSize(14);
    doc.text("DESPRENDIBLE DE NÓMINA", pageWidth - 14, 20, {
        align: "right"
    });

    doc.setFontSize(10);
    doc.setFont("NotoSans", "bold");
    doc.text(`Periodo: ${periodName}`, 14, 30);
    doc.text(
        `Fecha de inicio: ${formatDate(period.startDate)}`,
        14,
        36
    );
    doc.text(
        `Fecha de finalización: ${formatDate(period.endDate)}`,
        14,
        42
    );

    // Datos del empleado
    doc.setFontSize(12);
    doc.setFont("NotoSans", "bold");
    doc.text("Información del empleado", 14, 55);

    autoTable(doc, {
        startY: 60,
        theme: "grid",
        head: [["Dato", "Información"]],
        body: [
            ["Nombre", employee.employeeName],
            ["Identificación", employee.identification],
            ["Cargo", employee.position || "-"],
            ["Salario mensual", formatCurrency(employee.monthlySalary)]
        ],
        styles: {
            fontSize: 9,
            cellPadding: 3,
            font: "NotoSans",
            fontStyle: "normal"
        },
        headStyles: {
            fillColor: [35, 55, 90]
        }
    });

    // Conceptos liquidados
    const concepts = employee.concepts || [];*/

    /*doc.setFontSize(12);
    doc.setFont("NotoSans", "bold");

    doc.setFont("NotoSans", "normal");
    doc.setFontSize(12);

    doc.text(
        "Prueba de caracteres en español:",
        20,
        30
    );

    doc.text(
        "Liquidación, nómina, pensión, deducción,",
        20,
        40
    );

    doc.text(
        "salario, empleado, año, días y finalización.",
        20,
        50
    );

    doc.save("prueba-unicode.pdf");
    */

    /*const conceptsStartY = doc.lastAutoTable.finalY + 12;

    doc.text("Conceptos de liquidación", 14, conceptsStartY);

    autoTable(doc, {
        startY: conceptsStartY + 5,
        theme: "striped",
        head: [[
            "Código",
            "Descripción",
            "Categoría",
            "Cantidad",
            "Valor unitario",
            "Total"
        ]],
        body: concepts.map(concept => [
            concept.code || "-",
            concept.description || "-",
            concept.category || "-",
            concept.quantity ?? "-",
            concept.unitValue == null
                ? "-"
                : formatCurrency(concept.unitValue),
            formatCurrency(concept.amount)
        ]),
        styles: {
            fontSize: 8,
            cellPadding: 2
        },
        headStyles: {
            fillColor: [35, 55, 90]
        },
        columnStyles: {
            1: { cellWidth: 48 },
            5: { halign: "right" }
        }
    });

    // Resumen de liquidación
    const summaryStartY = doc.lastAutoTable.finalY + 12;

    autoTable(doc, {
        startY: summaryStartY,
        theme: "grid",
        head: [["Resumen", "Valor"]],
        body: [
            ["Total devengado", formatCurrency(employee.totalEarnings)],
            ["Total deducciones", formatCurrency(employee.totalDeductions)],
            ["NETO A PAGAR", formatCurrency(employee.netPay)]
        ],
        styles: {
            fontSize: 10,
            cellPadding: 4
        },
        headStyles: {
            fillColor: [35, 55, 90]
        },
        bodyStyles: {
            fontStyle: "normal"
        },
        didParseCell(data) {
            if (
                data.section === "body" &&
                data.row.index === 2
            ) {
                data.cell.styles.fontStyle = "bold";
            }
        }
    });

    // Pie de página
    const pageHeight = doc.internal.pageSize.getHeight();

    doc.setFontSize(8);
    doc.setFont("NotoSans", "italic");
    doc.text(
        "Documento generado por el sistema NominaEmpresa.",
        14,
        pageHeight - 12
    );

    const monthNumber = String(period.month).padStart(2, "0");

    const fileName =
        `Desprendible_${employee.identification}_${period.year}_${monthNumber}.pdf`;

    doc.save(fileName);
}*/