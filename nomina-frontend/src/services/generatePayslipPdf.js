
import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";

// Fuentes Unicode que deben existir en public/fonts
const FONT_FILES = [
    {
        file: "NotoSans-Regular.ttf",
        style: "normal"
    },
    {
        file: "NotoSans-Bold.ttf",
        style: "bold"
    }
];

// Cache para evitar descargar las fuentes repetidamente.
const fontCache = new Map();

async function fetchFontAsBase64(file) {
    if (fontCache.has(file)) {
        return fontCache.get(file);
    }

    const baseUrl = import.meta.env.BASE_URL || "/";
    const fontUrl = `${baseUrl}fonts/${file}`;

    const response = await fetch(fontUrl);

    if (!response.ok) {
        throw new Error(
            `No se pudo descargar la fuente ${file}. ` +
            `HTTP ${response.status}.`
        );
    }

    const contentType =
        response.headers.get("content-type") || "";

    const buffer = await response.arrayBuffer();
    const bytes = new Uint8Array(buffer);

    // Comprobar que Cloudflare no haya devuelto HTML.
    const firstBytes = new TextDecoder("utf-8")
        .decode(bytes.slice(0, 100))
        .trimStart()
        .toLowerCase();

    if (
        contentType.includes("text/html") ||
        firstBytes.startsWith("<!doctype html") ||
        firstBytes.startsWith("<html")
    ) {
        throw new Error(
            `La ruta ${fontUrl} devuelve HTML ` +
            "en lugar de una fuente TTF."
        );
    }

    // Verificar la cabecera de una fuente TrueType.
    const isTrueType =
        bytes.length >= 4 &&
        bytes[0] === 0x00 &&
        bytes[1] === 0x01 &&
        bytes[2] === 0x00 &&
        bytes[3] === 0x00;

    if (!isTrueType) {
        throw new Error(
            `${file} no es un archivo TTF válido.`
        );
    }

    let binary = "";
    const chunkSize = 0x8000;

    for (let i = 0; i < bytes.length; i += chunkSize) {
        const chunk = bytes.subarray(
            i,
            i + chunkSize
        );

        binary += String.fromCharCode(...chunk);
    }

    const base64 = btoa(binary);

    fontCache.set(file, base64);

    return base64;
}

async function loadUnicodeFonts(doc) {
    for (const font of FONT_FILES) {
        const base64 = await fetchFontAsBase64(
            font.file
        );

        doc.addFileToVFS(font.file, base64);

        doc.addFont(
            font.file,
            "NotoSans",
            font.style
        );
    }

    doc.setFont("NotoSans", "normal");
}

// Formato de moneda colombiana.
function formatCurrency(value) {
    const number = Number(value ?? 0);

    return new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP",
        maximumFractionDigits: 0
    }).format(number);
}

// Formato de fecha sin cambiar de zona horaria.
function formatDate(date) {
    if (!date) return "-";

    const value = String(date).substring(0, 10);
    const parts = value.split("-");

    if (parts.length !== 3) {
        return value;
    }

    return `${parts[2]}/${parts[1]}/${parts[0]}`;
}

// Verificar si una cadena ya contiene caracteres dañados.
function validateText(value, fieldName) {
    const text = String(value ?? "");

    if (text.includes("\uFFFD")) {
        console.warn(
            `NomiCorp: texto dañado en ${fieldName}:`,
            text
        );
    }

    return text;
}

export async function generatePayslipPdf(period, employee) {
    if (!period || !employee) {
        throw new Error(
            "No se encontraron los datos del desprendible."
        );
    }

    // Documento A4 horizontal para mayor espacio.
    const doc = new jsPDF({
        orientation: "landscape",
        unit: "mm",
        format: "a4",
        compress: true
    });

    await loadUnicodeFonts(doc);

    const monthNames = [
        "Enero",
        "Febrero",
        "Marzo",
        "Abril",
        "Mayo",
        "Junio",
        "Julio",
        "Agosto",
        "Septiembre",
        "Octubre",
        "Noviembre",
        "Diciembre"
    ];

    const periodName =
        `${monthNames[Number(period.month) - 1] || "-"} ` +
        `${period.year}`;

    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();

    const margin = 14;

    const baseStyles = {
        font: "NotoSans",
        fontStyle: "normal",
        fontSize: 8,
        cellPadding: 2.5,
        overflow: "linebreak",
        valign: "middle"
    };

    const headStyles = {
        font: "NotoSans",
        fontStyle: "bold",
        fillColor: [35, 55, 90],
        textColor: [255, 255, 255]
    };

    // =====================================
    // ENCABEZADO
    // =====================================

    doc.setFont("NotoSans", "bold");
    doc.setFontSize(18);
    doc.text("NomiCorp", margin, 20);

    doc.setFontSize(14);
    doc.text(
        "DESPRENDIBLE DE NÓMINA",
        pageWidth - margin,
        20,
        { align: "right" }
    );

    doc.setFont("NotoSans", "normal");
    doc.setFontSize(10);

    doc.text(
        `Período: ${periodName}`,
        margin,
        30
    );

    doc.text(
        `Fecha de inicio: ${formatDate(period.startDate)}`,
        margin,
        36
    );

    doc.text(
        `Fecha de finalización: ${formatDate(period.endDate)}`,
        margin,
        42
    );

    // =====================================
    // INFORMACIÓN DEL EMPLEADO
    // =====================================

    doc.setFont("NotoSans", "bold");
    doc.setFontSize(12);

    doc.text(
        "Información del empleado",
        margin,
        54
    );

    autoTable(doc, {
        startY: 60,
        margin: {
            left: margin,
            right: margin,
            bottom: 18
        },
        theme: "grid",
        head: [["Dato", "Información"]],
        body: [
            [
                "Nombre",
                validateText(
                    employee.employeeName,
                    "Nombre"
                )
            ],
            [
                "Identificación",
                String(employee.identification ?? "-")
            ],
            [
                "Cargo",
                validateText(
                    employee.position || "-",
                    "Cargo"
                )
            ],
            [
                "Salario mensual",
                formatCurrency(employee.monthlySalary)
            ]
        ],
        styles: {
            ...baseStyles,
            fontSize: 9,
            cellPadding: 3
        },
        headStyles,
        columnStyles: {
            0: { cellWidth: 65 }
        }
    });

    // =====================================
    // CONCEPTOS DE NÓMINA
    // =====================================

    const concepts = employee.concepts || [];

    const conceptsStartY =
        doc.lastAutoTable.finalY + 10;

    doc.setFont("NotoSans", "bold");
    doc.setFontSize(12);

    doc.text(
        "Conceptos de liquidación",
        margin,
        conceptsStartY
    );

    const conceptRows = concepts.map((concept) => [
        String(concept.code ?? "-"),

        validateText(
            concept.description || "-",
            `Concepto ${concept.code}`
        ),

        validateText(
            concept.category || "-",
            "Categoría"
        ),

        concept.quantity == null
            ? "-"
            : String(concept.quantity),

        concept.unitValue == null
            ? "-"
            : formatCurrency(concept.unitValue),

        formatCurrency(concept.amount)
    ]);

    autoTable(doc, {
        startY: conceptsStartY + 5,

        margin: {
            left: margin,
            right: margin,
            bottom: 18
        },

        theme: "striped",

        head: [[
            "Código",
            "Descripción",
            "Categoría",
            "Cantidad",
            "Valor unitario",
            "Total"
        ]],

        body: conceptRows,

        styles: baseStyles,

        headStyles,

        columnStyles: {
            0: {
                cellWidth: 29
            },

            1: {
                cellWidth: 94
            },

            2: {
                cellWidth: 43
            },

            3: {
                cellWidth: 25,
                halign: "center"
            },

            4: {
                cellWidth: 37,
                halign: "right"
            },

            5: {
                cellWidth: "auto",
                halign: "right"
            }
        },

        showHead: "everyPage"
    });

    // =====================================
    // RESUMEN DE LIQUIDACIÓN
    // =====================================

    const summaryStartY =
        doc.lastAutoTable.finalY + 10;

    doc.setFont("NotoSans", "bold");
    doc.setFontSize(12);

    doc.text(
        "Resumen de liquidación",
        margin,
        summaryStartY
    );

    autoTable(doc, {
        startY: summaryStartY + 5,

        margin: {
            left: margin,
            right: margin,
            bottom: 18
        },

        theme: "grid",

        head: [[
            "Concepto",
            "Valor"
        ]],

        body: [
            [
                "Total devengado",
                formatCurrency(employee.totalEarnings)
            ],
            [
                "Total deducciones",
                formatCurrency(employee.totalDeductions)
            ],
            [
                "NETO A PAGAR",
                formatCurrency(employee.netPay)
            ]
        ],

        styles: {
            ...baseStyles,
            fontSize: 10,
            cellPadding: 3
        },

        headStyles,

        columnStyles: {
            0: {
                cellWidth: 140
            },
            1: {
                halign: "right"
            }
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

    // =====================================
    // PIE DE PÁGINA
    // =====================================

    const totalPages = doc.getNumberOfPages();

    for (let page = 1; page <= totalPages; page++) {
        doc.setPage(page);

        doc.setFont("NotoSans", "normal");
        doc.setFontSize(8);
        doc.setTextColor(110, 110, 110);

        doc.text(
            "Documento generado por NomiCorp.",
            margin,
            pageHeight - 10
        );

        doc.text(
            `Página ${page} de ${totalPages}`,
            pageWidth - margin,
            pageHeight - 10,
            { align: "right" }
        );
    }

    // =====================================
    // GUARDAR PDF
    // =====================================

    const monthNumber = String(period.month)
        .padStart(2, "0");

    const identification = String(
        employee.identification ?? "empleado"
    ).replace(/[^a-zA-Z0-9_-]/g, "_");

    const fileName =
        `Desprendible_${identification}_` +
        `${period.year}_${monthNumber}.pdf`;

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