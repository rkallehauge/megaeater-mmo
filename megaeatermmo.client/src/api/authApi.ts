import { apiUrl } from "./client"

import type {
    LoginRequest,
    LoginResponse
} from "../types/auth"

export async function login(
    request: LoginRequest
) : Promise<LoginResponse> {
    const response = await fetch(
        `${apiUrl}/api/authentication/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    }
    );

    if (!response.ok) {
        throw new Error("Authentication failed.");
    }

    return await response.json();
}