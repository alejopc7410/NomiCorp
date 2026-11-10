import { useEffect, useState } from 'react';
import { employeesApi } from './services/employees';
import { contractsApi } from './services/contracts';

const initialForm = {
    employeeId: '',
    contractType: 'Indefinido',
    position: '',
    startDate: '',
    endDate: '',
    monthlySalary: '',
};

const money = new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    maximumFractionDigits: 2,
});

export default function Contracts() {
    const [employees, setEmployees] = useState([]);
    const [contracts, setContracts] = useState([]);
    const [form, setForm] = useState(initialForm);
    const [salaryForm, setSalaryForm] = useState({
        contractId: null,
        monthlySalary: '',
        effectiveFrom: '',
        reason: '',
    });
    const [history, setHistory] = useState([]);
    const [historyContractId, setHistoryContractId] = useState(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [message, setMessage] = useState('');

    async function loadData() {
        setLoading(true);
        setError('');

        try {
            const [employeeData, contractData] = await Promise.all([
                employeesApi.getAll(true),
                contractsApi.getAll(),
            ]);

            setEmployees(employeeData);
            setContracts(contractData);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        loadData();
    }, []);

    function handleChange(event) {
        const { name, value } = event.target;

        setForm((previous) => ({
            ...previous,
            [name]: value,
        }));
    }

    async function handleCreate(event) {
        event.preventDefault();
        setError('');
        setMessage('');
        setSaving(true);

        try {
            await contractsApi.create({
                employeeId: Number(form.employeeId),
                contractType: form.contractType,
                position: form.position.trim(),
                startDate: form.startDate,
                endDate: form.endDate || null,
                monthlySalary: Number(form.monthlySalary),
                salaryEffectiveFrom: form.startDate,
            });

            setForm(initialForm);
            setMessage('Contrato registrado correctamente.');
            await loadData();
        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    async function openHistory(contractId) {
        setError('');
        setMessage('');
        setHistoryContractId(contractId);

        try {
            const data = await contractsApi.getSalaryHistory(contractId);
            setHistory(data);
        } catch (err) {
            setError(err.message);
        }
    }

    async function handleSalaryChange(event) {
        event.preventDefault();
        setError('');
        setMessage('');
        setSaving(true);

        try {
            await contractsApi.changeSalary(
                salaryForm.contractId,
                {
                    monthlySalary: Number(salaryForm.monthlySalary),
                    effectiveFrom: salaryForm.effectiveFrom,
                    reason: salaryForm.reason.trim() || null,
                }
            );

            setMessage('Cambio salarial registrado correctamente.');

            setSalaryForm({
                contractId: null,
                monthlySalary: '',
                effectiveFrom: '',
                reason: '',
            });

            await loadData();

            if (historyContractId === salaryForm.contractId) {
                await openHistory(salaryForm.contractId);
            }
        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    async function handleEndContract(contract) {
        const endDate = window.prompt(
            'Ingresa la fecha de terminación (AAAA-MM-DD):'
        );

        if (!endDate) return;

        if (!/^\d{4}-\d{2}-\d{2}$/.test(endDate)) {
            setError('Utiliza el formato AAAA-MM-DD.');
            return;
        }

        const confirmed = window.confirm(
            `¿Finalizar el contrato de ${contract.employeeName} el ${endDate}?`
        );

        if (!confirmed) return;

        setError('');
        setMessage('');

        try {
            await contractsApi.end(contract.id, endDate);
            setMessage('Contrato finalizado correctamente.');
            await loadData();
        } catch (err) {
            setError(err.message);
        }
    }

    return (
        <main className="container py-4 py-lg-5">
            <header className="mb-4">
                <p className="text-primary fw-semibold mb-1">
                    ADMINISTRACIÓN
                </p>
                <h1 className="h2 fw-bold">Contratos y salarios</h1>
                <p className="text-secondary">
                    Gestiona las condiciones laborales y el historial salarial.
                </p>
            </header>

            {error && (
                <div className="alert alert-danger" role="alert">
                    {error}
                </div>
            )}

            {message && (
                <div className="alert alert-success" role="status">
                    {message}
                </div>
            )}

            <section className="card border-0 shadow-sm mb-4">
                <div className="card-header bg-white py-3">
                    <h2 className="h5 mb-0">Registrar contrato</h2>
                </div>

                <div className="card-body">
                    <form onSubmit={handleCreate}>
                        <div className="row g-3">
                            <div className="col-12 col-md-6">
                                <label className="form-label">Empleado *</label>
                                <select
                                    className="form-select"
                                    name="employeeId"
                                    value={form.employeeId}
                                    onChange={handleChange}
                                    required
                                >
                                    <option value="">Selecciona un empleado</option>
                                    {employees.map((employee) => (
                                        <option key={employee.id} value={employee.id}>
                                            {employee.identification} — {employee.firstName}{' '}
                                            {employee.lastName}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">Tipo de contrato *</label>
                                <select
                                    className="form-select"
                                    name="contractType"
                                    value={form.contractType}
                                    onChange={handleChange}
                                    required
                                >
                                    <option value="Indefinido">Indefinido</option>
                                    <option value="Fijo">Término fijo</option>
                                    <option value="Obra o labor">Obra o labor</option>
                                </select>
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">Cargo *</label>
                                <input
                                    className="form-control"
                                    name="position"
                                    value={form.position}
                                    onChange={handleChange}
                                    maxLength={100}
                                    required
                                />
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">Salario mensual (COP) *</label>
                                <input
                                    className="form-control"
                                    type="number"
                                    name="monthlySalary"
                                    value={form.monthlySalary}
                                    onChange={handleChange}
                                    min="0.01"
                                    step="0.01"
                                    required
                                />
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">Fecha de inicio *</label>
                                <input
                                    className="form-control"
                                    type="date"
                                    name="startDate"
                                    value={form.startDate}
                                    onChange={handleChange}
                                    required
                                />
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">
                                    Fecha de finalización
                                </label>
                                <input
                                    className="form-control"
                                    type="date"
                                    name="endDate"
                                    value={form.endDate}
                                    onChange={handleChange}
                                    min={form.startDate || undefined}
                                />
                            </div>
                        </div>

                        <p className="form-text mt-3">
                            El salario inicial tendrá vigencia desde la fecha de inicio
                            del contrato. Los aumentos se registran después en el historial.
                        </p>

                        <button
                            className="btn btn-primary mt-2"
                            type="submit"
                            disabled={saving || employees.length === 0}
                        >
                            {saving ? 'Guardando...' : 'Registrar contrato'}
                        </button>

                        {employees.length === 0 && !loading && (
                            <p className="text-secondary small mt-2 mb-0">
                                Primero debes registrar un empleado activo.
                            </p>
                        )}
                    </form>
                </div>
            </section>

            <section className="card border-0 shadow-sm mb-4">
                <div className="card-header bg-white py-3">
                    <h2 className="h5 mb-0">Contratos registrados</h2>
                </div>

                {loading ? (
                    <p className="p-4 mb-0">Cargando contratos...</p>
                ) : (
                    <div className="table-responsive">
                        <table className="table table-hover align-middle mb-0">
                            <thead className="table-light">
                                <tr>
                                    <th>Empleado</th>
                                    <th>Cargo</th>
                                    <th>Tipo</th>
                                    <th>Salario vigente</th>
                                    <th>Estado</th>
                                    <th>Acciones</th>
                                </tr>
                            </thead>
                            <tbody>
                                {contracts.map((contract) => (
                                    <tr key={contract.id}>
                                        <td>
                                            <div className="fw-semibold">
                                                {contract.employeeName}
                                            </div>
                                            <small className="text-secondary">
                                                {contract.identification}
                                            </small>
                                        </td>
                                        <td>{contract.position}</td>
                                        <td>{contract.contractType}</td>
                                        <td>
                                            {contract.currentMonthlySalary == null
                                                ? 'Sin salario vigente hoy'
                                                : money.format(contract.currentMonthlySalary)}
                                        </td>
                                        <td>
                                            <span
                                                className={`badge ${contract.isActive
                                                        ? 'text-bg-success'
                                                        : 'text-bg-secondary'
                                                    }`}
                                            >
                                                {contract.isActive ? 'Activo' : 'Inactivo'}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="d-flex flex-wrap gap-2">
                                                <button
                                                    className="btn btn-sm btn-outline-primary"
                                                    onClick={() => openHistory(contract.id)}
                                                >
                                                    Historial
                                                </button>

                                                {contract.isActive && (
                                                    <>
                                                        <button
                                                            className="btn btn-sm btn-outline-success"
                                                            onClick={() => {
                                                                setSalaryForm({
                                                                    contractId: contract.id,
                                                                    monthlySalary: '',
                                                                    effectiveFrom: '',
                                                                    reason: '',
                                                                });
                                                            }}
                                                        >
                                                            Cambiar salario
                                                        </button>

                                                        <button
                                                            className="btn btn-sm btn-outline-danger"
                                                            onClick={() => handleEndContract(contract)}
                                                        >
                                                            Finalizar
                                                        </button>
                                                    </>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}

                                {contracts.length === 0 && (
                                    <tr>
                                        <td colSpan="6" className="text-center py-4">
                                            Todavía no hay contratos registrados.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                )}
            </section>

            {salaryForm.contractId !== null && (
                <section className="card border-0 shadow-sm mb-4">
                    <div className="card-header bg-white py-3">
                        <h2 className="h5 mb-0">Registrar cambio salarial</h2>
                    </div>

                    <div className="card-body">
                        <form onSubmit={handleSalaryChange}>
                            <div className="row g-3">
                                <div className="col-12 col-md-4">
                                    <label className="form-label">
                                        Nuevo salario mensual (COP)
                                    </label>
                                    <input
                                        className="form-control"
                                        type="number"
                                        min="0.01"
                                        step="0.01"
                                        value={salaryForm.monthlySalary}
                                        onChange={(event) =>
                                            setSalaryForm({
                                                ...salaryForm,
                                                monthlySalary: event.target.value,
                                            })
                                        }
                                        required
                                    />
                                </div>

                                <div className="col-12 col-md-4">
                                    <label className="form-label">Vigente desde</label>
                                    <input
                                        className="form-control"
                                        type="date"
                                        value={salaryForm.effectiveFrom}
                                        onChange={(event) =>
                                            setSalaryForm({
                                                ...salaryForm,
                                                effectiveFrom: event.target.value,
                                            })
                                        }
                                        required
                                    />
                                </div>

                                <div className="col-12 col-md-4">
                                    <label className="form-label">Motivo</label>
                                    <input
                                        className="form-control"
                                        maxLength={250}
                                        value={salaryForm.reason}
                                        onChange={(event) =>
                                            setSalaryForm({
                                                ...salaryForm,
                                                reason: event.target.value,
                                            })
                                        }
                                        placeholder="Ej. Aumento salarial"
                                    />
                                </div>
                            </div>

                            <div className="d-flex gap-2 mt-3">
                                <button className="btn btn-primary" disabled={saving}>
                                    {saving ? 'Guardando...' : 'Guardar cambio'}
                                </button>
                                <button
                                    className="btn btn-outline-secondary"
                                    type="button"
                                    onClick={() =>
                                        setSalaryForm({
                                            contractId: null,
                                            monthlySalary: '',
                                            effectiveFrom: '',
                                            reason: '',
                                        })
                                    }
                                >
                                    Cancelar
                                </button>
                            </div>
                        </form>
                    </div>
                </section>
            )}

            {historyContractId !== null && (
                <section className="card border-0 shadow-sm mb-4">
                    <div className="card-header bg-white py-3 d-flex
            justify-content-between align-items-center">
                        <h2 className="h5 mb-0">Historial salarial</h2>
                        <button
                            className="btn btn-sm btn-outline-secondary"
                            onClick={() => {
                                setHistoryContractId(null);
                                setHistory([]);
                            }}
                        >
                            Cerrar
                        </button>
                    </div>

                    <div className="table-responsive">
                        <table className="table align-middle mb-0">
                            <thead className="table-light">
                                <tr>
                                    <th>Salario mensual</th>
                                    <th>Desde</th>
                                    <th>Hasta</th>
                                    <th>Motivo</th>
                                </tr>
                            </thead>
                            <tbody>
                                {history.map((item) => (
                                    <tr key={item.id}>
                                        <td>{money.format(item.monthlySalary)}</td>
                                        <td>{item.effectiveFrom}</td>
                                        <td>{item.effectiveTo || 'Sin fecha final'}</td>
                                        <td>{item.reason || '—'}</td>
                                    </tr>
                                ))}

                                {history.length === 0 && (
                                    <tr>
                                        <td colSpan="4" className="text-center py-3">
                                            No hay registros salariales.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </section>
            )}
        </main>
    );
}