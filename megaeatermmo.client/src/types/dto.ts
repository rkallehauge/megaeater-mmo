export interface UserDTO {
    username: string;
    id: number;
}

export interface ScoreDTO {
    user_id: number;
    score: number;
    date_set: string;
}