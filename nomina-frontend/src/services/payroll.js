import { getAccessToken } from "./auth";

const API_URL =
    import.meta.env.VITE_API_URL;

async function request(endpoint, options = {}) {
    const response = await fetch(`${API_URL}/api/payroll${endpoint}`, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${getAccessToken()}`,
            ...options.headers
        }
    });

    const responseText = await response.text();

    let result = null;

    try {
        result = responseText ? JSON.parse(responseText) : null;
    } catch {
        result = responseText;
    }

    if (!response.ok) {
        const errorMessage =
            typeof result === "string"
                ? result
                : result?.detail ||
                result?.message ||
                result?.title ||
                "Ocurrió un error al procesar la solicitud.";

        throw new Error(errorMessage);
    }

    return result;
}

export const payrollApi = {
    getPeriods() {
        return request("/periods");
    },

    createPeriod(data) {
        return request("/periods", {
            method: "POST",
            body: JSON.stringify(data)
        });
    },

    calculatePeriod(periodId) {
        return request(`/periods/${periodId}/calculate`, {
            method: "POST"
        });
    },

    getDetails(periodId) {
        return request(`/periods/${periodId}/details`);
    },

    closePeriod(periodId) {
        return request(`/periods/${periodId}/close`, {
            method: "POST"
        });
    }
};