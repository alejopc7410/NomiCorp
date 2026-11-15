import {useEffect, useState} from "react";
import {payrollApi} from "./services/payroll";
import {generatePayslipPdf} from "./services/generatePayslipPdf";
import {getAccessToken} from "./services/auth";

const months = [
    "Enero", "Febrero", "Marzo", "Abril",
    "Mayo", "Junio", "Julio", "Agosto",
    "Septiembre", "Octubre", "Noviembre", "Diciembre"
];

function formatCurrency(value) {
    return new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP",
        maximumFractionDigits: 0
    }).format(value || 0);
}

function formatDate(date) {
    if (!date) return "-";

    return new Date(`${date}T00:00:00`).toLocaleDateString("es-CO");
}

function statusClass(status) {
    switch (status) {
        case "Abierto":
            return "bg-warning text-dark";
        case "Calculado":
            return "bg-info text-dark";
        case "Cerrado":
            return "bg-success";
        default:
            return "bg-secondary";
    }
}

export default function Payroll() {
    const currentDate = new Date();

    const [periods, setPeriods] = useState([]);
    const [year, setYear] = useState(currentDate.getFullYear());
    const [month, setMonth] = useState(currentDate.getMonth() + 1);

    const [details, setDetails] = useState(null);
    const [selectedPeriod, setSelectedPeriod] = useState(null);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    useEffect(() => {
        loadPeriods();
    }, []);

    async function registerPayslipIssue(payrollDetailId) {
        const API_URL =
            import.meta.env.VITE_API_URL;

        const response = await fetch(
            `${API_URL}/api/payslip-issues/${payrollDetailId}`,
            {
                method: "POST",
                headers: {
                    Authorization:
                        `Bearer ${getAccessToken()}`
                }
            }
        );

        if (!response.ok) {
            throw new Error(
                "El PDF se generó, pero no se pudo " +
                "registrar su emisión."
            );
        }

        return response.json();
    }

    async function loadPeriods() {
        setLoading(true);
        setError("");

        try {
            const data = await payrollApi.getPeriods();
            setPeriods(data || []);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    async function handleCreatePeriod(event) {
        event.preventDefault();

        setSaving(true);
        setError("");
        setMessage("");

        try {
            await payrollApi.createPeriod({
                year: Number(year),
                month: Number(month)
            });

            setMessage("El periodo de nómina fue creado correctamente.");
            await loadPeriods();
        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    async function handleCalculate(period) {
        const confirmed = window.confirm(
            `¿Deseas calcular la nómina de ${months[period.month - 1]} ${period.year}?`
        );

        if (!confirmed) return;

        setSaving(true);
        setError("");
        setMessage("");

        try {
            const result = await payrollApi.calculatePeriod(period.id);

            setMessage(
                `${result.message} Contratos procesados: ${result.processedContracts}.`
            );

            await loadPeriods();
        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    async function handleViewDetails(period) {
        setError("");
        setMessage("");
        setDetails(null);
        setSelectedPeriod(period);
        setSaving(true);

        try {
            const result = await payrollApi.getDetails(period.id);
            setDetails(result);
        } catch (err) {
            setError(err.message);
            setSelectedPeriod(null);
        } finally {
            setSaving(false);
        }
    }

    async function handleClose(period) {
        const confirmed = window.confirm(
            `¿Estás seguro de cerrar la nómina de ${months[period.month - 1]} ${period.year}? Esta acción finalizará el periodo.`
        );

        if (!confirmed) return;

        setSaving(true);
        setError("");
        setMessage("");

        try {
            const result = await payrollApi.closePeriod(period.id);

            setMessage(result.message);
            await loadPeriods();

            if (selectedPeriod?.id === period.id) {
                const updatedDetails = await payrollApi.getDetails(period.id);
                setDetails(updatedDetails);
                setSelectedPeriod({
                    ...period,
                    status: "Cerrado"
                });
            }
        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    return (
        <div className="container-fluid py-4">

            {/* Encabezado */}
            <div className="mb-4">
                <h2 className="fw-bold">
                    <i className="bi bi-calculator me-2"></i>
                    Gestión de nómina
                </h2>

                <p className="text-muted">
                    Crea, calcula y administra los periodos de nómina de la empresa.
                </p>
            </div>

            {/* Mensajes */}
            {error && (
                <div className="alert alert-danger alert-dismissible">
                    {error}
                    <button
                        type="button"
                        className="btn-close"
                        onClick={() => setError("")}
                    />
                </div>
            )}

            {message && (
                <div className="alert alert-success alert-dismissible">
                    {message}
                    <button
                        type="button"
                        className="btn-close"
                        onClick={() => setMessage("")}
                    />
                </div>
            )}

            {/* Crear periodo */}
            <div className="card border-0 shadow-sm mb-4">
                <div className="card-header bg-white py-3">
                    <h5 className="mb-0 fw-semibold">
                        Crear periodo de nómina
                    </h5>
                </div>

                <div className="card-body">
                    <form
                        onSubmit={handleCreatePeriod}
                        className="row g-3 align-items-end"
                    >
                        <div className="col-md-4">
                            <label className="form-label">
                                Año
                            </label>

                            <input
                                type="number"
                                className="form-control"
                                min="2020"
                                max="2100"
                                value={year}
                                onChange={(e) => setYear(e.target.value)}
                                required
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Mes
                            </label>

                            <select
                                className="form-select"
                                value={month}
                                onChange={(e) => setMonth(e.target.value)}
                                required
                            >
                                {months.map((name, index) => (
                                    <option key={index} value={index + 1}>
                                        {name}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="col-md-4">
                            <button
                                type="submit"
                                className="btn btn-primary w-100"
                                disabled={saving}
                            >
                                <i className="bi bi-plus-circle me-2"></i>
                                Crear periodo
                            </button>
                        </div>
                    </form>
                </div>
            </div>

            {/* Lista de periodos */}
            <div className="card border-0 shadow-sm mb-4">
                <div className="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                    <h5 className="mb-0 fw-semibold">
                        Periodos registrados
                    </h5>

                    <button
                        className="btn btn-outline-secondary btn-sm"
                        onClick={loadPeriods}
                        disabled={loading}
                    >
                        <i className="bi bi-arrow-clockwise me-1"></i>
                        Actualizar
                    </button>
                </div>

                <div className="card-body">
                    {loading ? (
                        <div className="text-center py-4">
                            <div className="spinner-border text-primary"></div>
                            <p className="mt-2 text-muted">
                                Cargando periodos...
                            </p>
                        </div>
                    ) : periods.length === 0 ? (
                        <div className="text-center text-muted py-4">
                            No hay periodos de nómina registrados.
                        </div>
                    ) : (
                        <div className="table-responsive">
                            <table className="table table-hover align-middle">
                                <thead className="table-light">
                                    <tr>
                                        <th>Periodo</th>
                                        <th>Fecha inicial</th>
                                        <th>Fecha final</th>
                                        <th>Empleados</th>
                                        <th>Estado</th>
                                        <th>Acciones</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {periods.map((period) => (
                                        <tr key={period.id}>
                                            <td className="fw-semibold">
                                                {months[period.month - 1]} {period.year}
                                            </td>

                                            <td>{formatDate(period.startDate)}</td>

                                            <td>{formatDate(period.endDate)}</td>

                                            <td>{period.employeeCount}</td>

                                            <td>
                                                <span className={`badge ${statusClass(period.status)}`}>
                                                    {period.status}
                                                </span>
                                            </td>

                                            <td>
                                                <div className="d-flex flex-wrap gap-2">
                                                    <button
                                                        className="btn btn-outline-primary btn-sm"
                                                        onClick={() => handleViewDetails(period)}
                                                        disabled={saving}
                                                    >
                                                        <i className="bi bi-eye me-1"></i>
                                                        Detalles
                                                    </button>

                                                    {period.status === "Abierto" && (
                                                        <button
                                                            className="btn btn-primary btn-sm"
                                                            onClick={() => handleCalculate(period)}
                                                            disabled={saving}
                                                        >
                                                            <i className="bi bi-calculator me-1"></i>
                                                            Calcular
                                                        </button>
                                                    )}

                                                    {period.status === "Calculado" && (
                                                        <button
                                                            className="btn btn-success btn-sm"
                                                            onClick={() => handleClose(period)}
                                                            disabled={saving}
                                                        >
                                                            <i className="bi bi-lock me-1"></i>
                                                            Cerrar
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>

            {/* Detalle del periodo */}
            {details && (
                <div className="card border-0 shadow-sm mb-4">
                    <div className="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                        <div>
                            <h5 className="mb-1 fw-semibold">
                                Detalle de nómina
                            </h5>

                            <span className="text-muted">
                                {months[details.period.month - 1]} {details.period.year}
                            </span>
                        </div>

                        <button
                            className="btn btn-outline-secondary btn-sm"
                            onClick={() => {
                                setDetails(null);
                                setSelectedPeriod(null);
                            }}
                        >
                            Cerrar detalle
                        </button>
                    </div>

                    <div className="card-body">

                        {/* Resumen */}
                        <div className="row g-3 mb-4">
                            <div className="col-md-3">
                                <div className="p-3 bg-light rounded">
                                    <small className="text-muted">
                                        Empleados
                                    </small>
                                    <h4 className="fw-bold mb-0">
                                        {details.employeeCount}
                                    </h4>
                                </div>
                            </div>

                            <div className="col-md-3">
                                <div className="p-3 bg-light rounded">
                                    <small className="text-muted">
                                        Total devengado
                                    </small>
                                    <h5 className="fw-bold mb-0">
                                        {formatCurrency(details.totalEarnings)}
                                    </h5>
                                </div>
                            </div>

                            <div className="col-md-3">
                                <div className="p-3 bg-light rounded">
                                    <small className="text-muted">
                                        Total deducciones
                                    </small>
                                    <h5 className="fw-bold mb-0">
                                        {formatCurrency(details.totalDeductions)}
                                    </h5>
                                </div>
                            </div>

                            <div className="col-md-3">
                                <div className="p-3 bg-light rounded">
                                    <small className="text-muted">
                                        Total neto
                                    </small>
                                    <h5 className="fw-bold text-success mb-0">
                                        {formatCurrency(details.totalNetPay)}
                                    </h5>
                                </div>
                            </div>
                        </div>

                        {/* Empleados */}
                        <div className="table-responsive">
                            <table className="table table-bordered table-hover align-middle">
                                <thead className="table-light">
                                    <tr>
                                        <th>Identificación</th>
                                        <th>Empleado</th>
                                        <th>Cargo</th>
                                        <th>Salario mensual</th>
                                        <th>Devengado</th>
                                        <th>Deducciones</th>
                                        <th>Neto a pagar</th>
                                        <th>Desprendible</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {details.details.map((employee) => (
                                        <tr key={employee.id}>
                                            <td>{employee.identification}</td>
                                            <td>{employee.employeeName}</td>
                                            <td>{employee.position || "-"}</td>
                                            <td>{formatCurrency(employee.monthlySalary)}</td>
                                            <td>{formatCurrency(employee.totalEarnings)}</td>
                                            <td>{formatCurrency(employee.totalDeductions)}</td>
                                            <td className="fw-bold text-success">
                                                {formatCurrency(employee.netPay)}
                                            </td>
                                            <td>
                                                <button
                                                    type="button"
                                                    className="btn btn-outline-danger btn-sm"
                                                    onClick={async () => {
                                                        try {
                                                            await generatePayslipPdf(
                                                                details.period,
                                                                employee
                                                            );

                                                            await registerPayslipIssue(employee.id);

                                                            alert(
                                                                "Comprobante generado y emisión registrada."
                                                            );
                                                        } catch (error) {
                                                            alert(error.message);
                                                        }
                                                    }
                                                    }
                                                >
                                                    <i className="bi bi-file-earmark-pdf me-1"></i>
                                                    Descargar PDF
                                                </button>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        {/* Conceptos por empleado */}
                        {details.details.map((employee) => (
                            <div key={employee.id} className="mt-4">
                                <h6 className="fw-bold">
                                    {employee.employeeName} - Conceptos de liquidación
                                </h6>

                                {employee.concepts.length === 0 ? (
                                    <p className="text-muted">
                                        No hay conceptos registrados.
                                    </p>
                                ) : (
                                    <div className="table-responsive">
                                        <table className="table table-sm table-striped">
                                            <thead>
                                                <tr>
                                                    <th>Código</th>
                                                    <th>Descripción</th>
                                                    <th>Categoría</th>
                                                    <th>Cantidad</th>
                                                    <th>Valor unitario</th>
                                                    <th>Valor</th>
                                                </tr>
                                            </thead>

                                            <tbody>
                                                {employee.concepts.map((concept) => (
                                                    <tr key={concept.id}>
                                                        <td>{concept.code}</td>
                                                        <td>{concept.description}</td>
                                                        <td>{concept.category}</td>
                                                        <td>{concept.quantity ?? "-"}</td>
                                                        <td>
                                                            {concept.unitValue == null
                                                                ? "-"
                                                                : formatCurrency(concept.unitValue)}
                                                        </td>
                                                        <td>{formatCurrency(concept.amount)}</td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                </div>
            )}

        </div>
    );
}