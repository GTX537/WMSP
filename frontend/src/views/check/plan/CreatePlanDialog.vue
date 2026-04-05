<template>
  <el-dialog
    :model-value="modelValue"
    title="新建盘点计划"
    width="600px"
    destroy-on-close
    @update:model-value="$emit('update:modelValue', $event)"
    @open="onOpen"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
      <el-form-item label="计划名称" prop="plan_name">
        <el-input v-model="form.plan_name" maxlength="200" show-word-limit placeholder="请输入计划名称" />
      </el-form-item>
      <el-form-item label="目标仓库" prop="warehouse_id">
        <el-select v-model="form.warehouse_id" placeholder="请选择仓库" style="width: 100%">
          <el-option
            v-for="wh in userStore.warehouses"
            :key="wh.warehouse_id"
            :label="wh.warehouse_name"
            :value="wh.warehouse_id"
          />
        </el-select>
      </el-form-item>
      <el-form-item label="盘点类型" prop="plan_type">
        <el-radio-group v-model="form.plan_type" @change="onTypeChange">
          <el-radio-button value="FULL">全盘</el-radio-button>
          <el-radio-button value="ZONE">区域盘</el-radio-button>
          <el-radio-button value="SPOT">抽盘</el-radio-button>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="盘点模式" prop="check_mode">
        <el-radio-group v-model="form.check_mode">
          <el-radio-button value="BLIND">盲盘</el-radio-button>
          <el-radio-button value="OPEN">明盘</el-radio-button>
        </el-radio-group>
      </el-form-item>
      <el-form-item label="计划日期" prop="plan_date">
        <el-date-picker
          v-model="form.plan_date"
          type="date"
          placeholder="请选择日期"
          value-format="YYYY-MM-DD"
          :disabled-date="(d: Date) => d.getTime() < Date.now() - 86400000"
          style="width: 100%"
        />
      </el-form-item>
      <!-- ZONE类型显示区域选择 -->
      <el-form-item v-if="form.plan_type === 'ZONE'" label="区域选择" prop="zones">
        <el-select
          v-model="form.zones"
          multiple
          placeholder="请选择区域"
          style="width: 100%"
          :loading="zonesLoading"
        >
          <el-option v-for="z in zoneOptions" :key="z" :label="z" :value="z" />
        </el-select>
      </el-form-item>
      <!-- SPOT类型显示抽盘比例 -->
      <el-form-item v-if="form.plan_type === 'SPOT'" label="抽盘比例" prop="sample_rate">
        <el-input-number v-model="form.sample_rate" :min="1" :max="100" :step="5" style="width: 100%">
          <template #suffix>%</template>
        </el-input-number>
      </el-form-item>
      <el-form-item label="备注">
        <el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="可选" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="$emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">确认创建</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import type { FormInstance, FormRules } from 'element-plus'
import { useUserStore } from '@/store/user'
import { createCheckPlan, getWarehouseZones } from '@/api/check'
import type { CreatePlanForm, PlanType } from '@/types/check'
import dayjs from 'dayjs'

defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{
  'update:modelValue': [val: boolean]
  success: []
}>()

const userStore = useUserStore()
const formRef = ref<FormInstance>()
const submitting = ref(false)
const zonesLoading = ref(false)
const zoneOptions = ref<string[]>([])

const defaultForm = (): CreatePlanForm => ({
  plan_name: '',
  warehouse_id: null,
  plan_type: 'FULL',
  check_mode: 'BLIND',
  plan_date: dayjs().format('YYYY-MM-DD'),
  zones: [],
  sample_rate: null,
  remark: '',
})

const form = reactive<CreatePlanForm>(defaultForm())

const rules = reactive<FormRules<CreatePlanForm>>({
  plan_name: [{ required: true, message: '计划名称不能为空', trigger: 'blur' }],
  warehouse_id: [{ required: true, message: '请选择目标仓库', trigger: 'change' }],
  plan_type: [{ required: true, message: '请选择盘点类型', trigger: 'change' }],
  check_mode: [{ required: true, message: '请选择盘点模式', trigger: 'change' }],
  plan_date: [
    { required: true, message: '请选择计划日期', trigger: 'change' },
  ],
  zones: [{
    validator: (_rule, value, callback) => {
      if (form.plan_type === 'ZONE' && (!value || value.length === 0)) {
        callback(new Error('区域盘时必须选择至少一个区域'))
      } else {
        callback()
      }
    },
    trigger: 'change',
  }],
  sample_rate: [{
    validator: (_rule, value, callback) => {
      if (form.plan_type === 'SPOT') {
        if (value === null || value === undefined) {
          callback(new Error('抽盘时必须填写抽盘比例'))
        } else if (value < 1 || value > 100) {
          callback(new Error('抽盘比例必须在1-100之间'))
        } else {
          callback()
        }
      } else {
        callback()
      }
    },
    trigger: 'change',
  }],
})

function onOpen() {
  Object.assign(form, defaultForm())
  // 单仓库时自动选中
  if (userStore.warehouses.length === 1) {
    form.warehouse_id = userStore.warehouses[0].warehouse_id
  }
  formRef.value?.clearValidate()
}

function onTypeChange(val: PlanType) {
  form.zones = []
  form.sample_rate = null
  if (val === 'ZONE' && form.warehouse_id) {
    loadZones(form.warehouse_id)
  }
}

// 仓库切换时重新加载区域
watch(() => form.warehouse_id, (val) => {
  form.zones = []
  if (val && form.plan_type === 'ZONE') {
    loadZones(val)
  }
})

async function loadZones(warehouseId: number) {
  zonesLoading.value = true
  try {
    zoneOptions.value = await getWarehouseZones(warehouseId)
  } finally {
    zonesLoading.value = false
  }
}

async function handleSubmit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    await createCheckPlan(form)
    emit('success')
  } finally {
    submitting.value = false
  }
}
</script>
