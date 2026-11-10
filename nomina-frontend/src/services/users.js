import { getAccessToken } from "./auth";

const API_URL =
    import.meta.env.VITE_API_URL;

export const usersApi = {
    async createEmployee(data) {
        const response = await fetch(`${API_URL}/api/users/employee`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${getAccessToken()}`
            },
            body: JSON.stringify(data)
        });

        const result = await response.json().catch(() => null);

        if (!response.ok) {
            throw new Error(
                result?.message ||
                result?.title ||
                "No fue posible crear la cuenta del empleado."
            );
        }

        return result;
    }
};