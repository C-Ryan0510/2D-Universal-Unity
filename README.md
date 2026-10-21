# 流程事件視覺化 — Unity 2D 學習 PoC

用地鐵路線圖呈現軟體流程：每站代表一個邏輯步驟，透過模擬事件更新燈號，之後可從同一份事件紀錄重播。

這是候選方案的概念驗證，正式名稱未定。地鐵路線是目前採用的視覺比喻；既有檔案中的 `NetworkMetro` 是範例的技術名稱。所有展示資料都屬於 **Simulation**，不是真實 Git 或網路監控。

## 開發環境

- Unity **6000.6.6f1**（Unity 6.6）；使用專案記錄的版本開啟。
- 2D Universal，URP **17.6.0**，C#。
- macOS 本機 Unity Editor；GitHub 已完成首次專案提交與推送。
- 沿用目前專案，不另建 Unity 專案，不新增第三方依賴。

## 第一個範例：M0 四站路線

開啟 `Assets/Scenes/NetworkMetroM0.unity`，切到 **Game** 視窗；建議選 16:9 比例。

若場景尚未產生，在 Unity 上方選單執行 **Network Metro → Create M0 Sample Scene**。這會建立新範例場景；不覆寫既有的 `SampleScene`、URP 設定或 Build Settings。目標場景已存在時，只會開啟它。

畫面包含 `Local → Pack → Remote → Refs` 四站，以及 Simulation／手動預覽的標示。這些是 Git Push 的抽象階段，不是四次實際網路跳點。

### 你可以在 Unity 裡練習

1. 在 Hierarchy 展開 `Network Metro Canvas → Stations`。
2. 選取 `Station - Local`，在 Inspector 找到 **Metro Station View**。
3. 修改 **Station Label**、**Description** 或 **State**，觀察 Game 畫面。
4. 比較 `NotStarted`、`Running`、`Completed`、`Failed`、`Waiting`、`Skipped`；每種狀態都有文字，不只靠顏色。
5. 在編輯模式修改後儲存 Scene。Play Mode 中的修改通常不會在停止播放後保留。

目前 M0 是固定版面的手動燈號預覽，**沒有自動播放、點站詳情、事件產生器或歷史重播**。接下來才加入事件模型與情境 01。

### 連線元件練習（2026-10-21）

選取 `Connections → Track 1`，Add Component 搜尋 **Station Connector View**。指定 From Station 為 `Station - Local`、To Station 為 `Station - Pack`；Track Image 留空時會使用同物件的 Image，也可手動拖入。

2026-10-21 起，連線改為比較兩站狀態，規則如下：

採用「進站連線」：A → B 表示 B 的處理狀態，且 A 必須先 Completed。B 為 Running／Waiting／Failed 時，B → C 仍是未開始；等 B Completed，才允許 B → C 依 C 的狀態顯示。兩邊都會收到站點變更通知，但會各自依此規則判斷。

| From Station | To Station | 編輯模式 | Play Mode |
|---|---|---|---|
| Completed | Completed | 綠色 | 綠色 |
| Completed | Waiting | 固定黃色 | 慢速黃色呼吸 |
| Completed | Running | 固定青色 | 光帶從 From 移向 To |
| Completed | Failed | 紅色 | 固定紅色、停止光帶 |
| Completed | NotStarted／Skipped | 深灰 | 深灰 |
| 尚未 Completed／缺少任一站 | 任意 | 深灰 | 深灰、停止光帶 |

例如 Track 1 的 Local 保持 Completed，在 Play 中切換 Pack：Waiting → Running → Completed，線條應依序顯示慢速呼吸 → A 到 B 的移動光帶 → 固定綠色。

Track 的 Station Connector View 可設定 `Waiting Breath Seconds`（目前程式預設 6 秒一循環，數字越大越慢；已存場景以 Inspector 值為準）與 `Running Travel Seconds`（預設 1.2 秒走完 A 到 B）。光帶使用實際兩端物件的位置，適用目前的直線連線；尚未支援曲線／折線路徑。

動畫由 C# 控制 Image，**不需要 Animator、Controller 或 Clip**。Running 的 `Flow Band (Play Mode)` 子物件只在 Play 中產生，停用元件時會清除，不需手動建立。停止 Play 後通常恢復到播放前的設定。這只是兩站關係的視覺預覽，不會自動改變任一站的 State，也不代表真實傳輸。Failed 的紅色進站線表示目標站處理失敗，不代表資料傳送階段一定失敗；上游已完成站點保留綠色，下游線保持深灰。光暈／glow 尚未實作。

狀態判斷現在回傳有名稱的 `ConnectionState`（Idle／Waiting／Running／Completed／Failed），取代原本的 -1／0／1。未來若改用 Animator，可以讓它依同一個狀態切換顯示，不需重寫兩站比較規則。其他 Track 需各自掛元件並指定對應站點。

如果新元件沒有出現在 Add Component，先看 Console 是否有 C# 編譯錯誤；放入 Scripts 資料夾後，仍需編譯成功才能掛上。

## 開發順序

| 里程碑 | 範圍 |
|---|---|
| M0 | 四站、連線、站名、手動狀態預覽；本次範例 |
| M1 | 最小 Run／Event 模型，事件確定性地更新節點狀態 |
| M2 | 情境 01：模擬 Git Push 成功、事件列表、整體結果 |
| M3 | 情境 02／03：遠端拒絕與 HTTP 404，正確區分各層結果 |
| M4 | 情境 06：用已保存事件逐步重播、暫停與 Reset |
| M5／M6 | 情境 04 分支合併、05 重試視覺研究；第一版之後再評估 |

完整的優先順序、驗收與尚未決定事項見 [開發計畫](docs/DEVELOPMENT_PLAN.md)。

## 檔案入口

- `Assets/NetworkMetro/Scripts/MetroStationView.cs`：單站名稱、文字狀態與燈色。
- `Assets/NetworkMetro/Scripts/StationConnectorView.cs`：監聽兩端站點，顯示慢速等待呼吸與 Running 方向光帶。
- `Assets/NetworkMetro/Editor/MetroSampleSceneBuilder.cs`：在 Unity 內建立可編輯的 M0 場景。
- `Assets/Scenes/NetworkMetroM0.unity`：由建立工具產生的範例場景。
- `Assets/Scenes/SampleScene.unity`：保留原範本場景。

## 開發邊界

節點狀態、整體 Run 結果、HTTP 狀態碼分開處理。例如 DB 查詢成功但零筆，仍可回傳 404；Remote 接收完成，也不代表後續 Validation 通過。

初期只使用內建模擬資料，不呼叫 Git 指令、GitHub API、OpenAI API 或真實網路服務，不需要 Token。保留 URP 設定，不先做 3D／MR、資料庫、自動排版或大型框架。

## 驗證

M0 的檢查方式與目前結果記錄在 [開發計畫](docs/DEVELOPMENT_PLAN.md#目前驗證狀態)。功能完成與編譯通過會分開記錄；不要把尚未完成的里程碑視為已交付。
