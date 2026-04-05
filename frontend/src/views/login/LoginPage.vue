<template>
  <div class="login-page">
    <div class="login-card">
      <h2 class="login-title">库房物料数字化盘点系统</h2>
      <p class="login-subtitle">INV-CHK 盘点管理</p>

      <el-form ref="formRef" :model="form" :rules="rules" size="large" @keyup.enter="handleLogin">
        <el-form-item prop="username">
          <el-input v-model="form.username" placeholder="用户名" :prefix-icon="User" />
        </el-form-item>
        <el-form-item prop="password">
          <el-input
            v-model="form.password"
            type="password"
            placeholder="密码"
            :prefix-icon="Lock"
            show-password
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" style="width: 100%" @click="handleLogin">
            登 录
          </el-button>
        </el-form-item>
      </el-form>

      <div class="demo-accounts">
        <p>测试账号：</p>
        <el-tag
          v-for="acc in demoAccounts"
          :key="acc.username"
          class="demo-tag"
          effect="plain"
          @click="fillDemo(acc)"
        >
          {{ acc.label }}
        </el-tag>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'
import { login } from '@/api/auth'
import { useUserStore } from '@/store/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()
const formRef = ref<FormInstance>()
const loading = ref(false)

const form = reactive({ username: '', password: '' })

const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
}

const demoAccounts = [
  { username: 'admin', password: '123456', label: 'admin (管理员)' },
  { username: 'zhangsan', password: '123456', label: '张三 (主管)' },
  { username: 'lisi', password: '123456', label: '李四 (工人)' },
]

function fillDemo(acc: { username: string; password: string }) {
  form.username = acc.username
  form.password = acc.password
}

async function handleLogin() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  loading.value = true
  try {
    const res = await login({ username: form.username, password: form.password })
    localStorage.setItem('token', res.token)
    userStore.setUser({
      userId: res.userId,
      realName: res.realName,
      permissions: res.permissions,
      warehouses: res.warehouses,
    })
    ElMessage.success(`欢迎回来，${res.realName}`)
    const redirect = (route.query.redirect as string) || '/dashboard'
    router.push(redirect)
  } catch {
    // error already handled by request interceptor
  } finally {
    loading.value = false
  }
}
</script>

<style scoped lang="scss">
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 400px;
  padding: 40px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.15);
}

.login-title {
  text-align: center;
  margin: 0 0 4px;
  font-size: 22px;
  color: #303133;
}

.login-subtitle {
  text-align: center;
  color: #909399;
  font-size: 14px;
  margin: 0 0 32px;
}

.demo-accounts {
  margin-top: 16px;
  text-align: center;

  p {
    font-size: 12px;
    color: #909399;
    margin-bottom: 8px;
  }

  .demo-tag {
    cursor: pointer;
    margin: 0 4px 4px 0;

    &:hover {
      color: #409eff;
      border-color: #409eff;
    }
  }
}
</style>
