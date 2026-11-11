import EmployeePayslips from "./EmployeePayslips";
import { useEffect, useState } from 'react';
import { getMyInformation } from './services/employeePortal';
import { useAuth } from './context/AuthContext';
import logo from './media/logo.png';

export default function EmployeePortal() {
    const [employee, setEmployee] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const {user, logout} = useAuth();

    useEffect(() => {
        async function loadInformation() {
            try {
                const data = await getMyInformation();
                setEmployee(data);
            } catch (err) {
                setError(err.message || 'No fue posible cargar la información.');
            } finally {
                setLoading(false);
            }
        }

        loadInformation();
    }, []);

    if (loading) {
        return (
            <div className="container mt-5 text-center">
                <div className="spinner-border" role="status" />
                <p className="mt-2">Cargando información...</p>
            </div>
        );
    }

    if (error) {
        return (
            <div className="container mt-5">
                <div className="alert alert-danger">{error}</div>
            </div>
        );
    }

    if (!employee) return null;

    const contract = employee.activeContract;

    return (
        <>
            <nav className="navbar bg-white border-bottom">
                    <div className="container flex-wrap gap-3 py-2">
                        <span className="navbar-brand fw-bold">
                            <img
                                src={logo}
                                alt="NomiCorp"
                                width="100"
                                height="100"
                            />
                        </span>

                        <div className="d-flex align-items-center gap-2">
                            <span className="text-muted small">
                                {user.email}
                            </span>

                            <button
                                className="btn btn-outline-danger btn-sm"
                                onClick={logout}
                            >
                                Salir
                            </button>
                        </div>
                    </div>
            </nav>
            <div className="container py-4">
                <div className="mb-4">
                    <h2>Mi portal</h2>
                    <p className="h2 fw-bold mb-1">
                        Bienvenido, {employee.firstName} {employee.lastName}
                    </p>
                </div>

                <div className="card shadow-sm mb-4">
                    <div className="card-header">
                        <h5 className="mb-0">Mis datos personales</h5>
                    </div>

                    <div className="card-body">
                        <div className="row g-3">
                            <div className="col-md-6">
                                <strong>Identificación</strong>
                                <p>{employee.identification}</p>
                            </div>

                            <div className="col-md-6">
                                <strong>Correo electrónico</strong>
                                <p>{employee.email}</p>
                            </div>

                            <div className="col-md-6">
                                <strong>Teléfono</strong>
                                <p>{employee.phone || 'No registrado'}</p>
                            </div>

                            <div className="col-md-6">
                                <strong>Fecha de ingreso</strong>
                                <p>
                                    {employee.hireDate
                                        ? new Date(employee.hireDate).toLocaleDateString('es-CO')
                                        : 'No registrada'}
                                </p>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="card shadow-sm">
                    <div className="card-header">
                        <h5 className="mb-0">Mi contrato actual</h5>
                    </div>

                    <div className="card-body">
                        {contract ? (
                            <div className="row g-3">
                                <div className="col-md-6">
                                    <strong>Cargo</strong>
                                    <p>{contract.position}</p>
                                </div>

                                <div className="col-md-6">
                                    <strong>Tipo de contrato</strong>
                                    <p>{contract.contractType}</p>
                                </div>

                                <div className="col-md-6">
                                    <strong>Fecha de inicio</strong>
                                    <p>
                                        {new Date(contract.startDate).toLocaleDateString('es-CO')}
                                    </p>
                                </div>

                                <div className="col-md-6">
                                    <strong>Salario mensual</strong>
                                    <p>
                                        {contract.monthlySalary != null
                                            ? contract.monthlySalary.toLocaleString('es-CO', {
                                                style: 'currency',
                                                currency: 'COP',
                                                maximumFractionDigits: 0
                                            })
                                            : 'No registrado'}
                                    </p>
                                </div>
                            </div>
                        ) : (
                            <p className="text-muted mb-0">
                                No tienes un contrato activo registrado.
                            </p>
                        )}
                    </div>
                </div>
                <EmployeePayslips />
            </div>
        </>        
    );
}
