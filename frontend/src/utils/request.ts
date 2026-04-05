import axios from 'axios'
import { ElMessage } from 'element-plus'

const request = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 15000,
})

request.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

request.interceptors.response.use(
  (res) => res.data,
  (err) => {
    const msg = err.response?.data?.message || err.message || '请求失败'
    const status = err.response?.status
    if (status === 401) {
      // 避免登录接口自身401时跳转
      if (!err.config?.url?.includes('/auth/')) {
        localStorage.removeItem('token')
        window.location.href = '/login'
      }
    } else if (status === 403) {
      ElMessage.error('无操作权限')
    } else if (status === 409) {
      ElMessage.warning(msg)
    } else {
      ElMessage.error(msg)
    }
    return Promise.reject(err)
  }
)

export default request
