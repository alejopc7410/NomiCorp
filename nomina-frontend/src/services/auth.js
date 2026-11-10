
const API_URL = (
    import.meta.env.VITE_API_URL
).replace(/\/$/, '');

// El token permanece en memoria y no se guarda en localStorage.
let accessToken = null;

export function setAccessToken(token) {
    accessToken = token;
}

export function getAccessToken() {
    return accessToken;
}

async function request(path, options = {}) {
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers,
    };

    if (accessToken) {
        headers.Authorization = `Bearer ${accessToken}`;
    }

    const response = await fetch(`${API_URL}${path}`, {
        ...options,
        headers,
    });

    if (!response.ok) {
        let message = `Error HTTP ${response.status}`;

        try {
            const data = await response.json();

            message =
                data.message ||
                data.title ||
                data.errors?.join(', ') ||
                message;
        } catch {
            // La respuesta puede no contener JSON.
        }

        throw new Error(message);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

export const authApi = {
    login: (credentials) =>
        request('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify(credentials),
        }),

    changePassword: (passwords) =>
        request('/api/auth/change-password', {
            method: 'POST',
            body: JSON.stringify(passwords),
        }),
};
