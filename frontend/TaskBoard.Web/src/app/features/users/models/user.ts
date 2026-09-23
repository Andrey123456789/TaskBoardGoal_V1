export interface User {
  id: string;
  name: string;
  email: string;
  createdAt: string;
}

export interface SaveUserRequest {
  name: string;
  email: string;
}
