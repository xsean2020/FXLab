# Water Surface System — FXLab 水面渲染系统

> FXLab 项目 — 各类效果展示与学习库。此文档仅描述水面效果模块。

## 概述

基于 Unity Built-in Render Pipeline 的完整水面渲染方案，支持 **Gerstner 波**顶点位移 + **Haimian 风格泡沫** + **湍流 UV 扭曲**。

---

## 文件结构

```
Assets/Water/                      ← 所有水系统文件完全隔离在此目录
├── README.md                      ← 本文档
├── Shaders/
│   └── WaterSurface.shader        ← 唯一水面 Shader（Shader name: "Water/WaterSurface"）
├── Scripts/
│   ├── FloatingObject.cs          ← 浮动物体（Rigidbody 浮力物理）
│   └── DepthTextureEnabler.cs     ← 自动开启摄像机深度纹理
├── Editor/
│   └── WaterSceneSetup.cs         ← 一键场景搭建工具（菜单: Tools > Water > Setup Water Scene）
├── Textures/
│   ├── FX_sence_17posuizhidi_xingyunnoise.png  ← 湍流噪声贴图（输入）
│   ├── water001_zbh.png                       ← 水面纹理（输入，用于生成法线贴图）
│   ├── Water_Normal_01.png                    ← 法线贴图 1（自动生成）
│   ├── Water_Normal_02.png                    ← 法线贴图 2（自动生成）
│   └── Water_Foam_Noise.png                   ← 泡沫噪声贴图（自动生成）
└── Materials/
    └── Water_Mat.mat                          ← 水面材质（自动生成）
```

---

## Shader: `Water/WaterSurface`

完整功能水面着色器，所有参数暴露在材质面板中，**无需 C# 脚本**。

### Properties 完整参数表

#### Wave Mode（波浪模式）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_UseGerstner` | Toggle | 1 | 1=Gerstner 波，0=正弦波（回退方案） |
| `_WaveHeightScale` | Range(0,2) | 0.3 | **总波浪幅度缩放**，调这个最快控制波高 |
| `_Steepness` | Range(0,1) | 0.3 | Gerstner 波陡度，越大波峰越尖 |
| `_WaveAmp1~4` | Range(0,2) | 0.4/0.25/0.15/0.08 | 各波振幅 |
| `_WaveFreq1~4` | Range(0.1,5) | 0.8/1.2/2.0/3.0 | 各波频率 |
| `_WaveSpeed1~4` | Range(0.1,3) | 1.2/0.8/2.0/1.5 | 各波速度 |
| `_WaveDir1~4` | Vector | (1,0)/(0.7,0.7)/(-0.3,0.9)/(-0.8,0.6) | 各波方向（XY） |

#### Normal Maps（法线贴图）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_NormalMap1` | 2D | "bump" | 法线贴图 1 |
| `_NormalMap2` | 2D | "bump" | 法线贴图 2（双层叠加增强细节） |
| `_NormalScale` | Range(0,3) | 1.2 | 法线强度 |
| `_NormalSpeed1` | Vector | (0.015, 0.008) | 法线 1 滚动速度 |
| `_NormalSpeed2` | Vector | (-0.012, 0.015) | 法线 2 滚动速度 |
| `_NormalTiling` | Range(0.5,10) | 2.5 | 法线贴图平铺密度 |

#### Turbulence（湍流 UV 扭曲）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_TurbulenceTex` | 2D | "black" | 湍流噪声贴图 |
| `_TurbulenceStrength` | Range(0,0.1) | 0.03 | 扭曲强度 |
| `_TurbulenceSpeed` | Vector | (0.01, 0.01) | 扭曲动画速度 |

#### Reflection & Fresnel（反射 & 菲涅尔）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_Cubemap` | Cube | "_Skybox" | 反射 Cubemap |
| `_ReflectionStrength` | Range(0,1) | 0.5 | 反射强度 |
| `_FresnelPower` | Range(0.5,8) | 3.0 | 菲涅尔指数，越大边缘反射越集中 |
| `_FresnelOffset` | Range(0,0.5) | 0.02 | 菲涅尔基础反射率（水的 F0≈0.02） |

#### Specular（高光）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_SpecColor` | Color | (1,1,1) | 高光颜色 |
| `_Shininess` | Range(0.1,128) | 64 | 高光锐度 |

#### Water Color（水面颜色）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_ShallowColor` | Color | (0.1, 0.3, 0.2, 0.6) | 浅水区颜色（RGBA） |
| `_DeepColor` | Color | (0.0, 0.05, 0.1, 0.9) | 深水区颜色（RGBA） |
| `_DepthFactor` | Range(0.1,2) | 0.5 | 深浅混合速度 |

#### Foam（泡沫 — Haimian 风格）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_FoamColor` | Color | (1,1,1,1) | 泡沫颜色 |
| `_FoamTexture` | 2D | "white" | 泡沫噪声贴图 |
| `_MainFoamScale` | Float | 40 | 泡沫纹理密度 |
| `_MainFoamIntensity` | Range(0,10) | 3.8 | 泡沫强度 |
| `_MainFoamSpeed` | Float | 0.1 | 泡沫动画速度 |
| `_MainFoamOpacity` | Range(0,1) | 0.87 | 泡沫不透明度 |
| `_MainFoamWidth` | Range(0.01,2) | 0.2 | 泡沫宽度（越大泡沫范围越宽） |

#### Alpha（透明度）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_Alpha` | Range(0,1) | 0.85 | 总体透明度 |

### Shader 核心逻辑

#### 顶点着色器（vert）

```
1. 计算世界坐标 worldPos = ObjectToWorld(vertex)
2. 根据 _UseGerstner 选择波形：
   Gerstner: 4 方向 Gerstner 波叠加 → worldPos += offset * _WaveHeightScale
   正弦波: 3 层正弦波叠加 → worldPos.y += h
3. 变换到裁剪空间，计算视线空间深度（用于泡沫和雾化）
```

#### Gerstner 波公式

```
position:
  x += Q * A * dir.x * cos(phase)
  z += Q * A * dir.y * cos(phase)
  y += A * sin(phase)

其中:
  phase = freq * dot(dir, pos) + time
  Q = steepness / (freq * A * 4)   // 控制水平压缩
```

#### 片元着色器（frag）

```
1. 湍流 UV 扭曲 → 采样两层法线贴图 → UDN 混合
2. 法线沿波面法线重定向 → 得到最终法线 N
3. Schlick 菲涅尔: F = F0 + (1-F0) * pow(1-cosθ, power)
4. Cubemap 反射: reflect(-V, N) → texCUBE
5. 深度雾化: LinearEyeDepth(scene) - waterDepth → 浅/深水颜色混合
6. Blinn-Phong 高光: pow(dot(N, H), shininess)
7. Haimian 泡沫: depth / (width * noise * intensity) → pow 锐化 → 透明度
8. 合成: lerp(折射色, 反射色, F) + 高光 + 泡沫
```

---

## 浮动物体: `FloatingObject.cs`

`Assets/Water/Scripts/FloatingObject.cs` — 让物体在 Gerstner 波浪上真实浮动。

### 功能

| 行为 | 说明 |
|------|------|
| **随波起伏** | 采样 Gerstner 波高 → `SmoothDamp` 平滑跟随（不硬跳） |
| **多点多层采样** | 5 个采样点取平均，避免单点脱节 |
| **随波倾斜** | 采样波面法线 → 四元数球面插值倾斜 |
| **水流漂移** | 沿波浪合成方向缓慢漂移，到边界自动折返 |

### 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `waveHeightScale` | 0.3 | 与水面材质 `_WaveHeightScale` 同步 |
| `waves` | 4 波 | 与水面材质的 `_WaveAmp/Freq/Speed/Dir1~4` 同步 |
| `buoyancyHeight` | 0.3 | 浮出水面高度 |
| `followSmoothTime` | 0.15 | 垂直跟随平滑度（越小越紧） |
| `maxFollowSpeed` | 5 | 最大跟随速度 |
| `matchNormal` / `tiltSmooth` | true / 0.3 | 随波倾斜 |
| `enableDrift` / `driftSpeed` | true / 0.4 | 流向漂移 |
| `driftRadius` | 10 | 漂移范围半径 |

### 关键改进: 为什么这次不脱节

```
之前: pos.y = waveHeight(x, z)            ← 单点，硬跳
现在: avg = Σ waveHeight(samplePoints)     ← 5 点平均
      pos.y = SmoothDamp(pos.y, avg)       ← 平滑跟随
```

- **多采样点**: 在物体周围 5 个点采样波高，平均后更准确
- **SmoothDamp**: 不直接设置 position，用平滑阻尼逼近目标，杜绝硬跳
- **水平位移补偿**（可选）: `SampleHorizontalOffset` 计算 Gerstner 水平位移

### 使用

给任何 GameObject 添加 `FloatingObject` 组件即可。按 Play 后物体会在波浪上起伏、倾斜、随流漂移。

---

## 场景搭建工具: `WaterSceneSetup.cs`

### 菜单路径

`Tools > Water > Setup Water Scene`

### 功能

一键完成以下操作：
1. 删除旧材质和旧物体
2. 生成 **129×129 细分水面网格**（非 Quad）
3. 从噪声纹理生成法线贴图（Sobel 滤波）
4. 生成 Voronoi 细胞噪声泡沫贴图
5. 导入 Shader 并创建完整材质（所有参数已配置）
6. 放置 3 个测试物体（水下方块、浮球、半沉柱）
7. 设置摄像机位置和深度纹理模式
8. 设置方向光

### 网格精度配置

```csharp
// WaterSceneSetup.cs 第 9 行
public const int GridSegments = 128;
// 可选值:
//   32  →   1,089 顶点（极低配）
//   64  →   4,225 顶点（移动端）
//  128  →  16,641 顶点（推荐平衡）
//  256  →  65,537 顶点（高质量）
```

---

## DepthTextureEnabler.cs

自动开启主摄像机的深度纹理模式，使 Shader 中的 `_CameraDepthTexture` 可用。

```csharp
// 在场景加载后自动执行:
Camera.main.depthTextureMode |= DepthTextureMode.Depth;
// 并监听动态创建的摄像机
Camera.onPreCull += OnCameraPreCull;
```

---

## 用户操作流程

### 首次搭建

```
1. 确保 Unity 编辑器已打开此项目
2. 菜单 Tools > Water > Setup Water Scene
3. 按 Play 查看水面动画
```

### 调参

选中 `WaterSurface` → Inspector → 材质(Water_Mat) → 参数

**快速调波浪高度**: `_WaveHeightScale`（0~2，默认0.3）

### 切换波纹类型

材质面板 → `_UseGerstner`：
- **1** = Gerstner 波（尖峰效果，需细分网格）
- **0** = 正弦波（圆润，低配）

### 泡沫调参

| 效果 | 调大 | 调小 |
|------|------|------|
| 泡沫范围 | `_MainFoamWidth` ↑ | `_MainFoamWidth` ↓ |
| 泡沫明显度 | `_MainFoamIntensity` ↑ | `_MainFoamIntensity` ↓ |
| 泡沫纹理密度 | `_MainFoamScale` ↑ | `_MainFoamScale` ↓ |

### 改变网格密度

```
1. 打开 Assets/Water/Editor/WaterSceneSetup.cs
2. 修改第 9 行 GridSegments 的值
3. 重新运行 Tools > Water > Setup Water Scene
```

---

## 常见问题

### Q: 水面闪烁/周期性跳动
**原因**: 网格顶点太少，Gerstner 波无法平滑表现。  
**解决**: 增大 `GridSegments`（至少 64），重新 Setup。

### Q: Shader 无法在材质面板中选择
**原因**: Shader 编译错误（通常是因为文件中有特殊字符或缺少 `#pragma target 3.0`）。  
**解决**: Console 中查看具体错误信息，确保文件为 UTF-8 without BOM。

### Q: 泡沫不显示
**原因**: 
- 水面深度计算为负值 → `projPos.z` 需要存视线空间深度
- 泡沫纹理未赋值 → 检查 `_FoamTexture`  
**解决**: 重新运行 Setup，或检查材质面板中 `_FoamTexture` 和泡沫参数是否 >0。

### Q: `new Material(shader)` 创建空白材质
**原因**: Shader 编译是异步的，材质在 shader 未就绪时创建。  
**解决**: 使用 `AssetDatabase.ImportAsset` 强制同步导入后再创建材质。

---

## Shader 编译要求

- **Target**: 3.0（`#pragma target 3.0`）
- **Include**: `UnityCG.cginc`, `Lighting.cginc`
- **Render Pipeline**: Built-in（ForwardBase）
- **Unity 版本**: 2022.3+（低版本需验证）

---

## 效果层级对比

| 特性 | 正弦波模式 | Gerstner 模式 |
|------|-----------|--------------|
| 顶点数要求 | 低（Quad 即可） | 高（≥64×64 网格） |
| 波峰形状 | 圆润 | 尖锐 |
| 水平位移 | 无 | 有（真实感） |
| 性能 | 轻量 | 中量 |
| 泡沫 | ✅ | ✅ |
| 湍流扭曲 | ✅ | ✅ |
