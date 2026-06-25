import './App.css'

import { LoginForm } from "./components/LoginForm";

import { AuthStatus } from "./components/AuthStatus";

import { useAuth } from "./hooks/useAuth";

function App() {

    const auth = useAuth();

    return (
        <>
            <AuthStatus
                authenticated={auth.isAuthenticated}
            />

            <LoginForm
                onLogin={auth.setToken}
            />
        </>
    )
}

export default App
