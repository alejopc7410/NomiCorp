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

    function downloadPayslip() {
        if (!selectedPayslip) return;

        const doc = new jsPDF();

        const payslip = selectedPayslip;

        const month = monthNames[payslip.periodMonth - 1];

        // Encabezado
        doc.setFontSize(18);
        doc.setFont("helvetica", "bold");
        doc.text("COMPROBANTE DE NOMINA", 105, 20, {
            align: "center",
        });

        doc.setFontSize(11);
        doc.setFont("helvetica", "normal");
        doc.text(
            `Periodo: ${month} ${payslip.periodYear}`,
            105,
            29,
            { align: "center" }
        );

        // Información del empleado
        doc.setFontSize(12);
        doc.setFont("helvetica", "bold");
        doc.text("INFORMACION DEL EMPLEADO", 14, 43);

        autoTable(doc, {
            startY: 48,
            theme: "grid",
            head: [["Campo", "Informacion"]],
            body: [
                ["Nombre", payslip.employeeName],
                ["Identificacion", payslip.identification],
                ["Cargo", payslip.position],
                ["Salario mensual", currency.format(payslip.monthlySalary)],
                ["Periodo", `${payslip.startDate} al ${payslip.endDate}`],
            ],
            styles: {
                fontSize: 9,
                cellPadding: 3,
            },
        });

        // Conceptos
        const conceptRows = payslip.concepts.map((concept) => [
            concept.code,
            concept.description,
            concept.category,
            concept.quantity ?? "-",
            concept.unitValue != null
                ? currency.format(concept.unitValue)
                : "-",
            currency.format(concept.amount),
        ]);

        const conceptsStartY = doc.lastAutoTable.finalY + 10;

        doc.setFontSize(12);
        doc.setFont("helvetica", "bold");
        doc.text("DETALLE DE CONCEPTOS", 14, conceptsStartY);

        autoTable(doc, {
            startY: conceptsStartY + 5,
            theme: "striped",
            head: [[
                "Codigo",
                "Concepto",
                "Categoria",
                "Cantidad",
                "Valor unitario",
                "Total",
            ]],
            body: conceptRows,
            styles: {
                fontSize: 8,
                cellPadding: 2,
            },
            columnStyles: {
                1: { cellWidth: 45 },
            },
        });

        // Resumen de liquidación
        const summaryY = doc.lastAutoTable.finalY + 12;

        autoTable(doc, {
            startY: summaryY,
            theme: "grid",
            body: [
                ["Total devengado", currency.format(payslip.totalEarnings)],
                ["Total deducciones", currency.format(payslip.totalDeductions)],
                ["NETO A PAGAR", currency.format(payslip.netPay)],
            ],
            styles: {
                fontSize: 10,
                cellPadding: 3,
            },
            columnStyles: {
                0: { fontStyle: "bold" },
                1: { halign: "right" },
            },
            didParseCell: (data) => {
                if (data.row.index === 2) {
                    data.cell.styles.fontStyle = "bold";
                }
            },
        });

        // Pie de página
        const pageHeight = doc.internal.pageSize.height;

        doc.setFontSize(8);
        doc.text(
            "Documento generado por NominaEmpresa.",
            105,
            pageHeight - 10,
            { align: "center" }
        );

        // Descargar archivo
        doc.save(
            `Desprendible_${payslip.identification}_${payslip.periodYear}_${payslip.periodMonth}.pdf`
        );
    }

    if (loading) {
        return (
            <div className="text-center py-4">
                <div className="spinner-border text-primary" />
                <p className="mt-2">Cargando desprendibles...</p>
            </div>
        );
    }

    return (
        <div className="mt-4">
            <div className="card border-0 shadow-sm">
                <div className="card-body">
                    <h4 className="mb-3">Mis desprendibles</h4>

                    <p className="text-muted">
                        Consulta los comprobantes de pago de tus períodos de nómina.
                    </p>

                    {error && (
                        <div className="alert alert-danger">
                            {error}
                        </div>
                    )}

                    {payslips.length === 0 ? (
                        <div className="alert alert-info mb-0">
                            Todavía no tienes desprendibles disponibles.
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

                                            <td>{currency.format(item.totalEarnings)}</td>

                                            <td>{currency.format(item.totalDeductions)}</td>

                                            <td className="fw-bold">
                                                {currency.format(item.netPay)}
                                            </td>

                                            <td>
                                                <button
                                                    className="btn btn-sm btn-outline-primary"
                                                    onClick={() =>
                                                        showPayslip(item.payrollDetailId)
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

            {detailLoading && (
                <div className="text-center py-3">
                    <div className="spinner-border spinner-border-sm text-primary" />
                    <span className="ms-2">Consultando desprendible...</span>
                </div>
            )}

            {selectedPayslip && (
                <div className="card border-0 shadow-sm mt-4">
                    <div className="card-body">
                        <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
                            <h4 className="mb-0">Detalle del desprendible</h4>

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

                        <div className="row g-3 mb-4">
                            <div className="col-md-6">
                                <strong>Empleado:</strong>
                                <p>{selectedPayslip.employeeName}</p>
                            </div>

                            <div className="col-md-6">
                                <strong>Identificación:</strong>
                                <p>{selectedPayslip.identification}</p>
                            </div>

                            <div className="col-md-6">
                                <strong>Cargo:</strong>
                                <p>{selectedPayslip.position}</p>
                            </div>

                            <div className="col-md-6">
                                <strong>Salario mensual:</strong>
                                <p>{currency.format(selectedPayslip.monthlySalary)}</p>
                            </div>
                        </div>

                        <h6>Conceptos de nómina</h6>

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
                                    {selectedPayslip.concepts.map((concept) => (
                                        <tr key={concept.id}>
                                            <td>{concept.description}</td>
                                            <td>{concept.category}</td>
                                            <td>{concept.quantity ?? "-"}</td>
                                            <td>
                                                {concept.unitValue != null
                                                    ? currency.format(concept.unitValue)
                                                    : "-"}
                                            </td>
                                            <td>{currency.format(concept.amount)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <div className="row justify-content-end">
                            <div className="col-md-5">
                                <div className="d-flex justify-content-between mb-2">
                                    <span>Total devengado:</span>
                                    <strong>
                                        {currency.format(selectedPayslip.totalEarnings)}
                                    </strong>
                                </div>

                                <div className="d-flex justify-content-between mb-2">
                                    <span>Total deducciones:</span>
                                    <strong>
                                        {currency.format(selectedPayslip.totalDeductions)}
                                    </strong>
                                </div>

                                <hr />

                                <div className="d-flex justify-content-between">
                                    <span className="fw-bold">NETO A PAGAR:</span>
                                    <strong className="text-success fs-5">
                                        {currency.format(selectedPayslip.netPay)}
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