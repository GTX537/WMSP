-- ===================================================================
-- 库房物料数字化盘点系统 (WMSP) - 数据库初始化脚本
-- Module: INV-CHK 盘点管理
-- ===================================================================

-- 创建数据库
USE WMS01S;
GO

-- ===== 基础表 =====

-- 用户表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_user')
CREATE TABLE sys_user (
    user_id         INT IDENTITY(1,1) PRIMARY KEY,
    username        NVARCHAR(50)  NOT NULL UNIQUE,
    real_name       NVARCHAR(50)  NOT NULL,
    password_hash   NVARCHAR(200) NULL,
    is_active       BIT NOT NULL DEFAULT 1,
    created_at      DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- 用户权限表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_user_permission')
CREATE TABLE sys_user_permission (
    user_id         INT NOT NULL,
    permission_code NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_sys_user_permission PRIMARY KEY (user_id, permission_code),
    CONSTRAINT FK_user_perm_user FOREIGN KEY (user_id) REFERENCES sys_user(user_id)
);

-- 仓库表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'wh_warehouse')
CREATE TABLE wh_warehouse (
    warehouse_id    INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_code  NVARCHAR(30)  NOT NULL UNIQUE,
    warehouse_name  NVARCHAR(100) NOT NULL,
    is_active       BIT NOT NULL DEFAULT 1
);

-- 用户-仓库关联表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sys_user_warehouse')
CREATE TABLE sys_user_warehouse (
    user_id      INT NOT NULL,
    warehouse_id INT NOT NULL,
    CONSTRAINT PK_sys_user_warehouse PRIMARY KEY (user_id, warehouse_id),
    CONSTRAINT FK_uw_user FOREIGN KEY (user_id) REFERENCES sys_user(user_id),
    CONSTRAINT FK_uw_wh   FOREIGN KEY (warehouse_id) REFERENCES wh_warehouse(warehouse_id)
);

-- 货位表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'wh_location')
CREATE TABLE wh_location (
    location_id      BIGINT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id     INT NOT NULL,
    location_code    NVARCHAR(30) NOT NULL,
    location_barcode NVARCHAR(50) NULL,
    zone             NVARCHAR(50) NULL,
    is_active        BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_loc_wh FOREIGN KEY (warehouse_id) REFERENCES wh_warehouse(warehouse_id)
);
CREATE INDEX IX_location_warehouse ON wh_location(warehouse_id);

-- 物料主数据
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'mat_material')
CREATE TABLE mat_material (
    material_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    material_code NVARCHAR(50)  NOT NULL UNIQUE,
    material_name NVARCHAR(200) NOT NULL,
    barcode       NVARCHAR(50)  NULL,
    unit          NVARCHAR(20)  NOT NULL,
    unit_cost     DECIMAL(18,4) NULL
);

-- 库存表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'inv_stock')
CREATE TABLE inv_stock (
    stock_id        BIGINT IDENTITY(1,1) PRIMARY KEY,
    material_id     BIGINT NOT NULL,
    location_id     BIGINT NOT NULL,
    warehouse_id    INT NOT NULL,
    batch_no        NVARCHAR(50) NULL,
    book_qty        DECIMAL(18,4) NOT NULL DEFAULT 0,
    last_check_date DATETIME2 NULL,
    CONSTRAINT FK_stock_mat FOREIGN KEY (material_id) REFERENCES mat_material(material_id),
    CONSTRAINT FK_stock_loc FOREIGN KEY (location_id) REFERENCES wh_location(location_id)
);
CREATE UNIQUE INDEX IX_stock_unique ON inv_stock(material_id, location_id, batch_no) WHERE batch_no IS NOT NULL;
CREATE INDEX IX_stock_location ON inv_stock(location_id);

-- 库存流水表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'inv_transaction')
CREATE TABLE inv_transaction (
    transaction_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    material_id    BIGINT NOT NULL,
    location_id    BIGINT NOT NULL,
    trans_type     NVARCHAR(20)  NOT NULL,
    qty            DECIMAL(18,4) NOT NULL,
    before_qty     DECIMAL(18,4) NOT NULL,
    after_qty      DECIMAL(18,4) NOT NULL,
    ref_doc_no     NVARCHAR(30)  NULL,
    operator_id    INT NOT NULL,
    created_at     DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- ===== 盘点业务表 =====

-- 盘点计划表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chk_plan')
CREATE TABLE chk_plan (
    plan_id       BIGINT IDENTITY(1,1) PRIMARY KEY,
    plan_no       NVARCHAR(30)  NOT NULL,
    plan_name     NVARCHAR(200) NOT NULL,
    warehouse_id  INT NOT NULL,
    plan_type     NVARCHAR(20)  NOT NULL,  -- FULL/ZONE/SPOT
    check_mode    NVARCHAR(20)  NOT NULL,  -- BLIND/OPEN
    target_zones  NVARCHAR(500) NULL,
    sample_rate   INT NULL,
    plan_date     DATE NOT NULL,
    status        NVARCHAR(20)  NOT NULL DEFAULT 'DRAFT',
    created_by    INT NOT NULL,
    approved_by   INT NULL,
    completed_at  DATETIME2 NULL,
    remark        NVARCHAR(500) NULL,
    created_at    DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at    DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_plan_wh   FOREIGN KEY (warehouse_id) REFERENCES wh_warehouse(warehouse_id),
    CONSTRAINT FK_plan_user FOREIGN KEY (created_by)   REFERENCES sys_user(user_id)
);
CREATE UNIQUE INDEX IX_plan_no ON chk_plan(plan_no);

-- 盘点子任务表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chk_task')
CREATE TABLE chk_task (
    task_id       BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_no       NVARCHAR(30)  NOT NULL,
    plan_id       BIGINT NOT NULL,
    location_id   BIGINT NOT NULL,
    assigned_to   INT NULL,
    status        NVARCHAR(20)  NOT NULL DEFAULT 'PENDING',
    claimed_at    DATETIME2 NULL,
    submitted_at  DATETIME2 NULL,
    reviewed_by   INT NULL,
    reviewed_at   DATETIME2 NULL,
    reject_reason NVARCHAR(500) NULL,
    created_at    DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at    DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_task_plan FOREIGN KEY (plan_id)     REFERENCES chk_plan(plan_id),
    CONSTRAINT FK_task_loc  FOREIGN KEY (location_id) REFERENCES wh_location(location_id),
    CONSTRAINT FK_task_user FOREIGN KEY (assigned_to)  REFERENCES sys_user(user_id)
);
CREATE UNIQUE INDEX IX_task_no ON chk_task(task_no);
CREATE INDEX IX_task_plan ON chk_task(plan_id);

-- 盘点明细表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'chk_detail')
CREATE TABLE chk_detail (
    detail_id    BIGINT IDENTITY(1,1) PRIMARY KEY,
    task_id      BIGINT NOT NULL,
    material_id  BIGINT NOT NULL,
    location_id  BIGINT NOT NULL,
    batch_no     NVARCHAR(50) NULL,
    book_qty     DECIMAL(18,4) NOT NULL,
    actual_qty   DECIMAL(18,4) NULL,
    diff_qty     AS (ISNULL(actual_qty, 0) - book_qty) PERSISTED,
    diff_reason  NVARCHAR(200) NULL,
    scan_time    DATETIME2 NULL,
    is_rechecked BIT NOT NULL DEFAULT 0,
    recheck_qty  DECIMAL(18,4) NULL,
    operator_id  INT NOT NULL,
    remark       NVARCHAR(500) NULL,
    created_at   DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at   DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_detail_task FOREIGN KEY (task_id)     REFERENCES chk_task(task_id),
    CONSTRAINT FK_detail_mat  FOREIGN KEY (material_id) REFERENCES mat_material(material_id),
    CONSTRAINT FK_detail_loc  FOREIGN KEY (location_id) REFERENCES wh_location(location_id),
    CONSTRAINT FK_detail_user FOREIGN KEY (operator_id) REFERENCES sys_user(user_id)
);
CREATE INDEX IX_detail_task ON chk_detail(task_id);
CREATE INDEX IX_detail_material ON chk_detail(material_id);

-- ===== 测试数据 =====

-- 用户
SET IDENTITY_INSERT sys_user ON;
INSERT INTO sys_user (user_id, username, real_name) VALUES
(1, 'admin',    '系统管理员'),
(2, 'zhangsan', '张三'),
(3, 'lisi',     '李四'),
(4, 'wangwu',   '王五');
SET IDENTITY_INSERT sys_user OFF;

-- 权限
INSERT INTO sys_user_permission (user_id, permission_code) VALUES
(1, '*'),
(2, 'check:plan'), (2, 'check:plan:create'), (2, 'check:publish'),
(2, 'check:task'), (2, 'check:review'), (2, 'report:diff'), (2, 'inventory:adjust'),
(3, 'check:scan'), (3, 'check:submit'), (3, 'check:task'),
(4, 'check:scan'), (4, 'check:submit'), (4, 'check:task');

-- 仓库
SET IDENTITY_INSERT wh_warehouse ON;
INSERT INTO wh_warehouse (warehouse_id, warehouse_code, warehouse_name) VALUES
(1, 'WH-01', '主仓库'),
(2, 'WH-02', '备用仓库');
SET IDENTITY_INSERT wh_warehouse OFF;

-- 用户-仓库
INSERT INTO sys_user_warehouse (user_id, warehouse_id) VALUES
(1, 1), (1, 2), (2, 1), (2, 2), (3, 1), (4, 1);

-- 货位
SET IDENTITY_INSERT wh_location ON;
INSERT INTO wh_location (location_id, warehouse_id, location_code, location_barcode, zone) VALUES
(1,  1, 'A-01-01', 'LOC-A01-01', 'A区'),
(2,  1, 'A-01-02', 'LOC-A01-02', 'A区'),
(3,  1, 'A-02-01', 'LOC-A02-01', 'A区'),
(4,  1, 'B-01-01', 'LOC-B01-01', 'B区'),
(5,  1, 'B-01-02', 'LOC-B01-02', 'B区'),
(6,  1, 'C-01-01', 'LOC-C01-01', 'C区'),
(7,  2, 'D-01-01', 'LOC-D01-01', 'D区'),
(8,  2, 'D-01-02', 'LOC-D01-02', 'D区');
SET IDENTITY_INSERT wh_location OFF;

-- 物料
SET IDENTITY_INSERT mat_material ON;
INSERT INTO mat_material (material_id, material_code, material_name, barcode, unit, unit_cost) VALUES
(1,  'MAT-001', '螺栓M8x30',    '6901234567890', '个',  0.50),
(2,  'MAT-002', '螺母M8',        '6901234567891', '个',  0.30),
(3,  'MAT-003', '垫片M8',        '6901234567892', '个',  0.10),
(4,  'MAT-004', '轴承6205',      '6901234567893', '个', 25.00),
(5,  'MAT-005', '密封圈DN50',    '6901234567894', '个',  3.50),
(6,  'MAT-006', '润滑油5W-30',   '6901234567895', '桶', 85.00),
(7,  'MAT-007', '钢管DN100',     '6901234567896', '米', 120.00),
(8,  'MAT-008', '电缆RVV3x2.5', '6901234567897', '米', 15.00),
(9,  'MAT-009', '安全帽',        '6901234567898', '顶', 35.00),
(10, 'MAT-010', '工作手套',      '6901234567899', '双',  8.00);
SET IDENTITY_INSERT mat_material OFF;

-- 库存
INSERT INTO inv_stock (material_id, location_id, warehouse_id, batch_no, book_qty) VALUES
(1,  1, 1, 'B2026-001', 500),
(2,  1, 1, 'B2026-001', 800),
(3,  1, 1, NULL,         1000),
(4,  2, 1, 'B2026-002', 50),
(5,  2, 1, NULL,         200),
(6,  3, 1, 'B2026-003', 30),
(7,  4, 1, NULL,         100),
(8,  4, 1, NULL,         500),
(9,  5, 1, NULL,         80),
(10, 5, 1, NULL,         200),
(1,  6, 1, 'B2026-004', 300),
(4,  7, 2, 'B2026-005', 20),
(6,  8, 2, 'B2026-006', 15);

PRINT '数据库初始化完成';
GO
