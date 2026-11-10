
import { getAccessToken } from './auth';

const API_URL =
    import.meta.env.VITE_API_URL;

export async function getMyInformation() {
    const token = getAccessToken();

    const response = await fetch(
        `${API_URL}/api/employee-portal/me`,
        {
            method: 'GET',
            headers: {
                Authorization: `Bearer ${token}`,
                'Content-Type': 'application/json',
            },
        }
    );

    if (!response.ok) {
        const message = await response.text();
        throw new Error(message || `Error HTTP ${response.status}`);
    }

    return response.json();
}
