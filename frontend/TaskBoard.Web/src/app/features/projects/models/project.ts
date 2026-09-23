export interface Project {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
}

export interface SaveProjectRequest {
  name: string;
  description: string | null;
}
