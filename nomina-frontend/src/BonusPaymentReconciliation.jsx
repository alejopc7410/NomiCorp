
import { useEffect, useState } from "react";
import { bonusPreviousPaymentsApi } from "./services/bonuses";

const money = (amount) =>
    new Intl.NumberFormat("es-CO", {
        style: "currency",
        currency: "COP"
    }).format(amount ?? 0);

export default function BonusPaymentReconciliation({
    settlement
}) {
    const [summary, setSummary] = useState(null);
    const [employeeId, setEmployeeId] = useState("");
    const [amount, setAmount] = useState("");
    const [paymentDate, setPaymentDate] = useState("");
    const [reference, setReference] = useState("");
    const [notes, setNotes] = useState("");

    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    useEffect(() => {
        let active = true;

        setSummary(null);
        setError("");
        setLoading(true);

        bonusPreviousPaymentsApi.getSummary(settlement.id)
            .then((data) => {
                if (active) setSummary(data);
            })
            .catch((err) => {
                if (active) setError(err.message);
            })
            .finally(() => {
                if (active) setLoading(false);
            });

        return () => {
            active = false;
        };
    }, /*[
        settlement.id,
        settlement.totalAmount,
        settlement.endDate
    ]*/[settlement]);

    async function registerPayment(event) {
        event.preventDefault();

        if (!employeeId || !amount || !paymentDate) {
            setError("Completa los datos obligatorios.");
            return;
        }

        const confirmed = window.confirm(
            "Confirma que este pago ya se realizó y " +
            "que tienes un comprobante que lo respalda."
        );

        if (!confirmed) return;

        setSaving(true);
        setError("");
        setMessage("");

        try {
            await bonusPreviousPaymentsApi.register(
                settlement.id,
                {
                    employeeId: Number(employeeId),
                    amount: Number(amount),
                    paymentDate,
                    reference,
                    notes
                }
            );

            const updated =
                await bonusPreviousPaymentsApi.getSummary(
                    settlement.id
                );

            setSummary(updated);

            setAmount("");
            setPaymentDate("");
            setReference("");
            setNotes("");

            setMessage(
                "Pago anterior registrado correctamente."
            );
        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    return (
        <div className="card mt-4">
            <div className="card-header">
                <strong>Conciliación de prima</strong>
            </div>

            <div className="card-body">
                <p className="text-muted">
                    Registra únicamente pagos que ya
                    fueron realizados y cuentan con
                    un soporte verificable.
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

                {loading && <p>Cargando conciliación...</p>}

                {summary && (
                    <>
                        <div className="row g-3 mb-4">
                            <div className="col-md-4">
                                <div className="border rounded p-3">
                                    <small>Prima calculada</small>
                                    <h5>{money(summary.totalCalculated)}</h5>
                                </div>
                            </div>

                            <div className="col-md-4">
                                <div className="border rounded p-3">
                                    <small>Pagos anteriores</small>
                                    <h5>{money(summary.totalPreviouslyPaid)}</h5>
                                </div>
                            </div>

                            <div className="col-md-4">
                                <div className="border rounded p-3">
                                    <small>Saldo estimado</small>
                                    <h5>{money(summary.totalPending)}</h5>
                                </div>
                            </div>
                        </div>

                        <h6>Resumen por empleado</h6>

                        <div className="table-responsive mb-4">
                            <table className="table table-striped">
                                <thead>
                                    <tr>
                                        <th>Empleado</th>
                                        <th>Prima calculada</th>
                                        <th>Pagos anteriores</th>
                                        <th>Saldo</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {summary.employees.map((item) => (
                                        <tr key={item.employeeId}>
                                            <td>{item.employeeName}</td>
                                            <td>{money(item.calculatedBonus)}</td>
                                            <td>{money(item.previouslyPaid)}</td>
                                            <td>{money(item.pendingBalance)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        {summary.employees.some(
                            e => e.pendingBalance < 0
                        ) && (
                                <div className="alert alert-danger">
                                    Hay pagos registrados superiores
                                    a la prima calculada.
                                    Revisa el borrador y los comprobantes.
                                </div>
                            )}

                        <h5 className="mb-4">Pagos anteriores registrados</h5>

                        <div className="table-responsive mb-4">
                            <table className="table table-sm">
                                <thead>
                                    <tr>
                                        <th>Empleado</th>
                                        <th>Fecha</th>
                                        <th>Referencia</th>
                                        <th>Valor</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {summary.payments.length === 0 ? (
                                        <tr>
                                            <td colSpan="4">
                                                No hay pagos registrados.
                                            </td>
                                        </tr>
                                    ) : summary.payments.map((payment) => (
                                        <tr key={payment.id}>
                                            <td>
                                                {summary.employees.find(
                                                    e => e.employeeId ===
                                                        payment.employeeId
                                                )?.employeeName ?? payment.employeeId}
                                            </td>
                                            <td>{payment.paymentDate}</td>
                                            <td>{payment.reference}</td>
                                            <td>{money(payment.amount)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        {settlement.status === "Borrador" && (
                            <>
                                <h6>Registrar pago ya realizado</h6>

                                <form onSubmit={registerPayment}>
                                    <div className="row g-3">
                                        <div className="col-md-6">
                                            <label className="form-label">
                                                Empleado *
                                            </label>
                                            <select
                                                className="form-select"
                                                value={employeeId}
                                                onChange={e =>
                                                    setEmployeeId(e.target.value)
                                                }
                                                required
                                            >
                                                <option value="">
                                                    Selecciona un empleado
                                                </option>
                                                {summary.employees.map(e => (
                                                    <option
                                                        key={e.employeeId}
                                                        value={e.employeeId}
                                                    >
                                                        {e.employeeName}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>

                                        <div className="col-md-3">
                                            <label className="form-label">
                                                Valor pagado *
                                            </label>
                                            <input
                                                className="form-control"
                                                type="number"
                                                min="0.01"
                                                step="0.01"
                                                value={amount}
                                                onChange={e =>
                                                    setAmount(e.target.value)
                                                }
                                                required
                                            />
                                        </div>

                                        <div className="col-md-3">
                                            <label className="form-label">
                                                Fecha del pago *
                                            </label>
                                            <input
                                                className="form-control"
                                                type="date"
                                                value={paymentDate}
                                                onChange={e =>
                                                    setPaymentDate(e.target.value)
                                                }
                                                required
                                            />
                                        </div>

                                        <div className="col-md-6">
                                            <label className="form-label">
                                                Referencia o comprobante *
                                            </label>
                                            <input
                                                className="form-control"
                                                maxLength={100}
                                                value={reference}
                                                onChange={e =>
                                                    setReference(e.target.value)
                                                }
                                                required
                                            />
                                        </div>

                                        <div className="col-md-6">
                                            <label className="form-label">
                                                Observaciones
                                            </label>
                                            <input
                                                className="form-control"
                                                maxLength={500}
                                                value={notes}
                                                onChange={e =>
                                                    setNotes(e.target.value)
                                                }
                                            />
                                        </div>
                                    </div>

                                    <button
                                        type="submit"
                                        className="btn btn-primary mt-3"
                                        disabled={saving}
                                    >
                                        {saving
                                            ? "Guardando..."
                                            : "Registrar pago anterior"}
                                    </button>
                                </form>
                            </>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}
