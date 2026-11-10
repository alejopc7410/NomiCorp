import { getAccessToken } from './auth';


const API_URL = import.meta.env.VITE_API_URL;

async function request(path, options = {}) {
    const response = await fetch(`${API_URL}/api/payroll-novelties${path}`, {
        headers: {
            'Content-Type': 'application/json',
            ...(getAccessToken()
                ? { Authorization: `Bearer ${getAccessToken()}` }
                : {}),
            ...options.headers,
            /*'Content-Type': 'application/json',
            ...(token ? {
                Authorization: `Bearer ${token}`
            } : {}),
            ...options.headers,*/
        },
        ...options,
    });

    if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Error al procesar la novedad.');
    }

    if (response.status === 204) return null;
    return response.json();
}

export const payrollNoveltiesApi = {
    getAll: (year, month) =>
        request(`?year=${year}&month=${month}`),

    create: (data) =>
        request('', {
            method: 'POST',
            body: JSON.stringify(data),
        }),

    update: (id, data) =>
        request(`/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        }),

    approve: (id) =>
        request(`/${id}/approve`, { method: 'PATCH' }),

    cancel: (id) =>
        request(`/${id}/cancel`, { method: 'PATCH' }),
};
