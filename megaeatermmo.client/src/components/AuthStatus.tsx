interface Props {
    authenticated: boolean;
}

export function AuthStatus({authenticated}: Props) {
    return (
        <div>
            {authenticated
                ? "Logged in" // replace with username at later time 
                : "Not logged in"
            }
        
        </div>
    )
}