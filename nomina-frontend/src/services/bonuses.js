
import { getAccessToken } from "./auth";

const API_URL =
    import.meta.env.VITE_API_URL;

export const bonusesApi = {

    async preview(year, semester, cutoffDate) {
        const params = new URLSearchParams({
            year: String(year),
            semester: String(semester),
            cutoffDate
        });

        const response = await fetch(
            `${API_URL}/api/bonuses/preview?${params}`,
            {
                headers: {
                    Authorization: `Bearer ${getAccessToken()}`
                }
            }
        );

        const result = await response.json();

        if (!response.ok) {
            throw new Error(
                result.message ||
                "No se pudo calcular la prima."
            );
        }

        return result;
    }

};


export const bonusSettlementsApi = {
    async getAll() {
        return requestSettlements("");
    },

    async getById(id) {
        return requestSettlements(`/${id}`);
    },


    async updateCutoff(id, cutoffDate) {
        return requestSettlements(`/${id}/cutoff`, {
            method: "PUT",
            body: JSON.stringify({
                cutoffDate
            })
        });
    },

    async create(year, semester, cutoffDate) {
        return requestSettlements("", {
            method: "POST",
            body: JSON.stringify({
                year,
                semester,
                cutoffDate
            })
        });
    },

    async recalculate(id) {
        return requestSettlements(`/${id}/recalculate`, {
            method: "POST"
        });
    },

    async validate(id) {
        return requestSettlements(`/${id}/validate`);
    }
};


async function requestSettlements(endpoint, options = {}) {
    const response = await fetch(
        `${API_URL}/api/bonus-settlements${endpoint}`,
        {
            ...options,
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${getAccessToken()}`,
                ...options.headers
            }
        }
    );

    const responseText = await response.text();

    let result = null;

    try {
        result = responseText
            ? JSON.parse(responseText)
            : null;
    } catch {
        result = responseText;
    }

    if (!response.ok) {
        console.error(
            "Error del backend:",
            response.status,
            result
        );

        const errorMessage =
            typeof result === "string"
                ? result
                : result?.message ||
                result?.detail ||
                result?.title ||
                "Error al procesar la liquidación.";

        throw new Error(errorMessage);
    }

    return result;
}


export const bonusPreviousPaymentsApi = {
    getSummary(settlementId) {
        return requestSettlements(
            `/${settlementId}/previous-payments`
        );
    },

    register(settlementId, data) {
        return requestSettlements(
            `/${settlementId}/previous-payments`,
            {
                method: "POST",
                body: JSON.stringify(data)
            }
        );
    }
};



