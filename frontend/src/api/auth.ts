import request from '@/utils/request'
import type { Warehouse } from '@/types/check'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  userId: number
  realName: string
  permissions: string[]
  warehouses: Warehouse[]
}

export function login(data: LoginRequest) {
  return request.post<any, LoginResponse>('/auth/login', data)
}

export function getMe() {
  return request.get<any, LoginResponse>('/auth/me')
}
