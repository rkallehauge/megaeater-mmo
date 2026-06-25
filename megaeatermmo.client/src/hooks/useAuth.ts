import { useState } from "react";

export function useAuth() {

    const [token, setToken] =
        useState<string | null>(null);

    const isAuthenticated = token !== null;

    return {
        token,
        setToken,
        isAuthenticated
    };
}