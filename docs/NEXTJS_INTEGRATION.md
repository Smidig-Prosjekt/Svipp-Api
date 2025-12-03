# Next.js Integration Guide - JWT Autentisering

Denne guiden viser hvordan du integrerer Next.js frontend med Svipp API's JWT-autentisering.

## Oversikt

Next.js-appen må:
1. Håndtere login/register
2. Lagre JWT-token (cookies eller localStorage)
3. Sende token med alle API-kall
4. Håndtere token-utløp og refresh

## Oppsett

### 1. Environment Variables

Opprett `.env.local` i Next.js-prosjektet:

```env
NEXT_PUBLIC_API_URL=http://localhost:5087
# Eller i production:
# NEXT_PUBLIC_API_URL=https://api.svipp.no
```

### 2. API Client Setup

Opprett `lib/api-client.ts`:

```typescript
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5087';

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: {
    id: string;
    fullName: string;
    email: string;
    phoneNumber: string;
    createdAt: string;
    updatedAt?: string;
  };
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
}

class ApiClient {
  private baseUrl: string;

  constructor() {
    this.baseUrl = API_URL;
  }

  // Hent token fra cookies (server-side) eller localStorage (client-side)
  private getToken(): string | null {
    if (typeof window === 'undefined') {
      // Server-side: bruk cookies
      const { cookies } = require('next/headers');
      return cookies().get('auth_token')?.value || null;
    } else {
      // Client-side: bruk localStorage
      return localStorage.getItem('auth_token');
    }
  }

  // Lagre token
  private setToken(token: string): void {
    if (typeof window === 'undefined') {
      // Server-side: sett cookie
      const { cookies } = require('next/headers');
      cookies().set('auth_token', token, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        maxAge: 60 * 60 * 24, // 24 timer
      });
    } else {
      // Client-side: sett localStorage
      localStorage.setItem('auth_token', token);
    }
  }

  // Fjern token
  private removeToken(): void {
    if (typeof window === 'undefined') {
      const { cookies } = require('next/headers');
      cookies().delete('auth_token');
    } else {
      localStorage.removeItem('auth_token');
    }
  }

  // Gjør API-kall med autentisering
  async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = this.getToken();
    
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({
        message: 'An error occurred',
      }));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Auth endpoints
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await this.request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
    
    this.setToken(response.token);
    return response;
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await this.request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    });
    
    this.setToken(response.token);
    return response;
  }

  async logout(): Promise<void> {
    this.removeToken();
  }

  // User endpoints
  async getCurrentUser() {
    return this.request('/api/users/me');
  }

  async updateUser(data: {
    fullName: string;
    email: string;
    phoneNumber: string;
  }) {
    return this.request('/api/users/me', {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  async changePassword(data: {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
  }) {
    return this.request('/api/users/me/password', {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }
}

export const apiClient = new ApiClient();
```

### 3. Auth Context (React Context)

Opprett `contexts/AuthContext.tsx`:

```typescript
'use client';

import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { apiClient, AuthResponse } from '@/lib/api-client';

interface AuthContextType {
  user: AuthResponse['user'] | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: {
    fullName: string;
    email: string;
    phoneNumber: string;
    password: string;
  }) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResponse['user'] | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Sjekk om bruker er logget inn ved oppstart
    checkAuth();
  }, []);

  async function checkAuth() {
    try {
      const token = localStorage.getItem('auth_token');
      if (token) {
        const userData = await apiClient.getCurrentUser();
        setUser(userData);
      }
    } catch (error) {
      // Token er ugyldig eller utløpt
      localStorage.removeItem('auth_token');
    } finally {
      setIsLoading(false);
    }
  }

  async function login(email: string, password: string) {
    const response = await apiClient.login({ email, password });
    setUser(response.user);
  }

  async function register(data: {
    fullName: string;
    email: string;
    phoneNumber: string;
    password: string;
  }) {
    const response = await apiClient.register(data);
    setUser(response.user);
  }

  async function logout() {
    await apiClient.logout();
    setUser(null);
  }

  async function refreshUser() {
    try {
      const userData = await apiClient.getCurrentUser();
      setUser(userData);
    } catch (error) {
      setUser(null);
    }
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        register,
        logout,
        refreshUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
```

### 4. Login Page

Opprett `app/login/page.tsx` (eller `pages/login.tsx` for Pages Router):

```typescript
'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const router = useRouter();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      await login(email, password);
      router.push('/dashboard'); // Eller hvor du vil redirecte
    } catch (err: any) {
      setError(err.message || 'Login failed');
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="max-w-md mx-auto mt-8">
      <h1 className="text-2xl font-bold mb-4">Logg inn</h1>
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="bg-red-100 text-red-700 p-3 rounded">
            {error}
          </div>
        )}
        <div>
          <label className="block mb-1">E-post</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            className="w-full p-2 border rounded"
          />
        </div>
        <div>
          <label className="block mb-1">Passord</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            className="w-full p-2 border rounded"
          />
        </div>
        <button
          type="submit"
          disabled={isLoading}
          className="w-full bg-blue-500 text-white p-2 rounded hover:bg-blue-600 disabled:opacity-50"
        >
          {isLoading ? 'Logger inn...' : 'Logg inn'}
        </button>
      </form>
    </div>
  );
}
```

### 5. Protected Route Middleware

Opprett `middleware.ts` i root av Next.js-prosjektet:

```typescript
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const token = request.cookies.get('auth_token');

  // Beskyttede ruter
  const protectedPaths = ['/dashboard', '/profile', '/settings'];
  const isProtectedPath = protectedPaths.some((path) =>
    request.nextUrl.pathname.startsWith(path)
  );

  if (isProtectedPath && !token) {
    // Redirect til login hvis ikke autentisert
    return NextResponse.redirect(new URL('/login', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/dashboard/:path*', '/profile/:path*', '/settings/:path*'],
};
```

### 6. API Route Proxy (Valgfritt - for server-side)

Opprett `app/api/proxy/[...path]/route.ts` for å håndtere CORS og server-side requests:

```typescript
import { NextRequest, NextResponse } from 'next/server';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5087';

export async function GET(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) {
  return proxyRequest(request, params.path, 'GET');
}

export async function POST(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) {
  return proxyRequest(request, params.path, 'POST');
}

export async function PUT(
  request: NextRequest,
  { params }: { params: { path: string[] } }
) {
  return proxyRequest(request, params.path, 'PUT');
}

async function proxyRequest(
  request: NextRequest,
  path: string[],
  method: string
) {
  const token = request.cookies.get('auth_token');
  const body = method !== 'GET' ? await request.text() : undefined;

  const response = await fetch(`${API_URL}/api/${path.join('/')}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token.value}` }),
    },
    body,
  });

  const data = await response.json();

  return NextResponse.json(data, { status: response.status });
}
```

## Bruk i komponenter

```typescript
'use client';

import { useAuth } from '@/contexts/AuthContext';

export default function ProfilePage() {
  const { user, isLoading, logout } = useAuth();

  if (isLoading) return <div>Laster...</div>;
  if (!user) return <div>Ikke logget inn</div>;

  return (
    <div>
      <h1>Profil</h1>
      <p>Navn: {user.fullName}</p>
      <p>E-post: {user.email}</p>
      <button onClick={logout}>Logg ut</button>
    </div>
  );
}
```

## CORS-konfigurasjon

I `Program.cs` på API-siden, legg til Next.js origin:

```csharp
policy.WithOrigins(
    "http://localhost:3000", // Next.js dev server
    "https://svipp.no",       // Production
    "https://www.svipp.no"
)
```

## Tips og Best Practices

1. **Token Storage:**
   - Bruk `httpOnly` cookies for bedre sikkerhet (server-side)
   - Eller localStorage for enklere client-side implementasjon

2. **Token Refresh:**
   - Implementer automatisk refresh før utløp
   - Eller håndter 401 og redirect til login

3. **Error Handling:**
   - Håndter 401 (Unauthorized) ved å fjerne token og redirecte
   - Vis brukervennlige feilmeldinger

4. **Loading States:**
   - Vis loading-indikatorer under auth-operasjoner
   - Sjekk auth-status ved app-start

5. **Type Safety:**
   - Bruk TypeScript for alle API-responser
   - Definer interfaces for alle DTOs



