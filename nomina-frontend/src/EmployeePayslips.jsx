
import { useEffect, useState } from "react";
import { payslipsApi } from "./services/payslips";
import jsPDF from "jspdf";
import autoTable from "jspdf-autotable";

const currency = new Intl.NumberFormat("es-CO", {
    style: "currency",
    currency: "COP",
    maximumFractionDigits: 0,
});

const monthNames = [
    "Enero", "Febrero", "Marzo", "Abril",
    "Mayo", "Junio", "Julio", "Agosto",
    "Septiembre", "Octubre", "Noviembre", "Diciembre",
];

function EmployeePayslips() {
    const [payslips, setPayslips] = useState([]);
    const [selectedPayslip, setSelectedPayslip] = useState(null);

    const [loading, setLoading] = useState(true);
    const [detailLoading, setDetailLoading] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        loadPayslips();
    }, []);

    async function loadPayslips() {
        try {
            setLoading(true);
            setError("");

            const data = await payslipsApi.getAll();
            setPayslips(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    async function showPayslip(id) {
        try {
            setDetailLoading(true);
            setError("");

            const data = await payslipsApi.getById(id);
            setSelectedPayslip(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setDetailLoading(false);
        }
    }

    function closePayslip() {
        setSelectedPayslip(null);
    }

    // Cargar una fuente Unicode desde public/fonts.
    async function loadPdfFont(doc, fileName, style) {
        const url = `/fonts/${fileName}`;
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(
                `No se pudo cargar la fuente PDF: ${url}`
            );
        }

        const bytes = new Uint8Array(
            await response.arrayBuffer()
        );

        if (bytes.length < 1000) {
            throw new Error(
                `La fuente PDF está vacía o no es válida: ${url}`
            );
        }

        // Convertir los bytes a base64 por bloques.
        let binary = "";

        for (let i = 0; i < bytes.length; i += 8192) {
            binary += String.fromCharCode(
                ...bytes.subarray(i, i + 8192)
            );
        }

        doc.addFileToVFS(fileName, btoa(binary));
        doc.addFont(fileName, "NotoSans", style);
    }

    async function downloadPayslip() {
        if (!selectedPayslip) return;

        try {
            const doc = new jsPDF({
                orientation: "landscape",
                unit: "mm",
                format: "a4",
            });

            // Registrar las fuentes Unicode.
            await loadPdfFont(
                doc,
                "NotoSans-Regular.ttf",
                "normal"
            );

            await loadPdfFont(
                doc,
                "NotoSans-Bold.ttf",
                "bold"
            );

            const payslip = selectedPayslip;

            const month =
                monthNames[payslip.periodMonth - 1];

            const margins = {
                left: 14,
                right: 14,
                bottom: 18,
            };

            // ENCABEZADO DEL PDF
            doc.setFont("NotoSans", "bold");
            doc.setFontSize(17);

            doc.text(
                "COMPROBANTE DE NÓMINA",
                148.5,
                18,
                { align: "center" }
            );

            doc.setFont("NotoSans", "normal");
            doc.setFontSize(10);

            doc.text(
                `Período: ${month} ${payslip.periodYear}`,
                148.5,
                26,
                { align: "center" }
            );

            // INFORMACIÓN DEL EMPLEADO
            doc.setFont("NotoSans", "bold");
            doc.setFontSize(11);

            doc.text(
                "INFORMACIÓN DEL EMPLEADO",
                14,
                38
            );

            autoTable(doc, {
                startY: 43,
                theme: "grid",

                head: [
                    ["Campo", "Información"]
                ],

                body: [
                    [
                        "Nombre",
                        payslip.employeeName ?? ""
                    ],
                    [
                        "Identificación",
                        payslip.identification ?? ""
                    ],
                    [
                        "Cargo",
                        payslip.position ?? ""
                    ],
                    [
                        "Salario mensual",
                        currency.format(
                            payslip.monthlySalary
                        )
                    ],
                    [
                        "Período",
                        `${payslip.startDate} al ${payslip.endDate}`
                    ],
                ],

                styles: {
                    font: "NotoSans",
                    fontSize: 8,
                    cellPadding: 2,
                    overflow: "linebreak",
                },

                headStyles: {
                    font: "NotoSans",
                    fontStyle: "bold",
                    fillColor: [41, 128, 185],
                },

                margin: margins,
            });

            // CONCEPTOS DE NÓMINA
            const conceptRows = payslip.concepts.map(
                (concept) => [
                    concept.code,
                    concept.description,
                    concept.category,
                    concept.quantity ?? "-",

                    concept.unitValue != null
                        ? currency.format(
                            concept.unitValue
                        )
                        : "-",

                    currency.format(concept.amount),
                ]
            );

            const conceptsStartY =
                doc.lastAutoTable.finalY + 9;

            doc.setFont("NotoSans", "bold");
            doc.setFontSize(11);

            doc.text(
                "DETALLE DE CONCEPTOS",
                14,
                conceptsStartY
            );

            autoTable(doc, {
                startY: conceptsStartY + 4,
                theme: "striped",

                head: [[
                    "Código",
                    "Concepto",
                    "Categoría",
                    "Cantidad",
                    "Valor unitario",
                    "Total",
                ]],

                body: conceptRows,

                styles: {
                    font: "NotoSans",
                    fontSize: 7.5,
                    cellPadding: 2.5,
                    overflow: "linebreak",
                    valign: "middle",
                },

                headStyles: {
                    font: "NotoSans",
                    fontStyle: "bold",
                    fillColor: [41, 128, 185],
                },

                columnStyles: {
                    0: {
                        cellWidth: 26
                    },

                    1: {
                        cellWidth: 93
                    },

                    2: {
                        cellWidth: 34
                    },

                    3: {
                        cellWidth: 23,
                        halign: "center"
                    },

                    4: {
                        cellWidth: 45,
                        halign: "right"
                    },

                    5: {
                        cellWidth: 46,
                        halign: "right"
                    },
                },

                margin: margins,
            });

            // RESUMEN DE LA LIQUIDACIÓN
            const summaryY =
                doc.lastAutoTable.finalY + 9;

            autoTable(doc, {
                startY: summaryY,
                theme: "grid",

                body: [
                    [
                        "Total devengado",
                        currency.format(
                            payslip.totalEarnings
                        )
                    ],

                    [
                        "Total deducciones",
                        currency.format(
                            payslip.totalDeductions
                        )
                    ],

                    [
                        "NETO A PAGAR",
                        currency.format(
                            payslip.netPay
                        )
                    ],
                ],

                styles: {
                    font: "NotoSans",
                    fontSize: 9,
                    cellPadding: 3,
                },

                columnStyles: {
                    0: {
                        fontStyle: "bold"
                    },

                    1: {
                        halign: "right"
                    },
                },

                didParseCell: ({ row, cell }) => {
                    if (row.index === 2) {
                        cell.styles.fontStyle = "bold";
                    }
                },

                margin: margins,
            });

            // PIE DE PÁGINA
            const totalPages =
                doc.internal.getNumberOfPages();

            for (
                let page = 1;
                page <= totalPages;
                page++
            ) {
                doc.setPage(page);

                doc.setFont("NotoSans", "normal");
                doc.setFontSize(8);

                doc.text(
                    `Documento generado por NomiCorp · Página ${page} de ${totalPages}`,
                    148.5,
                    doc.internal.pageSize.height - 8,
                    { align: "center" }
                );
            }

            // DESCARGA
            doc.save(
                `Desprendible_${payslip.identification}_${payslip.periodYear}_${payslip.periodMonth}.pdf`
            );

        } catch (err) {
            setError(
                err.message ||
                "No fue posible generar el PDF."
            );
        }
    }

    if (loading) {
        return (
            <div className="text-center py-4">
                <div className="spinner-border text-primary" />
                <p className="mt-2">
                    Cargando desprendibles...
                </p>
            </div>
        );
    }

    return (
        <div className="mt-4">

            {/* LISTA DE DESPRENDIBLES */}
            <div className="card border-0 shadow-sm">
                <div className="card-body">

                    <h4 className="mb-3">
                        Mis desprendibles
                    </h4>

                    <p className="text-muted">
                        Consulta los comprobantes de pago
                        de tus períodos de nómina.
                    </p>

                    {error && (
                        <div className="alert alert-danger">
                            {error}
                        </div>
                    )}

                    {payslips.length === 0 ? (
                        <div className="alert alert-info mb-0">
                            Todavía no tienes desprendibles
                            disponibles.
                        </div>
                    ) : (
                        <div className="table-responsive">
                            <table className="table table-hover align-middle">

                                <thead>
                                    <tr>
                                        <th>Período</th>
                                        <th>Devengado</th>
                                        <th>Deducciones</th>
                                        <th>Neto a pagar</th>
                                        <th>Acción</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {payslips.map((item) => (
                                        <tr key={item.payrollDetailId}>

                                            <td>
                                                {monthNames[item.periodMonth - 1]}{" "}
                                                {item.periodYear}
                                            </td>

                                            <td>
                                                {currency.format(
                                                    item.totalEarnings
                                                )}
                                            </td>

                                            <td>
                                                {currency.format(
                                                    item.totalDeductions
                                                )}
                                            </td>

                                            <td className="fw-bold">
                                                {currency.format(
                                                    item.netPay
                                                )}
                                            </td>

                                            <td>
                                                <button
                                                    className="btn btn-sm btn-outline-primary"
                                                    onClick={() =>
                                                        showPayslip(
                                                            item.payrollDetailId
                                                        )
                                                    }
                                                    disabled={detailLoading}
                                                >
                                                    Ver detalle
                                                </button>
                                            </td>

                                        </tr>
                                    ))}
                                </tbody>

                            </table>
                        </div>
                    )}

                </div>
            </div>

            {/* CARGANDO DETALLE */}
            {detailLoading && (
                <div className="text-center py-3">
                    <div className="spinner-border spinner-border-sm text-primary" />

                    <span className="ms-2">
                        Consultando desprendible...
                    </span>
                </div>
            )}

            {/* DETALLE DEL DESPRENDIBLE */}
            {selectedPayslip && (
                <div className="card border-0 shadow-sm mt-4">
                    <div className="card-body">

                        <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">

                            <h4 className="mb-0">
                                Detalle del desprendible
                            </h4>

                            <div className="d-flex gap-2">

                                <button
                                    className="btn btn-success btn-sm"
                                    onClick={downloadPayslip}
                                >
                                    <i className="bi bi-file-earmark-pdf me-1"></i>
                                    Descargar PDF
                                </button>

                                <button
                                    className="btn btn-outline-secondary btn-sm"
                                    onClick={closePayslip}
                                >
                                    Cerrar
                                </button>

                            </div>
                        </div>

                        <h5 className="text-primary">
                            {monthNames[selectedPayslip.periodMonth - 1]}{" "}
                            {selectedPayslip.periodYear}
                        </h5>

                        <hr />

                        {/* DATOS DEL EMPLEADO */}
                        <div className="row g-3 mb-4">

                            <div className="col-md-6">
                                <strong>Empleado:</strong>
                                <p>
                                    {selectedPayslip.employeeName}
                                </p>
                            </div>

                            <div className="col-md-6">
                                <strong>Identificación:</strong>
                                <p>
                                    {selectedPayslip.identification}
                                </p>
                            </div>

                            <div className="col-md-6">
                                <strong>Cargo:</strong>
                                <p>
                                    {selectedPayslip.position}
                                </p>
                            </div>

                            <div className="col-md-6">
                                <strong>Salario mensual:</strong>
                                <p>
                                    {currency.format(
                                        selectedPayslip.monthlySalary
                                    )}
                                </p>
                            </div>

                        </div>

                        <h6>
                            Conceptos de nómina
                        </h6>

                        {/* TABLA DE CONCEPTOS */}
                        <div className="table-responsive">
                            <table className="table table-bordered">

                                <thead>
                                    <tr>
                                        <th>Concepto</th>
                                        <th>Categoría</th>
                                        <th>Cantidad</th>
                                        <th>Valor unitario</th>
                                        <th>Valor</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {selectedPayslip.concepts.map(
                                        (concept) => (
                                            <tr key={concept.id}>

                                                <td>
                                                    {concept.description}
                                                </td>

                                                <td>
                                                    {concept.category}
                                                </td>

                                                <td>
                                                    {concept.quantity ?? "-"}
                                                </td>

                                                <td>
                                                    {concept.unitValue != null
                                                        ? currency.format(
                                                            concept.unitValue
                                                        )
                                                        : "-"}
                                                </td>

                                                <td>
                                                    {currency.format(
                                                        concept.amount
                                                    )}
                                                </td>

                                            </tr>
                                        )
                                    )}
                                </tbody>

                            </table>
                        </div>

                        {/* TOTALES */}
                        <div className="row justify-content-end">
                            <div className="col-md-5">

                                <div className="d-flex justify-content-between mb-2">
                                    <span>
                                        Total devengado:
                                    </span>

                                    <strong>
                                        {currency.format(
                                            selectedPayslip.totalEarnings
                                        )}
                                    </strong>
                                </div>

                                <div className="d-flex justify-content-between mb-2">
                                    <span>
                                        Total deducciones:
                                    </span>

                                    <strong>
                                        {currency.format(
                                            selectedPayslip.totalDeductions
                                        )}
                                    </strong>
                                </div>

                                <hr />

                                <div className="d-flex justify-content-between">

                                    <span className="fw-bold">
                                        NETO A PAGAR:
                                    </span>

                                    <strong className="text-success fs-5">
                                        {currency.format(
                                            selectedPayslip.netPay
                                        )}
                                    </strong>

                                </div>

                            </div>
                        </div>

                    </div>
                </div>
            )}

        </div>
    );
}

export default EmployeePayslips;
