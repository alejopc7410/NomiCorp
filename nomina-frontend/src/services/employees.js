import { getAccessToken } from './auth';

const API_URL = (
    import.meta.env.VITE_API_URL
).replace(/\/$/, '');

async function request(path, options = {}) {
    const response = await fetch(`${API_URL}${path}`, {
        ...options,
        headers: {
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
            // La respuesta puede no contener JSON.
        }

        throw new Error(message);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

export const employeesApi = {
    getAll: (active) => {
        const query = active === undefined
            ? ''
            : `?active=${active}`;

        return request(`/api/employees${query}`);
    },

    getById: (id) => request(`/api/employees/${id}`),

    create: (employee) =>
        request('/api/employees', {
            method: 'POST',
            body: JSON.stringify(employee),
        }),

    update: (id, employee) =>
        request(`/api/employees/${id}`, {
            method: 'PUT',
            body: JSON.stringify(employee),
        }),

    deactivate: (id) =>
        request(`/api/employees/${id}/deactivate`, {
            method: 'PATCH',
        }),
};