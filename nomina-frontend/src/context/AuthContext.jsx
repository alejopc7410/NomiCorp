
import {
    createContext,
    useContext,
    useState,
} from 'react';

import {
    authApi,
    setAccessToken,
} from '../services/auth';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(false);

    async function login(email, password) {
        setLoading(true);

        try {
            const response = await authApi.login({
                email,
                password,
            });

            setAccessToken(response.token);

            setUser({
                email: response.email,
                roles: response.roles,
                employeeId: response.employeeId,
                mustChangePassword: response.mustChangePassword,
            });

            return response;
        } finally {
            setLoading(false);
        }
    }

    function logout() {
        setAccessToken(null);
        setUser(null);
    }

    async function changePassword(currentPassword, newPassword) {
        await authApi.changePassword({
            currentPassword,
            newPassword,
        });

        // El JWT anterior contiene el estado de contraseña anterior.
        // Cerramos la sesión para obligar a iniciar sesión nuevamente.
        logout();
    }

    const value = {
        user,
        loading,
        login,
        logout,
        changePassword,
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);

    if (!context) {
        throw new Error(
            'useAuth debe utilizarse dentro de AuthProvider.'
        );
    }

    return context;
}
