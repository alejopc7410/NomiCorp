
import { useCallback, useEffect, useState } from 'react';
import { payrollNoveltiesApi } from './services/payrollNovelties';
import { contractsApi } from './services/contracts';

const today = new Date();
const currentYear = today.getFullYear();
const currentMonth = String(today.getMonth() + 1).padStart(2, '0');

const emptyForm = () => ({
    contractId: '',
    periodYear: currentYear,
    periodMonth: Number(currentMonth),
    noveltyDate: `${currentYear}-${currentMonth}-${String(today.getDate()).padStart(2, '0')}`,
    noveltyType: 'Bonificacion',
    category: 'Devengado',
    isSalaryConstitutive: '',
    description: '',
    quantity: '',
    unitValue: '',
    amount: '',
    notes: '',
});

const types = [
    'Horas extra',
    'Recargo nocturno',
    'Recargo dominical/festivo',
    'Bonificacion',
    'Comision',
    'Auxilio',
    'Incapacidad',
    'Licencia',
    'Ausencia',
    'Deduccion',
    'Otro',
];

const categories = ['Devengado', 'Deduccion', 'Informativa'];

const money = (value) =>
    new Intl.NumberFormat('es-CO', {
        style: 'currency',
        currency: 'COP',
        maximumFractionDigits: 0,
    }).format(value ?? 0);

export default function PayrollNovelties() {
    const [contracts, setContracts] = useState([]);
    const [novelties, setNovelties] = useState([]);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [filterYear, setFilterYear] = useState(currentYear);
    const [filterMonth, setFilterMonth] = useState(Number(currentMonth));
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [message, setMessage] = useState('');

    const loadData = useCallback(async () => {
        setLoading(true);
        setError('');

        try {
            const [contractData, noveltyData] = await Promise.all([
                contractsApi.getAll(),
                payrollNoveltiesApi.getAll(filterYear, filterMonth),
            ]);

            setContracts(contractData.filter((c) => c.isActive));
            setNovelties(noveltyData);
        } catch (err) {
            setError(err.message || 'No fue posible cargar la información.');
        } finally {
            setLoading(false);
        }
    }, [filterYear, filterMonth]);

    useEffect(() => {
        loadData();
    }, [loadData]);

    function changeField(event) {
        const { name, value } = event.target;
        setForm((previous) => {
            const next = { ...previous, [name]: value };

            if (name === 'noveltyType') {
                if (value === 'Deduccion') next.category = 'Deduccion';
                else if (value === 'Incapacidad' || value === 'Licencia' || value === 'Ausencia') {
                    next.category = 'Informativa';
                } else {
                    next.category = 'Devengado';
                }
            }

            if (next.category !== 'Devengado') {
                next.isSalaryConstitutive = '';
            }

            return next;
        });
    }

    function resetForm() {
        setForm(emptyForm());
        setEditingId(null);
    }

    async function submit(event) {
        event.preventDefault();
        setSaving(true);
        setError('');
        setMessage('');

        const payload = {
            ...form,
            contractId: Number(form.contractId),
            periodYear: Number(form.periodYear),
            periodMonth: Number(form.periodMonth),
            quantity: form.quantity === '' ? null : Number(form.quantity),
            unitValue: form.unitValue === '' ? null : Number(form.unitValue),
            amount: Number(form.amount),
            isSalaryConstitutive: form.category === 'Devengado'
                ? (form.isSalaryConstitutive === 'true' ? true
                    : form.isSalaryConstitutive === 'false' ? false : null)
                : null,
        };

        try {
            if (editingId) {
                await payrollNoveltiesApi.update(editingId, payload);
                setMessage('Novedad actualizada correctamente.');
            } else {
                await payrollNoveltiesApi.create(payload);
                setMessage('Novedad registrada correctamente.');
            }

            resetForm();
            await loadData();
        } catch (err) {
            setError(err.message || 'No fue posible guardar la novedad.');
        } finally {
            setSaving(false);
        }
    }

    function editNovelty(novelty) {
        setEditingId(novelty.id);
        setForm({
            contractId: String(novelty.contractId),
            periodYear: novelty.periodYear,
            periodMonth: novelty.periodMonth,
            noveltyDate: novelty.noveltyDate,
            noveltyType: novelty.noveltyType,
            category: novelty.category,
            isSalaryConstitutive: novelty.isSalaryConstitutive == null
                ? '' : String(novelty.isSalaryConstitutive),
            description: novelty.description,
            quantity: novelty.quantity ?? '',
            unitValue: novelty.unitValue ?? '',
            amount: novelty.amount,
            notes: novelty.notes ?? '',
        });
        setError('');
        setMessage('');
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    async function changeStatus(id, action) {
        const verb = action === 'approve' ? 'aprobar' : 'anular';

        if (!window.confirm(`¿Deseas ${verb} esta novedad?`)) return;

        setError('');
        setMessage('');

        try {
            await payrollNoveltiesApi[action](id);
            setMessage(`Novedad ${action === 'approve' ? 'aprobada' : 'anulada'}.`);
            await loadData();
        } catch (err) {
            setError(err.message || 'No fue posible actualizar la novedad.');
        }
    }

    const totalDevengado = novelties
        .filter((n) => n.category === 'Devengado' && n.status === 'Aprobada')
        .reduce((sum, n) => sum + n.amount, 0);

    const totalDeducciones = novelties
        .filter((n) => n.category === 'Deduccion' && n.status === 'Aprobada')
        .reduce((sum, n) => sum + n.amount, 0);

    return (
        <main className="container py-4">
            <div className="mb-4">
                <h2 className="fw-bold">Novedades de nómina</h2>
                <p className="text-secondary mb-0">
                    Registra y administra las novedades de cada período.
                </p>
            </div>

            {error && <div className="alert alert-danger">{error}</div>}
            {message && <div className="alert alert-success">{message}</div>}

            <div className="row g-3 mb-4">
                <div className="col-md-6">
                    <div className="card border-0 shadow-sm">
                        <div className="card-body">
                            <div className="text-secondary">Devengados aprobados</div>
                            <div className="fs-4 fw-bold">{money(totalDevengado)}</div>
                        </div>
                    </div>
                </div>
                <div className="col-md-6">
                    <div className="card border-0 shadow-sm">
                        <div className="card-body">
                            <div className="text-secondary">Deducciones aprobadas</div>
                            <div className="fs-4 fw-bold">{money(totalDeducciones)}</div>
                        </div>
                    </div>
                </div>
            </div>

            <section className="card border-0 shadow-sm mb-4">
                <div className="card-header bg-white py-3 fw-semibold">
                    {editingId ? 'Editar novedad' : 'Registrar novedad'}
                </div>
                <div className="card-body">
                    <form onSubmit={submit}>
                        <div className="row g-3">
                            <div className="col-md-6">
                                <label className="form-label">Contrato y empleado *</label>
                                <select
                                    className="form-select"
                                    name="contractId"
                                    value={form.contractId}
                                    onChange={changeField}
                                    required
                                    disabled={Boolean(editingId)}
                                >
                                    <option value="">Selecciona un contrato</option>
                                    {contracts.map((c) => (
                                        <option key={c.id} value={c.id}>
                                            {c.employeeName || `Empleado #${c.employeeId}`} — {c.position}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="col-md-3">
                                <label className="form-label">Año *</label>
                                <input
                                    className="form-control"
                                    type="number"
                                    name="periodYear"
                                    min="2020"
                                    max="2100"
                                    value={form.periodYear}
                                    onChange={changeField}
                                    required
                                />
                            </div>

                            <div className="col-md-3">
                                <label className="form-label">Mes *</label>
                                <select
                                    className="form-select"
                                    name="periodMonth"
                                    value={form.periodMonth}
                                    onChange={changeField}
                                    required
                                >
                                    {Array.from({ length: 12 }, (_, i) => (
                                        <option key={i + 1} value={i + 1}>
                                            {new Date(2000, i, 1).toLocaleString('es-CO', { month: 'long' })}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="col-md-4">
                                <label className="form-label">Fecha de la novedad *</label>
                                <input
                                    className="form-control"
                                    type="date"
                                    name="noveltyDate"
                                    value={form.noveltyDate}
                                    onChange={changeField}
                                    required
                                />
                            </div>

                            <div className="col-md-4">
                                <label className="form-label">Tipo *</label>
                                <select
                                    className="form-select"
                                    name="noveltyType"
                                    value={form.noveltyType}
                                    onChange={changeField}
                                >
                                    {types.map((type) => (
                                        <option key={type}>{type}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="col-md-4">
                                <label className="form-label">Categoría *</label>
                                <select
                                    className="form-select"
                                    name="category"
                                    value={form.category}
                                    onChange={changeField}
                                    required
                                >
                                    {categories.map((category) => (
                                        <option key={category}>{category}</option>
                                    ))}
                                </select>
                            </div>

                            {form.category === 'Devengado' && (
                                <div className="col-md-6">
                                    <label className="form-label">¿Constituye salario? *</label>
                                    <select
                                        className="form-select"
                                        name="isSalaryConstitutive"
                                        value={form.isSalaryConstitutive}
                                        onChange={changeField}
                                        required
                                    >
                                        <option value="">Selecciona una opción</option>
                                        <option value="true">Sí, constituye salario</option>
                                        <option value="false">No constituye salario</option>
                                    </select>
                                    <small className="text-secondary">
                                        La clasificación debe reflejar la naturaleza legal del pago.
                                    </small>
                                </div>
                            )}

                            <div className="col-md-6">
                                <label className="form-label">Descripción *</label>
                                <input
                                    className="form-control"
                                    name="description"
                                    maxLength="200"
                                    value={form.description}
                                    onChange={changeField}
                                    placeholder="Ej. Bonificación por cumplimiento"
                                    required
                                />
                            </div>

                            <div className="col-md-3">
                                <label className="form-label">Cantidad</label>
                                <input
                                    className="form-control"
                                    type="number"
                                    name="quantity"
                                    min="0.0001"
                                    step="0.0001"
                                    value={form.quantity}
                                    onChange={changeField}
                                />
                            </div>

                            <div className="col-md-3">
                                <label className="form-label">Valor unitario (COP)</label>
                                <input
                                    className="form-control"
                                    type="number"
                                    name="unitValue"
                                    min="0.01"
                                    step="0.01"
                                    value={form.unitValue}
                                    onChange={changeField}
                                />
                            </div>

                            <div className="col-md-4">
                                <label className="form-label">Valor total (COP) *</label>
                                <input
                                    className="form-control"
                                    type="number"
                                    name="amount"
                                    min="0.01"
                                    step="0.01"
                                    value={form.amount}
                                    onChange={changeField}
                                    required
                                />
                            </div>

                            <div className="col-md-8">
                                <label className="form-label">Observaciones</label>
                                <input
                                    className="form-control"
                                    name="notes"
                                    maxLength="500"
                                    value={form.notes}
                                    onChange={changeField}
                                />
                            </div>
                        </div>

                        <div className="d-flex gap-2 mt-4">
                            <button className="btn btn-primary" type="submit" disabled={saving}>
                                {saving ? 'Guardando...' : editingId ? 'Guardar cambios' : 'Registrar novedad'}
                            </button>
                            {editingId && (
                                <button
                                    className="btn btn-outline-secondary"
                                    type="button"
                                    onClick={resetForm}
                                >
                                    Cancelar edición
                                </button>
                            )}
                        </div>
                    </form>
                </div>
            </section>

            <section className="card border-0 shadow-sm">
                <div className="card-header bg-white py-3">
                    <div className="row g-2 align-items-center">
                        <div className="col">
                            <span className="fw-semibold">Novedades registradas</span>
                        </div>
                        <div className="col-auto">
                            <input
                                aria-label="Año del filtro"
                                className="form-control"
                                type="number"
                                min="2020"
                                max="2100"
                                value={filterYear}
                                onChange={(e) => setFilterYear(Number(e.target.value))}
                            />
                        </div>
                        <div className="col-auto">
                            <select
                                aria-label="Mes del filtro"
                                className="form-select"
                                value={filterMonth}
                                onChange={(e) => setFilterMonth(Number(e.target.value))}
                            >
                                {Array.from({ length: 12 }, (_, i) => (
                                    <option key={i + 1} value={i + 1}>
                                        {new Date(2000, i, 1).toLocaleString('es-CO', { month: 'long' })}
                                    </option>
                                ))}
                            </select>
                        </div>
                    </div>
                </div>

                <div className="table-responsive">
                    <table className="table table-hover align-middle mb-0">
                        <thead className="table-light">
                            <tr>
                                <th>Empleado</th>
                                <th>Fecha</th>
                                <th>Tipo / descripción</th>
                                <th>Categoría</th>
                                <th>¿Es salarial?</th>
                                <th>Valor</th>
                                <th>Estado</th>
                                <th>Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            {loading ? (
                                <tr><td colSpan="8" className="text-center py-4">Cargando...</td></tr>
                            ) : novelties.length === 0 ? (
                                <tr><td colSpan="8" className="text-center text-secondary py-4">
                                    No hay novedades para este período.
                                </td></tr>
                            ) : novelties.map((n) => (
                                <tr key={n.id}>
                                    <td>
                                        <div className="fw-semibold">{n.employeeName}</div>
                                        <small className="text-secondary">{n.identification}</small>
                                    </td>
                                    <td>{n.noveltyDate}</td>
                                    <td>
                                        <div>{n.noveltyType}</div>
                                        <small className="text-secondary">{n.description}</small>
                                    </td>
                                    <td>{n.category}</td>
                                    <td>
                                        {n.category !== 'Devengado' ? 'No aplica'
                                            : n.isSalaryConstitutive === true ? 'Sí'
                                            : n.isSalaryConstitutive === false ? 'No'
                                            : 'Pendiente'}
                                    </td>
                                    <td className="text-nowrap">{money(n.amount)}</td>
                                    <td>
                                        <span className={`badge ${n.status === 'Aprobada' ? 'text-bg-success' :
                                                n.status === 'Anulada' ? 'text-bg-secondary' :
                                                    'text-bg-warning'
                                            }`}>
                                            {n.status}
                                        </span>
                                    </td>
                                    <td>
                                        <div className="d-flex gap-1 flex-wrap">
                                            {n.status === 'Pendiente' && (
                                                <>
                                                    <button
                                                        className="btn btn-sm btn-outline-primary"
                                                        onClick={() => editNovelty(n)}
                                                    >
                                                        Editar
                                                    </button>
                                                    <button
                                                        className="btn btn-sm btn-outline-success"
                                                        onClick={() => changeStatus(n.id, 'approve')}
                                                    >
                                                        Aprobar
                                                    </button>
                                                </>
                                            )}
                                            {n.status !== 'Anulada' && (
                                                <button
                                                    className="btn btn-sm btn-outline-danger"
                                                    onClick={() => changeStatus(n.id, 'cancel')}
                                                >
                                                    Anular
                                                </button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </section>
        </main>
    );
}
