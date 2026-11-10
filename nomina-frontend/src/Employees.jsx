import { useEffect, useMemo, useState } from 'react';
import { employeesApi } from './services/employees';
import { usersApi } from "./services/users";

const emptyForm = {
    identification: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    birthDate: '',
    hireDate: '',
};

export default function App() {
    const [employees, setEmployees] = useState([]);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [search, setSearch] = useState('');
    const [showInactive, setShowInactive] = useState(false);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [message, setMessage] = useState('');
    const [credentials, setCredentials] = useState(null);
    const [copied, setCopied] = useState(false);

    async function loadEmployees() {
        setLoading(true);
        setError('');

        try {
            const data = await employeesApi.getAll();
            setEmployees(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        loadEmployees();
    }, []);

    const visibleEmployees = useMemo(() => {
        const term = search.trim().toLowerCase();

        return employees.filter((employee) => {
            const matchesStatus = showInactive || employee.isActive;
            const fullName =
                `${employee.firstName} ${employee.lastName}`.toLowerCase();

            const matchesSearch =
                !term ||
                fullName.includes(term) ||
                employee.identification.toLowerCase().includes(term) ||
                (employee.email || '').toLowerCase().includes(term);

            return matchesStatus && matchesSearch;
        });
    }, [employees, search, showInactive]);

    function handleChange(event) {
        const { name, value } = event.target;

        setForm((previous) => ({
            ...previous,
            [name]: value,
        }));
    }

    function resetForm() {
        setForm(emptyForm);
        setEditingId(null);
    }

    async function handleSubmit(event) {
        event.preventDefault();

        setError("");
        setMessage("");
        setSaving(true);

        try {
            const payload = {
                ...form,
                identification: form.identification.trim(),
                firstName: form.firstName.trim(),
                lastName: form.lastName.trim(),
                email: form.email.trim() || null,
                phone: form.phone.trim() || null,
                birthDate: form.birthDate || null,
                hireDate: form.hireDate || null,
            };

            if (editingId === null) {

                // 1. Crear el empleado.
                const employee = await employeesApi.create(payload);

                // 2. Crear su cuenta de acceso.
                if (!payload.email) {
                    resetForm();
                    await loadEmployees();

                    setMessage(
                        `Empleado registrado correctamente (ID ${employee.id}), pero no se creó una cuenta porque no se proporcionó un correo electrónico.`
                    );

                    return;
                }

                try {
                    const account = await usersApi.createEmployee({
                        employeeId: employee.id,
                        email: payload.email
                    });

                    // 3. Guardar las credenciales para mostrarlas.
                    setCredentials({
                        employeeName: `${payload.firstName} ${payload.lastName}`,
                        email: account.email,
                        password: account.temporaryPassword
                    });

                    setCopied(false);

                    setMessage("Empleado y cuenta registrados correctamente.");

                } catch (accountError) {
                    setMessage(
                        `El empleado fue registrado (ID ${employee.id}), pero no se pudo crear su cuenta. No vuelvas a registrar al empleado.`
                    );

                    throw new Error(
                        `Error al crear la cuenta del empleado: ${accountError.message}`
                    );
                }

            } else {

                // Actualizar empleado existente.
                await employeesApi.update(editingId, payload);

                setMessage("Empleado actualizado correctamente.");
            }

            resetForm();
            await loadEmployees();

        } catch (err) {
            setError(err.message);
        } finally {
            setSaving(false);
        }
    }

    function startEditing(employee) {
        setEditingId(employee.id);

        setForm({
            identification: employee.identification,
            firstName: employee.firstName,
            lastName: employee.lastName,
            email: employee.email || '',
            phone: employee.phone || '',
            birthDate: employee.birthDate || '',
            hireDate: employee.hireDate || '',
        });

        setMessage('');
        setError('');
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    async function handleDeactivate(employee) {
        const confirmed = window.confirm(
            `¿Deseas desactivar a ${employee.firstName} ${employee.lastName}?`
        );

        if (!confirmed) return;

        setError('');
        setMessage('');

        try {
            await employeesApi.deactivate(employee.id);
            setMessage('Empleado desactivado correctamente.');

            if (editingId === employee.id) {
                resetForm();
            }

            await loadEmployees();
        } catch (err) {
            setError(err.message);
        }
    }

    const activeCount = employees.filter(
        (employee) => employee.isActive
    ).length;

    return (
        <main className="container py-4 py-lg-5">
            <header className="d-flex justify-content-between
        align-items-center flex-wrap gap-3 mb-4">
                <div>
                    <p className="text-primary fw-semibold mb-1">
                        ADMINISTRACIÓN
                    </p>
                    <h1 className="h2 fw-bold mb-1">
                        Gestión de empleados
                    </h1>
                    <p className="text-secondary mb-0">
                        Administra la información de tu equipo.
                    </p>
                </div>

                <button
                    className="btn btn-primary"
                    onClick={() => {
                        resetForm();
                        setError('');
                        setMessage('');
                    }}
                >
                    + Nuevo empleado
                </button>
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

            <section className="row g-3 mb-4">
                <div className="col-12 col-md-6">
                    <div className="card shadow-sm border-0">
                        <div className="card-body">
                            <p className="text-secondary mb-1">
                                Total registrados
                            </p>
                            <p className="display-6 fw-bold mb-0">
                                {employees.length}
                            </p>
                        </div>
                    </div>
                </div>

                <div className="col-12 col-md-6">
                    <div className="card shadow-sm border-0">
                        <div className="card-body">
                            <p className="text-secondary mb-1">
                                Empleados activos
                            </p>
                            <p className="display-6 fw-bold mb-0">
                                {activeCount}
                            </p>
                        </div>
                    </div>
                </div>
            </section>

            <section className="card shadow-sm border-0 mb-4">
                <div className="card-header bg-white py-3">
                    <h2 className="h5 mb-0">
                        {editingId === null
                            ? 'Registrar empleado'
                            : `Editar empleado #${editingId}`}
                    </h2>
                </div>

                <div className="card-body">
                    <form onSubmit={handleSubmit}>
                        <div className="row g-3">
                            <div className="col-12 col-md-4">
                                <label className="form-label">
                                    Identificación *
                                </label>
                                <input
                                    className="form-control"
                                    name="identification"
                                    value={form.identification}
                                    onChange={handleChange}
                                    maxLength={30}
                                    required
                                />
                            </div>

                            <div className="col-12 col-md-4">
                                <label className="form-label">Nombres *</label>
                                <input
                                    className="form-control"
                                    name="firstName"
                                    value={form.firstName}
                                    onChange={handleChange}
                                    maxLength={80}
                                    minLength={2}
                                    required
                                />
                            </div>

                            <div className="col-12 col-md-4">
                                <label className="form-label">Apellidos *</label>
                                <input
                                    className="form-control"
                                    name="lastName"
                                    value={form.lastName}
                                    onChange={handleChange}
                                    maxLength={80}
                                    minLength={2}
                                    required
                                />
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">Correo electrónico {editingId === null && "*"}</label>
                                <input
                                    className="form-control"
                                    type="email"
                                    name="email"
                                    value={form.email}
                                    onChange={handleChange}
                                    maxLength={150}
                                    required={editingId === null}
                                />
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">Teléfono</label>
                                <input
                                    className="form-control"
                                    name="phone"
                                    value={form.phone}
                                    onChange={handleChange}
                                    maxLength={30}
                                />
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">
                                    Fecha de nacimiento
                                </label>
                                <input
                                    className="form-control"
                                    type="date"
                                    name="birthDate"
                                    value={form.birthDate}
                                    onChange={handleChange}
                                />
                            </div>

                            <div className="col-12 col-md-6">
                                <label className="form-label">
                                    Fecha de ingreso
                                </label>
                                <input
                                    className="form-control"
                                    type="date"
                                    name="hireDate"
                                    value={form.hireDate}
                                    onChange={handleChange}
                                />
                            </div>
                        </div>

                        <div className="d-flex gap-2 mt-4">
                            <button
                                className="btn btn-primary"
                                type="submit"
                                disabled={saving}
                            >
                                {saving
                                    ? 'Guardando...'
                                    : editingId === null
                                        ? 'Registrar empleado'
                                        : 'Guardar cambios'}
                            </button>

                            {editingId !== null && (
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

            <section className="card shadow-sm border-0">
                <div className="card-header bg-white py-3">
                    <h2 className="h5 mb-0">Directorio de empleados</h2>
                </div>

                <div className="card-body border-bottom">
                    <div className="row g-3 align-items-center">
                        <div className="col-12 col-md-8">
                            <input
                                className="form-control"
                                placeholder="Buscar por nombre, identificación o correo..."
                                value={search}
                                onChange={(event) => setSearch(event.target.value)}
                            />
                        </div>

                        <div className="col-12 col-md-4">
                            <div className="form-check">
                                <input
                                    className="form-check-input"
                                    type="checkbox"
                                    id="showInactive"
                                    checked={showInactive}
                                    onChange={(event) =>
                                        setShowInactive(event.target.checked)
                                    }
                                />
                                <label
                                    className="form-check-label"
                                    htmlFor="showInactive"
                                >
                                    Incluir empleados inactivos
                                </label>
                            </div>
                        </div>
                    </div>
                </div>

                {loading ? (
                    <div className="p-4 text-secondary">
                        Cargando empleados...
                    </div>
                ) : (
                    <div className="table-responsive">
                        <table className="table table-hover align-middle mb-0">
                            <thead className="table-light">
                                <tr>
                                    <th>Identificación</th>
                                    <th>Empleado</th>
                                    <th>Correo</th>
                                    <th>Estado</th>
                                    <th>Acciones</th>
                                </tr>
                            </thead>

                            <tbody>
                                {visibleEmployees.map((employee) => (
                                    <tr key={employee.id}>
                                        <td>{employee.identification}</td>
                                        <td>
                                            <div className="fw-semibold">
                                                {employee.firstName} {employee.lastName}
                                            </div>
                                            <small className="text-secondary">
                                                {employee.phone || 'Sin teléfono'}
                                            </small>
                                        </td>
                                        <td>{employee.email || '—'}</td>
                                        <td>
                                            <span
                                                className={`badge ${employee.isActive
                                                        ? 'text-bg-success'
                                                        : 'text-bg-secondary'
                                                    }`}
                                            >
                                                {employee.isActive ? 'Activo' : 'Inactivo'}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="d-flex gap-2">
                                                <button
                                                    className="btn btn-sm btn-outline-primary"
                                                    onClick={() => startEditing(employee)}
                                                >
                                                    Editar
                                                </button>

                                                {employee.isActive && (
                                                    <button
                                                        className="btn btn-sm btn-outline-danger"
                                                        onClick={() => handleDeactivate(employee)}
                                                    >
                                                        Desactivar
                                                    </button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}

                                {visibleEmployees.length === 0 && (
                                    <tr>
                                        <td
                                            colSpan="5"
                                            className="text-center text-secondary py-4"
                                        >
                                            No se encontraron empleados.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                )}
            </section>

            <footer className="text-secondary small mt-4">
                Sistema de nómina · Módulo de empleados
            </footer>
            {credentials && (
                <div
                    className="modal d-block"
                    tabIndex="-1"
                    role="dialog"
                    aria-modal="true"
                    style={{
                        backgroundColor: "rgba(0,0,0,0.55)"
                    }}
                >
                    <div className="modal-dialog modal-dialog-centered">
                        <div className="modal-content border-0 shadow">

                            <div className="modal-header">
                                <h5 className="modal-title">
                                    <i className="bi bi-check-circle-fill text-success me-2"></i>
                                    ¡Empleado registrado!
                                </h5>

                                <button
                                    type="button"
                                    className="btn-close"
                                    onClick={() => setCredentials(null)}
                                    aria-label="Cerrar"
                                />
                            </div>

                            <div className="modal-body">

                                <div className="text-center mb-4">
                                    <div className="text-success fs-1">
                                        <i className="bi bi-person-check-fill"></i>
                                    </div>

                                    <h5>{credentials.employeeName}</h5>

                                    <p className="text-muted">
                                        La cuenta de acceso fue creada correctamente.
                                    </p>
                                </div>

                                <div className="mb-3">
                                    <label className="form-label fw-semibold">
                                        Correo electrónico
                                    </label>

                                    <div className="input-group">
                                        <input
                                            className="form-control"
                                            value={credentials.email}
                                            readOnly
                                        />

                                        <button
                                            className="btn btn-outline-secondary"
                                            type="button"
                                            onClick={() =>
                                                navigator.clipboard.writeText(
                                                    credentials.email
                                                )
                                            }
                                            title="Copiar correo"
                                        >
                                            
                                        </button>
                                    </div>
                                </div>

                                <div className="mb-3">
                                    <label className="form-label fw-semibold">
                                        Contraseña temporal
                                    </label>

                                    <div className="input-group">
                                        <input
                                            className="form-control"
                                            value={credentials.password}
                                            readOnly
                                        />

                                        <button
                                            className="btn btn-outline-secondary"
                                            type="button"
                                            onClick={() =>
                                                navigator.clipboard.writeText(
                                                    credentials.password
                                                )
                                            }
                                            title="Copiar contraseña"
                                        >
                                            <i className="bi bi-clipboard"></i>
                                        </button>
                                    </div>
                                </div>

                                <div className="alert alert-warning mb-0">
                                    <strong>Importante:</strong> el empleado deberá
                                    cambiar su contraseña cuando ingrese por primera vez.
                                    Guarda estas credenciales ahora; no volverán a
                                    mostrarse.
                                </div>

                            </div>

                            <div className="modal-footer">
                                <button
                                    className="btn btn-primary"
                                    onClick={() => {
                                        const text =
                                            `Acceso a NominaEmpresa\n` +
                                            `Empleado: ${credentials.employeeName}\n` +
                                            `Correo: ${credentials.email}\n` +
                                            `Contraseña temporal: ${credentials.password}\n` +
                                            `Al ingresar deberá cambiar su contraseña.`;

                                        navigator.clipboard.writeText(text);
                                        setCopied(true);
                                    }}
                                >
                                    <i className="bi bi-clipboard me-1"></i>
                                    {copied ? "Credenciales copiadas" : "Copiar credenciales"}
                                </button>

                                <button
                                    className="btn btn-secondary"
                                    onClick={() => setCredentials(null)}
                                >
                                    Cerrar
                                </button>
                            </div>

                        </div>
                    </div>
                </div>
            )}
        </main>
    );
}

/*import { useState } from 'react'
import heroImg from './assets/hero.png'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import './App.css'

function App() {
  const [count, setCount] = useState(0)

  return (
    <>
      <section id="center">
        <div className="hero">
          <img src={heroImg} className="base" width="170" height="179" alt="" />
          <img src={reactLogo} className="framework" alt="React logo" />
          <img src={viteLogo} className="vite" alt="Vite logo" />
        </div>
        <div>
          <h1>Get started</h1>
          <p>
            Edit <code>src/App.jsx</code> and save to test <code>HMR</code>
          </p>
        </div>
        <button
          type="button"
          className="counter"
          onClick={() => setCount((count) => count + 1)}
        >
          Count is {count}
        </button>
      </section>

      <div className="ticks"></div>

      <section id="next-steps">
        <div id="docs">
          <svg className="icon" role="presentation" aria-hidden="true">
            <use href="/icons.svg#documentation-icon"></use>
          </svg>
          <h2>Documentation</h2>
          <p>Your questions, answered</p>
          <ul>
            <li>
              <a href="https://vite.dev/" target="_blank">
                <img className="logo" src={viteLogo} alt="" />
                Explore Vite
              </a>
            </li>
            <li>
              <a href="https://react.dev/" target="_blank">
                <img className="button-icon" src={reactLogo} alt="" />
                Learn more
              </a>
            </li>
          </ul>
        </div>
        <div id="social">
          <svg className="icon" role="presentation" aria-hidden="true">
            <use href="/icons.svg#social-icon"></use>
          </svg>
          <h2>Connect with us</h2>
          <p>Join the Vite community</p>
          <ul>
            <li>
              <a href="https://github.com/vitejs/vite" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#github-icon"></use>
                </svg>
                GitHub
              </a>
            </li>
            <li>
              <a href="https://chat.vite.dev/" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#discord-icon"></use>
                </svg>
                Discord
              </a>
            </li>
            <li>
              <a href="https://x.com/vite_js" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#x-icon"></use>
                </svg>
                X.com
              </a>
            </li>
            <li>
              <a href="https://bsky.app/profile/vite.dev" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#bluesky-icon"></use>
                </svg>
                Bluesky
              </a>
            </li>
          </ul>
        </div>
      </section>

      <div className="ticks"></div>
      <section id="spacer"></section>
    </>
  )
}

export default App
*/