import { getAccessToken } from "./auth";

const API_URL =
    import.meta.env.VITE_API_URL;

async function request(endpoint) {
    const response = await fetch(
        `${API_URL}/api/employee-portal/payslips${endpoint}`,
        {
            method: "GET",
            headers: {
                Authorization: `Bearer ${getAccessToken()}`,
                "Content-Type": "application/json",
            },
        }
    );

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(
            errorText || "No fue posible consultar los desprendibles."
        );
    }

    return response.json();
}

export const payslipsApi = {
    getAll: () => request(""),
    getById: (id) => request(`/${id}`),
};