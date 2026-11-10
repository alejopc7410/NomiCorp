
import { useState } from 'react';
import { useAuth } from './context/AuthContext';

export default function Login() {
    const { login, loading } = useAuth();

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');

    async function handleSubmit(event) {
        event.preventDefault();
        setError('');

        try {
            await login(email, password);
        } catch (err) {
            setError(err.message || 'No se pudo iniciar sesión.');
        }
    }

    return (
        <div className="min-vh-100 d-flex align-items-center justify-content-center login-bg-img">
            <div className="card shadow-sm border-0 p-4"
                style={{ width: '100%', maxWidth: '420px' }}>

                <div className="text-center">
                    <img
                        src="/src/media/logo.png"
                        alt="NomiCorp"
                        width="200"
                        height="200"
                    />
                </div>

                <h5 className="mb-3">Iniciar sesión</h5>

                {error && (
                    <div className="alert alert-danger">
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit}>
                    <div className="mb-3">
                        <label className="form-label">
                            Correo electrónico
                        </label>

                        <input
                            type="email"
                            className="form-control"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                            autoComplete="username"
                        />
                    </div>

                    <div className="mb-3">
                        <label className="form-label">
                            Contraseña
                        </label>

                        <input
                            type="password"
                            className="form-control"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            required
                            autoComplete="current-password"
                        />
                    </div>

                    <button
                        type="submit"
                        className="btn btn-primary w-100"
                        disabled={loading}
                    >
                        {loading ? 'Ingresando...' : 'Ingresar'}
                    </button>
                </form>
            </div>
        </div>
    );
}
