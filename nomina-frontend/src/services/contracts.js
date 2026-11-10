import { getAccessToken } from './auth';

const API_URL = (
    import.meta.env.VITE_API_URL
).replace(/\/$/, '');

async function request(path, options = {}) {
    const response = await fetch(`${API_URL}${path}`, {
        ...options,
        headers: {
            /*'Content-Type': 'application/json',
            ...(token ? {
                Authorization: `Bearer ${token}`
            } : {}),
            ...options.headers,*/
            'Content-Type': 'application/json',
            ...(getAccessToken()
                ? { Authorization: `Bearer ${getAccessToken()}` }
                : {}),
            ...options.headers,
        },
    });

    if (!response.ok) {
        let message = `Error HTTP ${response.status}`;

        try {
            const data = await response.json();
            message = data.message || data.title || message;
        } catch {
            // Puede que el servidor no devuelva JSON.
        }

        throw new Error(message);
    }

    if (response.status === 204) return null;

    return response.json();
}

export const contractsApi = {
    getAll: () => request('/api/contracts'),

    create: (contract) =>
        request('/api/contracts', {
            method: 'POST',
            body: JSON.stringify(contract),
        }),

    getSalaryHistory: (id) =>
        request(`/api/contracts/${id}/salary-history`),

    changeSalary: (id, data) =>
        request(`/api/contracts/${id}/salary-history`, {
            method: 'POST',
            body: JSON.stringify(data),
        }),

    end: (id, endDate) =>
        request(
            `/api/contracts/${id}/end?endDate=${encodeURIComponent(endDate)}`,
            { method: 'PATCH' }
        ),
};