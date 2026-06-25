import { useState } from "react";
import { login } from "../api/authApi";

interface Props {
    onLogin: (token: string) => void;
}

export function LoginForm(
    { onLogin }: Props) {

    const [username, setUsername] =
        useState("");

    const [password, setPassword] =
        useState("");

    async function handleSubmit(
        e: React.FormEvent) {

        e.preventDefault();

        const result = await login({
            username,
            password
        });

        onLogin(result.token);
    }

    return (
        <form onSubmit={handleSubmit}>
            <input
                value={username}
                onChange={e =>
                    setUsername(e.target.value)}
            />

            <input
                type="password"
                value={password}
                onChange={e =>
                    setPassword(e.target.value)}
            />

            <button type="submit">
                Login
            </button>
        </form>
    );
}