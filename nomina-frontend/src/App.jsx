
import { useState } from 'react';
import EmployeePortal from './EmployeePortal';
import Employees from './Employees';
import Contracts from './Contracts';
import PayrollNovelties from './PayrollNovelties';
import Login from './Login';
import ChangePassword from './ChangePassword';
import { AuthProvider, useAuth } from './context/AuthContext';
import Payroll from "./Payroll";
import Bonuses from "./Bonuses";

function Application() {
    const { user, logout } = useAuth();

    const [module, setModule] = useState('employees');

    // Si no hay usuario autenticado, mostrar login.
    if (!user) {
        return <Login />;
    }

    // Si debe cambiar su contraseña, impedir el acceso a los módulos.
    if (user.mustChangePassword) {
        return <ChangePassword />;
    }

    const isAdmin = user.roles.includes('Administrador');
    const isEmployee = user.roles.includes('Empleado');

    if (
        user.roles.includes('Empleado') &&
        !user.roles.includes('Administrador')
    ) {
        return <EmployeePortal />;
    }
    // Por ahora, el portal del empleado será una pantalla provisional.
    /*if (isEmployee && !isAdmin) {
        return (
            <div className="container py-5">
                <div className="d-flex justify-content-between align-items-center mb-4">
                    <h3>Portal del empleado</h3>

                    <button
                        className="btn btn-outline-danger"
                        onClick={logout}
                    >
                        Cerrar sesión
                    </button>
                </div>

                <div className="card p-4 shadow-sm">
                    <h5>Bienvenido</h5>

                    <p className="text-muted mb-1">
                        {user.email}
                    </p>

                    <p>
                        Estamos preparando tu portal de nómina.
                        Próximamente podrás consultar tus desprendibles.
                    </p>
                </div>
            </div>
        );
    }*/

    if (!isAdmin) {
        return (
            <div className="container py-5">
                <h4>Tu cuenta no tiene permisos para acceder a este sistema.</h4>
                <button className="btn btn-outline-danger mt-3" onClick={logout}>
                    Cerrar sesión
                </button>
            </div>
        );
    }

    const buttonClass = (name) =>
        `btn ${module === name ? 'btn-primary' : 'btn-outline-secondary'}`;

    return (
        <>
            <nav className="navbar bg-white border-bottom">
                <div className="container flex-wrap gap-3 py-2">
                    <span className="navbar-brand fw-bold">
                        <img
                            src="/src/media/logo.png"
                            alt="NomiCorp"
                            width="100"
                            height="100"
                        />
                    </span>

                    <div className="d-flex gap-2 flex-wrap">
                        <button
                            className={buttonClass('employees')}
                            onClick={() => setModule('employees')}
                        >
                            Empleados
                        </button>

                        <button
                            className={buttonClass('contracts')}
                            onClick={() => setModule('contracts')}
                        >
                            Contratos y salarios
                        </button>

                        <button
                            className={buttonClass('novelties')}
                            onClick={() => setModule('novelties')}
                        >
                            Novedades de nómina
                        </button>

                        <button
                            className={buttonClass('payroll')}
                            onClick={() => setModule('payroll')}
                        >
                            Nómina
                        </button>

                        <button
                            className="btn btn-outline-primary"
                            onClick={() => setModule("bonuses")}
                        >
                            Prima de servicios
                        </button>
                    </div>

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

            {module === 'employees' && <Employees />}
            {module === 'contracts' && <Contracts />}
            {module === 'novelties' && <PayrollNovelties />}
            {module === 'payroll' && <Payroll />}
            {module === "bonuses" && <Bonuses />}
        </>
    );
}

export default function App() {
    return (
        <AuthProvider>
            <Application />
        </AuthProvider>
    );
}




/*import { useState } from 'react';
import Employees from './Employees';
import Contracts from './Contracts';
import PayrollNovelties from './PayrollNovelties';

export default function App() {
    const [module, setModule] = useState('employees');

    const buttonClass = (name) =>
        `btn ${module === name ? 'btn-primary' : 'btn-outline-secondary'}`;

    return (
        <>
            <nav className="navbar bg-white border-bottom">
                <div className="container flex-wrap gap-3 py-2">
                    <span className="navbar-brand fw-bold"><img src="src/media/logo.png" alt="NomiCorp" width="100" height="100"></img></span>

                    <div className="d-flex gap-2 flex-wrap">
                        <button
                            className={buttonClass('employees')}
                            onClick={() => setModule('employees')}
                        >
                            Empleados
                        </button>

                        <button
                            className={buttonClass('contracts')}
                            onClick={() => setModule('contracts')}
                        >
                            Contratos y salarios
                        </button>

                        <button
                            className={buttonClass('novelties')}
                            onClick={() => setModule('novelties')}
                        >
                            Novedades de nómina
                        </button>
                    </div>
                </div>
            </nav>

            {module === 'employees' && <Employees />}
            {module === 'contracts' && <Contracts />}
            {module === 'novelties' && <PayrollNovelties />}
        </>
    );
}*/
