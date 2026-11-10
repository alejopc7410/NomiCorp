import BonusPaymentReconciliation from "./BonusPaymentReconciliation";
import { useEffect, useState } from "react";
import {bonusesApi, bonusSettlementsApi} from "./services/bonuses";

const formatCOP = (value) =>
    new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP"
    }).format(value ?? 0);

const semesterName = (semester) =>
    Number(semester) === 1
        ? "Enero - Junio"
        : "Julio - Diciembre";


export default function Bonuses() {
    const [cutoffDate, setCutoffDate] = useState(
        new Intl.DateTimeFormat("sv-SE", {
            timeZone: "America/Bogota",
            year: "numeric",
            month: "2-digit",
            day: "2-digit"
        }).format(new Date())
    );
    const [newCutoffDate, setNewCutoffDate] = useState("");
    const [updatingCutoff, setUpdatingCutoff] = useState(false);

    const [year, setYear] = useState(2026);
    const [semester, setSemester] = useState(2);

    const [preview, setPreview] = useState(null);
    const [settlements, setSettlements] = useState([]);
    const [selected, setSelected] = useState(null);

    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [recalculating, setRecalculating] = useState(false);

    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    const [validation, setValidation] = useState(null);
    const [validating, setValidating] = useState(false);

    async function loadSettlements() {
        const data = await bonusSettlementsApi.getAll();
        setSettlements(data);
    }

    useEffect(() => {
        loadSettlements().catch((err) => {
            setError(err.message);
        });
    }, []);


    async function updateCutoff() {
        if (!selected || !newCutoffDate) return;

        if (selected.status !== "Borrador") {
            setError(
                "Solo se puede modificar la fecha de " +
                "una liquidación en estado Borrador."
            );
            return;
        }

        const confirmed = window.confirm(
            "¿Deseas cambiar la fecha de corte?\n\n" +
            "El sistema recalculará todos los valores " +
            "de prima y reemplazará los detalles del borrador."
        );

        if (!confirmed) return;

        setUpdatingCutoff(true);
        setError("");
        setMessage("");
        setValidation(null);

        try {
            await bonusSettlementsApi.updateCutoff(
                selected.id,
                newCutoffDate
            );

            await viewSettlement(selected.id);
            await loadSettlements();

            setMessage(
                "Fecha de corte y liquidación " +
                "actualizadas correctamente."
            );
        } catch (err) {
            setError(
                err.message ||
                "No fue posible actualizar la fecha de corte."
            );
        } finally {
            setUpdatingCutoff(false);
        }
    }


    async function calculatePreview() {
        setLoading(true);
        setError("");
        setMessage("");
        setPreview(null);

        try {
            const data = await bonusesApi.preview(
                year,
                semester,
                cutoffDate
            );

            setPreview(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    async function saveDraft() {
        if (!preview) return;

        setSaving(true);
        setError("");
        setMessage("");

        try {
            const saved = await bonusSettlementsApi.create(
                preview.year,
                preview.semester,
                preview.endDate
            );

            await loadSettlements();
            await viewSettlement(saved.id);

            setMessage(
                "Borrador guardado correctamente."
            );
            setPreview(null);
        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    async function viewSettlement(id) {
        setValidation(null);
        setError("");

        try {
            const data =
                await bonusSettlementsApi.getById(id);

            setNewCutoffDate(data.endDate);

            setSelected(data);
        } catch (err) {
            setError(err.message);
        }
    }


    async function recalculateDraft() {
        if (!selected) return;

        if (selected.status !== "Borrador") {
            setError(
                "Solo se pueden recalcular liquidaciones en estado Borrador."
            );
            return;
        }

        const confirmed = window.confirm(
            "¿Deseas actualizar este borrador?\n\n" +
            "El sistema consultará nuevamente los salarios " +
            "y las novedades del período, incluyendo las " +
            "novedades salariales aprobadas.\n\n" +
            "Se reemplazarán los valores anteriores del borrador."
        );

        if (!confirmed) return;

        setRecalculating(true);
        setError("");
        setMessage("");
        setValidation(null);

        try {
            // Recalcular con la fecha de corte guardada.
            const response =
                await bonusSettlementsApi.recalculate(
                    selected.id
                );

            // Obtener los detalles actualizados desde SQL Server.
            const updated =
                await bonusSettlementsApi.getById(
                    selected.id
                );

            // Actualizar inmediatamente la pantalla.
            setSelected(updated);
            setNewCutoffDate(updated.endDate);

            // Actualizar el historial.
            await loadSettlements();

            setMessage(
                response.message ||
                "El borrador se actualizó correctamente. " +
                "Se incluyeron las novedades salariales " +
                "aprobadas hasta la fecha de corte."
            );

        } catch (err) {
            setError(
                err.message ||
                "No fue posible recalcular el borrador."
            );
        } finally {
            setRecalculating(false);
        }
    }



    async function validateSettlement() {
        if (!selected) return;

        setValidating(true);
        setError("");
        setValidation(null);

        try {
            const result =
                await bonusSettlementsApi.validate(
                    selected.id
                );

            setValidation(result);
        } catch (err) {
            setError(err.message);
        } finally {
            setValidating(false);
        }
    }


    return (
        <div className="container-fluid py-4">
            <h2 className="mb-2">
                Prima de servicios
            </h2>

            <p className="text-muted mb-3">
                Proyecciones y borradores de
                liquidaciones semestrales.
            </p>

            {error && (
                <div className="alert alert-danger">
                    {error}
                </div>
            )}

            {message && (
                <div className="alert alert-success">
                    {message}
                </div>
            )}

            {/* CALCULAR PRIMA */}
            <div className="card mb-4">
                <div className="card-header">
                    <strong>
                        Nueva proyección de prima
                    </strong>
                </div>

                <div className="card-body">
                    <div className="row g-3 align-items-center justify-content-center text-center">
                        <div className="col-md-3">
                            <label className="form-label">
                                Año
                            </label>

                            <input
                                type="number"
                                className="form-control"
                                value={year}
                                onChange={(e) =>
                                    setYear(Number(e.target.value))
                                }
                            />
                        </div>

                        <div className="col-md-4">
                            <label className="form-label">
                                Semestre
                            </label>

                            <select
                                className="form-select"
                                value={semester}
                                onChange={(e) =>
                                    setSemester(Number(e.target.value))
                                }
                            >
                                <option value={1}>
                                    Enero - Junio
                                </option>

                                <option value={2}>
                                    Julio - Diciembre
                                </option>
                            </select>
                        </div>


                        <div className="col-md-3">
                            <label className="form-label">
                                Fecha de corte *
                            </label>

                            <input
                                type="date"
                                className="form-control"
                                value={cutoffDate}
                                onChange={(e) =>
                                    setCutoffDate(e.target.value)
                                }
                                required
                            />

                            
                        </div>


                        <div className="col-md-3">
                            <button
                                className="btn btn-primary"
                                disabled={loading}
                                onClick={calculatePreview}
                            >
                                {loading
                                    ? "Calculando..."
                                    : "Calcular prima"}
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            {/* VISTA PREVIA */}
            {preview && (
                <div className="card mb-4">
                    <div className="card-header">
                        <strong>
                            Resultado preliminar
                        </strong>
                    </div>

                    <div className="card-body">
                        <div className="alert alert-warning">
                            {preview.message}
                        </div>

                        <p>
                            Empleados:
                            {" "}
                            <strong>
                                {preview.employeeCount}
                            </strong>
                        </p>

                        <p>
                            <strong>Fecha de inicio:</strong>{" "}
                            {preview.startDate}
                        </p>

                        <p className="mb-2">
                            <strong>Fecha de corte:</strong>{" "}
                            {preview.endDate}
                        </p>

                        <h4>
                            {formatCOP(preview.totalBonus)}
                        </h4>

                        <div className="table-responsive">
                            <table className="table table-striped">
                                <thead>
                                    <tr>
                                        <th>Empleado</th>
                                        <th>Días</th>
                                        <th>Salario acumulado</th>
                                        <th>Auxilio acumulado</th>
                                        <th>Prima estimada</th>
                                        <th>Novedades salariales</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {preview.details.map((d) => (
                                        <tr key={d.contractId}>
                                            <td>{d.employeeName}</td>
                                            <td>{d.workedDays}</td>
                                            <td>
                                                {formatCOP(
                                                    d.salaryAccrued
                                                )}
                                            </td>
                                            <td>
                                                {formatCOP(
                                                    d.transportAccrued
                                                )}
                                            </td>
                                            <td>
                                                {formatCOP(
                                                    d.estimatedBonus
                                                )}
                                            </td>
                                            <td>
                                                {formatCOP(d.salaryNovelties)}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <button
                            className="btn btn-success"
                            onClick={saveDraft}
                            disabled={saving}
                        >
                            {saving
                                ? "Guardando..."
                                : "Guardar borrador"}
                        </button>
                    </div>
                </div>
            )}

            {/* HISTORIAL */}
            <div className="card mb-4">
                <div className="card-header d-flex justify-content-between align-items-center">
                    <strong>
                        Historial de liquidaciones
                    </strong>

                    <button
                        className="btn btn-outline-secondary btn-sm"
                        onClick={() =>
                            loadSettlements().catch(
                                (err) => setError(err.message)
                            )
                        }
                    >
                        Actualizar
                    </button>
                </div>

                <div className="card-body">
                    {settlements.length === 0 ? (
                        <p className="text-muted mb-0">
                            No hay liquidaciones guardadas.
                        </p>
                    ) : (
                        <div className="table-responsive">
                            <table className="table table-hover">
                                <thead>
                                    <tr>
                                        <th>Año</th>
                                        <th>Semestre</th>
                                        <th>Estado</th>
                                        <th>Empleados</th>
                                        <th>Total</th>
                                        <th>Acción</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {settlements.map((item) => (
                                        <tr key={item.id}>
                                            <td>{item.year}</td>
                                            <td>
                                                {semesterName(
                                                    item.semester
                                                )}
                                            </td>
                                            <td>
                                                <span className="badge bg-secondary">
                                                    {item.status}
                                                </span>
                                            </td>
                                            <td>
                                                {item.employeeCount}
                                            </td>
                                            <td>
                                                {formatCOP(
                                                    item.totalAmount
                                                )}
                                            </td>
                                            <td>
                                                <button
                                                    className="btn btn-outline-primary btn-sm"
                                                    onClick={() =>
                                                        viewSettlement(item.id)
                                                    }
                                                >
                                                    Ver detalles
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

            {/* LIQUIDACIÓN SELECCIONADA */}
            {selected && (
                <div className="card mb-4">
                    
                    <div className="card-header bg-white py-3 d-flex justify-content-between align-items-center">
                        <div>
                            <h5 className="mb-1 fw-semibold">
                                <strong>
                                    Liquidación #{selected.id}
                                </strong>
                            </h5>
                        </div>

                        <button
                            className="btn btn-outline-secondary btn-sm"
                            onClick={() => {
                                setSelected(null);
                            }}
                        >
                            Cerrar detalle
                        </button>
                    </div>

                    <div className="card-body">
                        <p>
                            <strong>Período: </strong>
                            {selected.year} -{" "}
                            {semesterName(selected.semester)}
                        </p>

                        <p>
                            <strong>Fecha de corte:</strong>{" "}
                            {selected.endDate}
                        </p>

                        <p>
                            <strong>Estado: </strong>
                            {selected.status}
                        </p>

                        <h4>
                            Total: {formatCOP(
                                selected.totalAmount
                            )}
                        </h4>


                        {selected.status === "Borrador" && (
                            <div className="card border-warning mb-4">
                                <div className="card-body text-center">
                                    <h5 className="card-title">
                                        Modificar fecha de corte
                                    </h5>

                                    <p className="text-muted mb-3">
                                        Al cambiar la fecha, se recalcularán
                                        los salarios, las novedades salariales
                                        y los días causados de prima.
                                    </p>

                                    <div className="row g-3 align-items-center align-items-center justify-content-center">
                                        <div className="col-md-4 text-center">
                                            <label className="form-label">
                                                Nueva fecha de corte
                                            </label>

                                            <input
                                                type="date"
                                                className="form-control"
                                                value={newCutoffDate}
                                                min={selected.startDate}
                                                max={new Intl.DateTimeFormat(
                                                    "sv-SE",
                                                    {
                                                        timeZone: "America/Bogota",
                                                        year: "numeric",
                                                        month: "2-digit",
                                                        day: "2-digit"
                                                    }
                                                ).format(new Date())}
                                                onChange={(e) =>
                                                    setNewCutoffDate(e.target.value)
                                                }
                                                required
                                            />
                                        </div>

                                        <div className="col-md-4 text-center">
                                            <button
                                                type="button"
                                                className="btn btn-warning"
                                                disabled={
                                                    updatingCutoff ||
                                                    !newCutoffDate ||
                                                    newCutoffDate === selected.endDate
                                                }
                                                onClick={updateCutoff}
                                            >
                                                {updatingCutoff
                                                    ? "Actualizando..."
                                                    : "Actualizar fecha y recalcular"}
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        )}


                        <div className="table-responsive mt-3">
                            <table className="table table-bordered">
                                <thead>
                                    <tr>
                                        <th>Empleado</th>
                                        <th>Identificación</th>
                                        <th>Días</th>
                                        <th>Salario</th>
                                        <th>Auxilio</th>
                                        <th>Prima</th>
                                        <th>Novedades salariales</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {selected.details.map((d) => (
                                        <tr key={d.id}>
                                            <td>{d.employeeName}</td>
                                            <td>{d.identification}</td>
                                            <td>{d.workedDays}</td>
                                            <td>
                                                {formatCOP(
                                                    d.salaryAccrued
                                                )}
                                            </td>
                                            <td>
                                                {formatCOP(
                                                    d.transportAccrued
                                                )}
                                            </td>
                                            <td>
                                                <strong>
                                                    {formatCOP(
                                                        d.bonusAmount
                                                    )}
                                                </strong>
                                            </td>
                                            <td>
                                                {formatCOP(d.salaryNovelties)}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>

                            {selected && (
                                <BonusPaymentReconciliation
                                    settlement={selected}
                                />
                            )}

                            <div className="mt-4">
                                <h5>Validación de la liquidación</h5>

                                <p className="text-muted">
                                    Comprueba que los datos guardados cumplan
                                    las condiciones básicas antes de avanzar
                                    con la aprobación.
                                </p>
                                <br/>
                                <button
                                    className="btn btn-outline-primary"
                                    disabled={validating}
                                    onClick={validateSettlement}
                                >
                                    {validating
                                        ? "Validando..."
                                        : "Validar liquidación"}
                                </button>
                                <br/>
                                <br/>
                                {selected.status === "Borrador" && (
                                    <button
                                        type="button"
                                        className="btn btn-warning"
                                        disabled={recalculating || updatingCutoff}
                                        onClick={recalculateDraft}
                                    >
                                        {recalculating
                                            ? "Actualizando borrador..."
                                            : "Recalcular borrador"}
                                    </button>
                                )}

                                {validation && (
                                    <div className="mt-3">
                                        <div className="alert alert-info">
                                            <strong>Estado:</strong>{" "}
                                            {validation.readyForApproval
                                                ? "Lista para aprobación"
                                                : "Pendiente de revisión"}
                                        </div>

                                        {validation.errors.some(error =>
                                            error.toLowerCase().includes("recalcula") ||
                                            error.toLowerCase().includes("desactualizado")
                                        ) && (
                                                <p className="mt-3 mb-0">
                                                    <strong>Acción recomendada:</strong>{" "}
                                                    revisa los cambios realizados y utiliza
                                                    el botón "Recalcular borrador".
                                                    Después ejecuta nuevamente la validación.
                                                </p>)
                                        }

                                        {validation.warnings.length > 0 && (
                                            <div className="alert alert-warning">
                                                <h6>Advertencias</h6>

                                                <ul className="mb-0">
                                                    {validation.warnings.map(
                                                        (item, index) => (
                                                            <li key={index}>
                                                                {item}
                                                            </li>
                                                        )
                                                    )}
                                                </ul>
                                            </div>
                                        )}
                                    </div>
                                )}
                            </div>
                        </div>

                    </div>
                </div>
            )}
        </div>
    );
}

