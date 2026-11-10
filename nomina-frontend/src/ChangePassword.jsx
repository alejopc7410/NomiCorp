
import { useState } from 'react';
import { useAuth } from './context/AuthContext';

export default function ChangePassword() {
    const { changePassword, user } = useAuth();

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');

    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleSubmit(event) {
        event.preventDefault();

        setError('');
        setSuccess('');

        if (newPassword !== confirmPassword) {
            setError('Las contraseñas nuevas no coinciden.');
            return;
        }

        if (newPassword.length < 10) {
            setError('La contraseña debe tener al menos 10 caracteres.');
            return;
        }

        setLoading(true);

        try {
            await changePassword(currentPassword, newPassword);

            setSuccess(
                'Contraseña actualizada. Ahora puedes iniciar sesión con tu nueva contraseña.'
            );
        } catch (err) {
            setError(err.message || 'No se pudo cambiar la contraseña.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="container min-vh-100 d-flex align-items-center justify-content-center">
            <div className="card shadow-sm border-0 p-4"
                style={{ width: '100%', maxWidth: '450px' }}>

                <h4 className="fw-bold mb-2">
                    Cambiar contraseña
                </h4>

                <p className="text-muted">
                    Hola, {user?.email}. Debes actualizar tu contraseña
                    antes de continuar.
                </p>

                {error && (
                    <div className="alert alert-danger">
                        {error}
                    </div>
                )}

                {success && (
                    <div className="alert alert-success">
                        {success}
                    </div>
                )}

                {!success && (
                    <form onSubmit={handleSubmit}>
                        <div className="mb-3">
                            <label className="form-label">
                                Contraseña actual
                            </label>

                            <input
                                type="password"
                                className="form-control"
                                value={currentPassword}
                                onChange={(e) =>
                                    setCurrentPassword(e.target.value)
                                }
                                required
                            />
                        </div>

                        <div className="mb-3">
                            <label className="form-label">
                                Nueva contraseña
                            </label>

                            <input
                                type="password"
                                className="form-control"
                                value={newPassword}
                                onChange={(e) =>
                                    setNewPassword(e.target.value)
                                }
                                minLength={10}
                                required
                            />
                        </div>

                        <div className="mb-3">
                            <label className="form-label">
                                Confirmar nueva contraseña
                            </label>

                            <input
                                type="password"
                                className="form-control"
                                value={confirmPassword}
                                onChange={(e) =>
                                    setConfirmPassword(e.target.value)
                                }
                                minLength={10}
                                required
                            />
                        </div>

                        <button
                            type="submit"
                            className="btn btn-primary w-100"
                            disabled={loading}
                        >
                            {loading ? 'Actualizando...' : 'Cambiar contraseña'}
                        </button>
                    </form>
                )}

                {success && (
                    <button
                        className="btn btn-outline-primary w-100"
                        onClick={() => window.location.reload()}
                    >
                        Ir al inicio de sesión
                    </button>
                )}
            </div>
        </div>
    );
}
